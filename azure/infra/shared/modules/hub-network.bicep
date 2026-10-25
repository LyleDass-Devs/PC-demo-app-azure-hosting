@description('Azure region for the network resources.')
param location string

@description('Name of the hub virtual network.')
param hubVnetName string

@description('Address space of the hub virtual network.')
param hubAddressPrefix string

@description('Address range of the subnet that holds the pipeline agent.')
param agentSubnetPrefix string

@description('Tags applied to every resource.')
param tags object

var agentSubnetName = 'snet-agents'

// Nothing reaches the agent from outside: operators use "az vm run-command", which is
// logged in the activity log, instead of SSH. Outbound stays open through the default rules.
resource agentNsg 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: 'nsg-kubapc-agents'
  location: location
  tags: tags
  properties: {
    securityRules: [
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

resource hubVnet 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: hubVnetName
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        hubAddressPrefix
      ]
    }
    subnets: [
      {
        name: agentSubnetName
        properties: {
          addressPrefix: agentSubnetPrefix
          networkSecurityGroup: {
            id: agentNsg.id
          }
        }
      }
    ]
  }
}

output hubVnetId string = hubVnet.id
output agentSubnetId string = '${hubVnet.id}/subnets/${agentSubnetName}'
