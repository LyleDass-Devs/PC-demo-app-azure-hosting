@description('Azure region for the virtual machine.')
param location string

@description('Name of the virtual machine that runs the pipeline agent.')
param agentVmName string

@description('Size of the virtual machine.')
param agentVmSize string

@description('Administrator user name on the virtual machine.')
param agentAdminUsername string

@description('SSH public key for the administrator user. Password sign-in is disabled.')
param agentSshPublicKey string

@description('Resource id of the subnet the virtual machine is placed in.')
param agentSubnetId string

@description('Tags applied to every resource.')
param tags object

// A new virtual machine has no internet access by default. A Standard public IP behind a
// deny-all-inbound network security group gives outbound access for a fraction of the
// cost of a NAT gateway, which is what a production estate would use instead.
resource publicIp 'Microsoft.Network/publicIPAddresses@2024-05-01' = {
  name: 'pip-kubapc-agent'
  location: location
  tags: tags
  sku: {
    name: 'Standard'
    tier: 'Regional'
  }
  properties: {
    publicIPAllocationMethod: 'Static'
    publicIPAddressVersion: 'IPv4'
  }
}

resource networkInterface 'Microsoft.Network/networkInterfaces@2024-05-01' = {
  name: 'nic-kubapc-agent'
  location: location
  tags: tags
  properties: {
    ipConfigurations: [
      {
        name: 'primary'
        properties: {
          primary: true
          privateIPAllocationMethod: 'Dynamic'
          subnet: {
            id: agentSubnetId
          }
          publicIPAddress: {
            id: publicIp.id
          }
        }
      }
    ]
  }
}

resource agentVm 'Microsoft.Compute/virtualMachines@2024-07-01' = {
  name: agentVmName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    hardwareProfile: {
      vmSize: agentVmSize
    }
    storageProfile: {
      imageReference: {
        publisher: 'Canonical'
        offer: 'ubuntu-24_04-lts'
        sku: 'server'
        version: 'latest'
      }
      osDisk: {
        name: 'osdisk-${agentVmName}'
        createOption: 'FromImage'
        diskSizeGB: 30
        managedDisk: {
          storageAccountType: 'StandardSSD_LRS'
        }
        deleteOption: 'Delete'
      }
    }
    osProfile: {
      computerName: agentVmName
      adminUsername: agentAdminUsername
      linuxConfiguration: {
        disablePasswordAuthentication: true
        ssh: {
          publicKeys: [
            {
              path: '/home/${agentAdminUsername}/.ssh/authorized_keys'
              keyData: agentSshPublicKey
            }
          ]
        }
      }
    }
    networkProfile: {
      networkInterfaces: [
        {
          id: networkInterface.id
          properties: {
            deleteOption: 'Delete'
          }
        }
      ]
    }
    diagnosticsProfile: {
      bootDiagnostics: {
        enabled: true
      }
    }
  }
}

output agentVmName string = agentVm.name
output agentPrincipalId string = agentVm.identity.principalId
output agentPublicIp string = publicIp.properties.ipAddress
