using './main.bicep'

param hubVnetName = 'vnet-kubapc-hub'
param hubAddressPrefix = '10.10.0.0/16'
param agentSubnetPrefix = '10.10.1.0/24'
param agentVmName = 'vm-kubapc-agent'
param agentVmSize = 'Standard_B2s'
param agentAdminUsername = 'kubapc'

// deploy-shared.sh exports this from ~/.ssh/id_ed25519.pub before deploying.
param agentSshPublicKey = readEnvironmentVariable('AGENT_SSH_PUBLIC_KEY')

param tags = {
  workload: 'kubapc'
  environment: 'shared'
  owner: 'lyledass'
  purpose: 'demo'
}
