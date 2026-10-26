using './main.bicep'

param namePrefix = 'kubapc-prod'
param environmentName = 'prod'
param staticWebAppLocation = 'westeurope'
param vnetAddressPrefix = '10.30.0.0/16'
param appSubnetPrefix = '10.30.1.0/24'
param privateEndpointSubnetPrefix = '10.30.2.0/24'
param sharedResourceGroupName = 'rg-kubapc-shared'
param hubVnetName = 'vnet-kubapc-hub'
param databaseName = 'kubapc'

// Paste the object id of the sc-kubapc-prod service principal here. The last line
// printed by azure/scripts/grant-pipeline-roles.sh prod <client id> gives it to you.
param pipelinePrincipalObjectId = 'fad8a987-badd-4f0e-818b-667ba23903dd'
param pipelinePrincipalLoginName = 'sc-kubapc-prod'

param jwtSecret = readEnvironmentVariable('JWT_SECRET')
