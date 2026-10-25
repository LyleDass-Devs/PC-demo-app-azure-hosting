@description('Azure region for the plan and the API.')
param location string

@description('Short name used in every resource name, for example kubapc-test.')
param namePrefix string

@description('Six-character suffix that keeps globally unique names from colliding.')
param uniqueSuffix string

@description('Tags applied to every resource.')
param tags object

@description('Resource id of the delegated subnet the App Service plan is integrated into.')
param appSubnetId string

@description('Name of the Key Vault that holds the token-signing key.')
param keyVaultName string

@description('Secret identifier of the token-signing key in Key Vault.')
param jwtSecretUri string

@description('Fully qualified host name of the PostgreSQL server.')
param postgresServerFqdn string

@description('Name of the application database.')
param databaseName string

@description('Application Insights connection string.')
param appInsightsConnectionString string

@description('Resource id of the Log Analytics workspace that receives the API logs.')
param logAnalyticsId string

@description('Origin of the admin site, allowed by CORS.')
param clientOrigin string

var planName = 'asp-${namePrefix}'
var apiName = 'app-${namePrefix}-api-${uniqueSuffix}'
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

// The API signs in to PostgreSQL as its own managed identity, so its user name is its own
// name and the connection string holds no secret. That is why it is a plain setting.
var databaseConnectionString = 'Host=${postgresServerFqdn};Port=5432;Database=${databaseName};Username=${apiName};SSL Mode=Require;Trust Server Certificate=false'

resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: planName
  location: location
  tags: tags
  kind: 'linux'
  sku: {
    name: 'B1'
    tier: 'Basic'
    capacity: 1
  }
  properties: {
    reserved: true
    zoneRedundant: false
  }
}

resource api 'Microsoft.Web/sites@2024-04-01' = {
  name: apiName
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    clientAffinityEnabled: false
    virtualNetworkSubnetId: appSubnetId
    vnetRouteAllEnabled: true
    publicNetworkAccess: 'Enabled'
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      healthCheckPath: '/health'
      remoteDebuggingEnabled: false
      webSocketsEnabled: false
      scmMinTlsVersion: '1.2'
      appSettings: [
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
        { name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED', value: 'true' }
        { name: 'WEBSITE_RUN_FROM_PACKAGE', value: '1' }
        { name: 'ConnectionStrings__KubaPCDatabase', value: databaseConnectionString }
        { name: 'Database__UseEntraIdAuthentication', value: 'true' }
        { name: 'AppSettings__Secret', value: '@Microsoft.KeyVault(SecretUri=${jwtSecretUri})' }
        { name: 'AppSettings__Issuer', value: 'KubaPC' }
        { name: 'AppSettings__Audience', value: 'KubaPC.Clients' }
        { name: 'AppSettings__TokenExpireHours', value: '5' }
        { name: 'Cors__Origins__0', value: clientOrigin }
        { name: 'AllowedHosts', value: '${apiName}.azurewebsites.net' }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnectionString }
        { name: 'ApplicationInsightsAgent_EXTENSION_VERSION', value: '~3' }
      ]
    }
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2024-11-01' existing = {
  name: keyVaultName
}

resource apiReadsSecrets 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, api.id, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: api.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'send-to-log-analytics'
  scope: api
  properties: {
    workspaceId: logAnalyticsId
    logs: [
      { category: 'AppServiceHTTPLogs', enabled: true }
      { category: 'AppServiceConsoleLogs', enabled: true }
      { category: 'AppServiceAppLogs', enabled: true }
      { category: 'AppServiceAuditLogs', enabled: true }
    ]
  }
}

output apiName string = api.name
output apiHostname string = api.properties.defaultHostName
output apiPrincipalId string = api.identity.principalId
