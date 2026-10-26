@description('Azure region for the server.')
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

@description('Resource id of the Log Analytics workspace that receives server logs.')
param logAnalyticsId string

@description('Name of the application database.')
param databaseName string

@description('Object id of the pipeline service principal that becomes the Entra ID administrator.')
param pipelinePrincipalObjectId string

@description('Login name the pipeline service principal uses on the server, for example sc-kubapc-test.')
param pipelinePrincipalLoginName string

var serverName = 'psql-${namePrefix}-${uniqueSuffix}'
var privateDnsZoneId = resourceId(sharedResourceGroupName, 'Microsoft.Network/privateDnsZones', 'privatelink.postgres.database.azure.com')

// Password login is off at the server, so no administrator password exists anywhere.
// Everything that runs SQL signs in with an Entra ID token.
resource server 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: serverName
  location: location
  tags: tags
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    version: '17'
    storage: {
      storageSizeGB: 32
      autoGrow: 'Disabled'
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
    network: {
      publicNetworkAccess: 'Disabled'
    }
    authConfig: {
      activeDirectoryAuth: 'Enabled'
      passwordAuth: 'Disabled'
      tenantId: subscription().tenantId
    }
  }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: server
  name: databaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// Server-level operations fail when they overlap, so the child resources below form a chain.
resource requireSsl 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2024-08-01' = {
  parent: server
  name: 'require_secure_transport'
  properties: {
    value: 'on'
    source: 'user-override'
  }
  dependsOn: [
    database
  ]
}

resource allowedExtensions 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2024-08-01' = {
  parent: server
  name: 'azure.extensions'
  properties: {
    value: 'PGCRYPTO'
    source: 'user-override'
  }
  dependsOn: [
    requireSsl
  ]
}

// The login name is chosen here rather than taken from the app registration's display
// name, which can exceed PostgreSQL's 63-character limit for role names.
resource pipelineAdministrator 'Microsoft.DBforPostgreSQL/flexibleServers/administrators@2024-08-01' = {
  parent: server
  name: pipelinePrincipalObjectId
  properties: {
    principalName: pipelinePrincipalLoginName
    principalType: 'ServicePrincipal'
    tenantId: subscription().tenantId
  }
  dependsOn: [
    allowedExtensions
  ]
}

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2024-05-01' = {
  name: 'pe-${serverName}'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'postgres'
        properties: {
          privateLinkServiceId: server.id
          groupIds: [
            'postgresqlServer'
          ]
        }
      }
    ]
  }
  // Last link of the chain above: the server rejects a private endpoint while it is
  // still applying configuration ("ServerIsBusy").
  dependsOn: [
    pipelineAdministrator
  ]
}

resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2024-05-01' = {
  parent: privateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'postgres'
        properties: {
          privateDnsZoneId: privateDnsZoneId
        }
      }
    ]
  }
}

resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'send-to-log-analytics'
  scope: server
  dependsOn: [
    privateDnsZoneGroup
  ]
  properties: {
    workspaceId: logAnalyticsId
    logs: [
      {
        category: 'PostgreSQLLogs'
        enabled: true
      }
    ]
  }
}

output serverName string = server.name
output serverFqdn string = server.properties.fullyQualifiedDomainName
output databaseName string = database.name
