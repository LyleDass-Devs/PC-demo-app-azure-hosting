using './main.bicep'

param namePrefix = 'kubapc-test'
param environmentName = 'test'
param staticWebAppLocation = 'westeurope'
param vnetAddressPrefix = '10.20.0.0/16'
param appSubnetPrefix = '10.20.1.0/24'
param privateEndpointSubnetPrefix = '10.20.2.0/24'
param sharedResourceGroupName = 'rg-kubapc-shared'
param hubVnetName = 'vnet-kubapc-hub'
param databaseName = 'kubapc'

// Paste the object id of the sc-kubapc-test service principal here. The last line
// printed by azure/scripts/grant-pipeline-roles.sh test <client id> gives it to you.
param pipelinePrincipalObjectId = 'c9f20ce8-3e73-4795-9a80-4c77979d6d6b'
param pipelinePrincipalLoginName = 'sc-kubapc-test'

param jwtSecret = readEnvironmentVariable('JWT_SECRET')
