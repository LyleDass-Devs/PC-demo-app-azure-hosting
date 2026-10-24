param location string
param namePrefix string
param uniqueSuffix string
param tags object
param appSubnetId string
param keyVaultName string
param connectionStringSecretUri string
param jwtSecretUri string
param appInsightsConnectionString string
param logAnalyticsId string
param clientOrigin string

var planName = 'asp-${namePrefix}'
var apiName = 'app-${namePrefix}-api-${uniqueSuffix}'
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

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
        { name: 'ConnectionStrings__KubaPCDatabase', value: '@Microsoft.KeyVault(SecretUri=${connectionStringSecretUri})' }
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
