@description('Azure region for the vault.')
param location string

@description('Short name used in every resource name, for example kubapc-test.')
param namePrefix string

@description('Six-character suffix that keeps globally unique names from colliding.')
param uniqueSuffix string

@description('Tags applied to every resource.')
param tags object

@description('Resource id of the subnet that holds the private endpoint.')
param privateEndpointSubnetId string

@description('Resource group that holds the shared private DNS zones.')
param sharedResourceGroupName string

@description('Resource id of the Log Analytics workspace that receives audit events.')
param logAnalyticsId string

@secure()
@description('At least 64 random bytes used to sign sign-in tokens.')
param jwtSecret string

var keyVaultName = 'kv-${namePrefix}-${uniqueSuffix}'
var privateDnsZoneId = resourceId(sharedResourceGroupName, 'Microsoft.Network/privateDnsZones', 'privatelink.vaultcore.azure.net')

resource keyVault 'Microsoft.KeyVault/vaults@2024-11-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    // Purge protection is left unset (off). Azure rejects an explicit false, and
    // turning it on cannot be undone, which would block teardown and redeploy for
    // the retention period. A bank would turn it on; see the hardening table.
    publicNetworkAccess: 'Disabled'
    networkAcls: {
      defaultAction: 'Deny'
      bypass: 'AzureServices'
    }
  }
}

// The token-signing key is the only secret left: the database connection string no
// longer carries a password, so it is a plain App Service setting instead.
resource jwtSecretResource 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = {
  parent: keyVault
  name: 'KubaPC-JwtSecret'
  properties: {
    value: jwtSecret
    contentType: 'text/plain'
  }
}

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2024-05-01' = {
  name: 'pe-${keyVaultName}'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'keyvault'
        properties: {
          privateLinkServiceId: keyVault.id
          groupIds: [
            'vault'
          ]
        }
      }
    ]
  }
}

resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2024-05-01' = {
  parent: privateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'keyvault'
        properties: {
          privateDnsZoneId: privateDnsZoneId
        }
      }
    ]
  }
}

resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'send-to-log-analytics'
  scope: keyVault
  properties: {
    workspaceId: logAnalyticsId
    logs: [
      {
        category: 'AuditEvent'
        enabled: true
      }
    ]
  }
}

output keyVaultName string = keyVault.name
output keyVaultId string = keyVault.id
output jwtSecretUri string = jwtSecretResource.properties.secretUri
