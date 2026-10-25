@description('Short name used in every resource name, for example kubapc-test.')
param namePrefix string

@description('Tags applied to every resource.')
param tags object

@description('Region for the Static Web App, which is not offered in South Africa.')
param location string

resource adminSite 'Microsoft.Web/staticSites@2024-04-01' = {
  name: 'stapp-${namePrefix}-admin'
  location: location
  tags: tags
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    stagingEnvironmentPolicy: 'Disabled'
    allowConfigFileUpdates: true
    enterpriseGradeCdnStatus: 'Disabled'
  }
}

output adminSiteName string = adminSite.name
output adminHostname string = adminSite.properties.defaultHostname
