targetScope = 'resourceGroup'

@description('Short name used in every resource name, for example kubapc-dev.')
param namePrefix string = 'kubapc-dev'

@description('Azure region for everything except Static Web Apps.')
param location string = resourceGroup().location

@description('Region for Static Web Apps, which is not offered in South Africa.')
param staticWebAppLocation string = 'westeurope'

@description('PostgreSQL administrator login.')
param postgresAdministratorLogin string = 'kubapc_admin'

@description('Name of the application database.')
param databaseName string = 'kubapc'

@description('Login the API uses. Created by the database scripts, password set by the migration step.')
param applicationDatabaseLogin string = 'kubapc_app'

@secure()
@description('Password for the PostgreSQL administrator.')
param postgresAdministratorPassword string

@secure()
@description('Password for the application login the API connects with.')
param applicationDatabasePassword string

@secure()
@description('At least 64 random bytes used to sign sign-in tokens.')
param jwtSecret string

param tags object = {
  workload: 'kubapc'
  environment: 'dev'
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
    privateDnsZoneId: network.outputs.postgresDnsZoneId
    logAnalyticsId: monitoring.outputs.logAnalyticsId
    administratorLogin: postgresAdministratorLogin
    administratorPassword: postgresAdministratorPassword
    databaseName: databaseName
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

var connectionString = 'Host=${postgres.outputs.serverFqdn};Port=5432;Database=${databaseName};Username=${applicationDatabaseLogin};Password=${applicationDatabasePassword};SSL Mode=Require;Trust Server Certificate=false'

module keyVault 'modules/keyvault.bicep' = {
  name: 'key-vault'
  params: {
    location: location
    namePrefix: namePrefix
    uniqueSuffix: uniqueSuffix
    tags: tags
    privateEndpointSubnetId: network.outputs.privateEndpointSubnetId
    privateDnsZoneId: network.outputs.keyVaultDnsZoneId
    logAnalyticsId: monitoring.outputs.logAnalyticsId
    databaseConnectionString: connectionString
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
    connectionStringSecretUri: keyVault.outputs.connectionStringSecretUri
    jwtSecretUri: keyVault.outputs.jwtSecretUri
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    logAnalyticsId: monitoring.outputs.logAnalyticsId
    clientOrigin: 'https://${staticWebApp.outputs.adminHostname}'
  }
}

output apiName string = appService.outputs.apiName
output apiUrl string = 'https://${appService.outputs.apiHostname}'
output adminSiteName string = staticWebApp.outputs.adminSiteName
output adminUrl string = 'https://${staticWebApp.outputs.adminHostname}'
output postgresServerName string = postgres.outputs.serverName
output postgresServerFqdn string = postgres.outputs.serverFqdn
output databaseName string = postgres.outputs.databaseName
output keyVaultName string = keyVault.outputs.keyVaultName
