targetScope = 'resourceGroup'

@description('Azure region for every shared resource.')
param location string = resourceGroup().location

@description('Name of the hub virtual network every environment peers with.')
param hubVnetName string = 'vnet-kubapc-hub'

@description('Address space of the hub virtual network.')
param hubAddressPrefix string = '10.10.0.0/16'

@description('Address range of the subnet that holds the pipeline agent.')
param agentSubnetPrefix string = '10.10.1.0/24'

@description('Name of the virtual machine that runs the pipeline agent.')
param agentVmName string = 'vm-kubapc-agent'

@description('Size of the agent virtual machine.')
param agentVmSize string = 'Standard_B2s_v2'

@description('Administrator user name on the agent virtual machine.')
param agentAdminUsername string = 'kubapc'

@description('SSH public key for the administrator user. Password sign-in is disabled.')
param agentSshPublicKey string

@description('Tags applied to every resource.')
param tags object

module hubNetwork 'modules/hub-network.bicep' = {
  name: 'hub-network'
  params: {
    location: location
    hubVnetName: hubVnetName
    hubAddressPrefix: hubAddressPrefix
    agentSubnetPrefix: agentSubnetPrefix
    tags: tags
  }
}

module agentVm 'modules/agent-vm.bicep' = {
  name: 'agent-vm'
  params: {
    location: location
    agentVmName: agentVmName
    agentVmSize: agentVmSize
    agentAdminUsername: agentAdminUsername
    agentSshPublicKey: agentSshPublicKey
    agentSubnetId: hubNetwork.outputs.agentSubnetId
    tags: tags
  }
}

module privateDnsZones 'modules/private-dns-zones.bicep' = {
  name: 'private-dns-zones'
  params: {
    hubVnetName: hubVnetName
    hubVnetId: hubNetwork.outputs.hubVnetId
    tags: tags
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    tags: tags
  }
}

output agentVmName string = agentVm.outputs.agentVmName
output agentPrincipalId string = agentVm.outputs.agentPrincipalId
output agentPublicIp string = agentVm.outputs.agentPublicIp
output storageAccountName string = storage.outputs.storageAccountName
output hubVnetId string = hubNetwork.outputs.hubVnetId
output postgresDnsZoneId string = privateDnsZones.outputs.postgresDnsZoneId
output keyVaultDnsZoneId string = privateDnsZones.outputs.keyVaultDnsZoneId
