@description('Azure region for the network resources.')
param location string

@description('Short name used in every resource name, for example kubapc-test.')
param namePrefix string

@description('Tags applied to every resource.')
param tags object

@description('Address space of this environment\'s virtual network.')
param vnetAddressPrefix string

@description('Address range of the subnet the App Service plan is integrated into.')
param appSubnetPrefix string

@description('Address range of the subnet that holds the private endpoints.')
param privateEndpointSubnetPrefix string

@description('Resource group that holds the hub virtual network and the private DNS zones.')
param sharedResourceGroupName string

@description('Name of the hub virtual network this environment peers with.')
param hubVnetName string

var vnetName = 'vnet-${namePrefix}'
var appSubnetName = 'snet-app'
var privateEndpointSubnetName = 'snet-private-endpoints'
var hubVnetId = resourceId(sharedResourceGroupName, 'Microsoft.Network/virtualNetworks', hubVnetName)

resource privateEndpointNsg 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: 'nsg-${namePrefix}-private-endpoints'
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'AllowVnetInbound'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: '*'
          sourceAddressPrefix: 'VirtualNetwork'
          sourcePortRange: '*'
          destinationAddressPrefix: 'VirtualNetwork'
          destinationPortRange: '*'
        }
      }
      {
        name: 'DenyAllInbound'
        properties: {
          priority: 4000
          direction: 'Inbound'
          access: 'Deny'
          protocol: '*'
          sourceAddressPrefix: '*'
          sourcePortRange: '*'
          destinationAddressPrefix: '*'
          destinationPortRange: '*'
        }
      }
    ]
  }
}

resource vnet 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: vnetName
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        vnetAddressPrefix
      ]
    }
    subnets: [
      {
        name: appSubnetName
        properties: {
          addressPrefix: appSubnetPrefix
          delegations: [
            {
              name: 'appservice'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
        }
      }
      {
        name: privateEndpointSubnetName
        properties: {
          addressPrefix: privateEndpointSubnetPrefix
          privateEndpointNetworkPolicies: 'Enabled'
          networkSecurityGroup: {
            id: privateEndpointNsg.id
          }
        }
      }
    ]
  }
}

// The hub side of the peering and the DNS zone links live in the shared resource group,
// so they are created by modules deployed into that group rather than this one.
resource spokeToHubPeering 'Microsoft.Network/virtualNetworks/virtualNetworkPeerings@2024-05-01' = {
  parent: vnet
  name: 'peer-to-hub'
  properties: {
    remoteVirtualNetwork: {
      id: hubVnetId
    }
    allowVirtualNetworkAccess: true
    allowForwardedTraffic: false
    allowGatewayTransit: false
    useRemoteGateways: false
  }
}

module hubToSpokePeering 'hub-peering.bicep' = {
  name: 'hub-peering-${vnetName}'
  scope: resourceGroup(sharedResourceGroupName)
  params: {
    hubVnetName: hubVnetName
    spokeVnetName: vnetName
    spokeVnetId: vnet.id
  }
}

module privateDnsLinks 'private-dns-links.bicep' = {
  name: 'private-dns-links-${vnetName}'
  scope: resourceGroup(sharedResourceGroupName)
  params: {
    spokeVnetName: vnetName
    spokeVnetId: vnet.id
  }
}

output vnetId string = vnet.id
output appSubnetId string = '${vnet.id}/subnets/${appSubnetName}'
output privateEndpointSubnetId string = '${vnet.id}/subnets/${privateEndpointSubnetName}'
