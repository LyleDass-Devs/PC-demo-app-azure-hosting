#!/usr/bin/env bash
set -euo pipefail

resourceGroup="${1:?resource group}"
deploymentName="${2:?bicep deployment name}"
siteFolder="${3:?published wwwroot folder}"

apiUrl=$(az deployment group show --resource-group "$resourceGroup" --name "$deploymentName" --query properties.outputs.apiUrl.value --output tsv)
siteName=$(az deployment group show --resource-group "$resourceGroup" --name "$deploymentName" --query properties.outputs.adminSiteName.value --output tsv)

cat > "$siteFolder/appsettings.json" <<EOF
{
  "ApiBaseUrl": "$apiUrl"
}
EOF
rm -f "$siteFolder/appsettings.Development.json"

sed -i "s#connect-src 'self' http://localhost:5161 https://localhost:7122#connect-src 'self' $apiUrl#" "$siteFolder/index.html"
grep -q "connect-src 'self' $apiUrl" "$siteFolder/index.html" || { echo "Content security policy was not updated."; exit 1; }

token=$(az staticwebapp secrets list --name "$siteName" --resource-group "$resourceGroup" --query properties.apiKey --output tsv)
npx --yes @azure/static-web-apps-cli@2 deploy "$siteFolder" --deployment-token "$token" --env production
echo "Admin client published."
