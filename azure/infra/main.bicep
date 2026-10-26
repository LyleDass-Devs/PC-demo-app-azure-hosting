targetScope = 'resourceGroup'

@description('Short name used in every resource name, for example kubapc-test.')
param namePrefix string

@description('Environment this deployment belongs to, for example test or prod. Used for the environment tag.')
param environmentName string

@description('Azure region for everything except Static Web Apps.')
param location string = resourceGroup().location

@description('Region for Static Web Apps, which is not offered in South Africa.')
param staticWebAppLocation string = 'eastus2'

@description('Address space of this environment\'s virtual network, for example 10.20.0.0/16.')
param vnetAddressPrefix string

@description('Address range of the subnet the App Service plan is integrated into, for example 10.20.1.0/24.')
param appSubnetPrefix string

@description('Address range of the subnet that holds the private endpoints, for example 10.20.2.0/24.')
param privateEndpointSubnetPrefix string

@description('Resource group that holds the hub virtual network and the private DNS zones.')
param sharedResourceGroupName string = 'rg-kubapc-shared'

@description('Name of the hub virtual network this environment peers with.')
param hubVnetName string = 'vnet-kubapc-hub'

@description('Name of the application database.')
param databaseName string = 'kubapc'

@description('Object id of the pipeline service principal that becomes the PostgreSQL Entra ID administrator.')
param pipelinePrincipalObjectId string

@description('Login name the pipeline service principal uses on PostgreSQL, for example sc-kubapc-test.')
param pipelinePrincipalLoginName string

@secure()
@description('At least 64 random bytes used to sign sign-in tokens.')
param jwtSecret string

var tags = {
  workload: 'kubapc'
  environment: environmentName
  owner: 'lyledass'
  purpose: 'demo'
}

var uniqueSuffix = substring(uniqueString(resourceGroup().id), 0, 6)

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    namePrefix: namePrefix
    tags: tags
  }
}

module network 'modules/network.bicep' = {
  name: 'network'
  params: {
    location: location
    namePrefix: namePrefix
    tags: tags
    vnetAddressPrefix: vnetAddressPrefix
    appSubnetPrefix: appSubnetPrefix
    privateEndpointSubnetPrefix: privateEndpointSubnetPrefix
    sharedResourceGroupName: sharedResourceGroupName
    hubVnetName: hubVnetName
  }
}

module postgres 'modules/postgres.bicep' = {
  name: 'postgres'
  params: {
    location: location
    namePrefix: namePrefix
    uniqueSuffix: uniqueSuffix
    tags: tags
    privateEndpointSubnetId: network.outputs.privateEndpointSubnetId
    sharedResourceGroupName: sharedResourceGroupName
    logAnalyticsId: monitoring.outputs.logAnalyticsId
    databaseName: databaseName
    pipelinePrincipalObjectId: pipelinePrincipalObjectId
    pipelinePrincipalLoginName: pipelinePrincipalLoginName
  }
}

module staticWebApp 'modules/staticwebapp.bicep' = {
  name: 'static-web-app'
  params: {
    namePrefix: namePrefix
    tags: tags
    location: staticWebAppLocation
  }
}

module keyVault 'modules/keyvault.bicep' = {
  name: 'key-vault'
  params: {
    location: location
    namePrefix: namePrefix
    uniqueSuffix: uniqueSuffix
    tags: tags
    privateEndpointSubnetId: network.outputs.privateEndpointSubnetId
    sharedResourceGroupName: sharedResourceGroupName
    logAnalyticsId: monitoring.outputs.logAnalyticsId
    jwtSecret: jwtSecret
  }
}

module appService 'modules/appservice.bicep' = {
  name: 'app-service'
  params: {
    location: location
    namePrefix: namePrefix
    uniqueSuffix: uniqueSuffix
    tags: tags
    appSubnetId: network.outputs.appSubnetId
    keyVaultName: keyVault.outputs.keyVaultName
    jwtSecretUri: keyVault.outputs.jwtSecretUri
    postgresServerFqdn: postgres.outputs.serverFqdn
    databaseName: postgres.outputs.databaseName
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    logAnalyticsId: monitoring.outputs.logAnalyticsId
    clientOrigin: 'https://${staticWebApp.outputs.adminHostname}'
  }
}

output apiName string = appService.outputs.apiName
output apiUrl string = 'https://${appService.outputs.apiHostname}'
output apiPrincipalId string = appService.outputs.apiPrincipalId
output adminSiteName string = staticWebApp.outputs.adminSiteName
output adminUrl string = 'https://${staticWebApp.outputs.adminHostname}'
output postgresServerName string = postgres.outputs.serverName
output postgresServerFqdn string = postgres.outputs.serverFqdn
output databaseName string = postgres.outputs.databaseName
output keyVaultName string = keyVault.outputs.keyVaultName
