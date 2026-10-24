param namePrefix string
param tags object
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
