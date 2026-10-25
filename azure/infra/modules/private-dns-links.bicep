// Deployed into the shared resource group: the private DNS zones are shared by every
// environment, so each environment only adds a link for its own virtual network.

@description('Name of the environment virtual network to link to the shared zones.')
param spokeVnetName string

@description('Resource id of the environment virtual network.')
param spokeVnetId string

resource postgresDnsZone 'Microsoft.Network/privateDnsZones@2024-06-01' existing = {
  name: 'privatelink.postgres.database.azure.com'
}

resource keyVaultDnsZone 'Microsoft.Network/privateDnsZones@2024-06-01' existing = {
  name: 'privatelink.vaultcore.azure.net'
}

resource postgresDnsLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' = {
  parent: postgresDnsZone
  name: 'link-${spokeVnetName}'
  location: 'global'
  properties: {
    virtualNetwork: {
      id: spokeVnetId
    }
    registrationEnabled: false
  }
}

resource keyVaultDnsLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' = {
  parent: keyVaultDnsZone
  name: 'link-${spokeVnetName}'
  location: 'global'
  properties: {
    virtualNetwork: {
      id: spokeVnetId
    }
    registrationEnabled: false
  }
}

output postgresDnsLinkId string = postgresDnsLink.id
output keyVaultDnsLinkId string = keyVaultDnsLink.id
