// Deployed into the shared resource group: a peering can only be created on the
// virtual network it belongs to, and the hub lives in a different group.

@description('Name of the hub virtual network that receives the peering.')
param hubVnetName string

@description('Name of the environment virtual network being peered to the hub.')
param spokeVnetName string

@description('Resource id of the environment virtual network.')
param spokeVnetId string

resource hubVnet 'Microsoft.Network/virtualNetworks@2024-05-01' existing = {
  name: hubVnetName
}

resource hubToSpokePeering 'Microsoft.Network/virtualNetworks/virtualNetworkPeerings@2024-05-01' = {
  parent: hubVnet
  name: 'peer-to-${spokeVnetName}'
  properties: {
    remoteVirtualNetwork: {
      id: spokeVnetId
    }
    allowVirtualNetworkAccess: true
    allowForwardedTraffic: false
    allowGatewayTransit: false
    useRemoteGateways: false
  }
}

output peeringId string = hubToSpokePeering.id
