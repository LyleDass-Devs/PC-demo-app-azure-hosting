@description('Azure region for the storage account.')
param location string

@description('Tags applied to every resource.')
param tags object

// Storage account names allow only lower-case letters and digits, 3 to 24 characters.
// The 13-character hash keeps the name unique without leaving a dash in it.
var storageAccountName = 'stkubapc${uniqueString(resourceGroup().id)}'
// One container per environment, so each pipeline identity can be given rights on
// its own container only and the test identity can never read a prod dump.
var environmentNames = [
  'test'
  'prod'
]

// Entra ID is the only way in: shared keys are off so a leaked key cannot read a dump,
// and public blob access is off so a container can never be made anonymous by mistake.
resource storageAccount 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: storageAccountName
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    allowSharedKeyAccess: false
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    publicNetworkAccess: 'Enabled'
    defaultToOAuthAuthentication: true
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2024-01-01' = {
  parent: storageAccount
  name: 'default'
}

resource dataLoadsContainers 'Microsoft.Storage/storageAccounts/blobServices/containers@2024-01-01' = [for environmentName in environmentNames: {
  parent: blobService
  name: 'data-loads-${environmentName}'
  properties: {
    publicAccess: 'None'
  }
}]

output storageAccountName string = storageAccount.name
