#!/usr/bin/env bash
# Prepares the subscription before anything is deployed. Run once, from WSL,
# after `az login`, and before deploy-shared.sh.
#
# It checks the region can give us the VM and database sizes the templates ask
# for (a new subscription sometimes has a quota of zero and the deployment would
# fail twenty minutes in with an unhelpful message), switches on the Azure
# services we use, creates the three resource groups so the pipeline can
# what-if prod from its very first run, and prints the two token-signing secrets
# you paste into Azure DevOps. Safe to rerun: every step is idempotent.
set -euo pipefail

location="${LOCATION:-southafricanorth}"
tags=(workload=kubapc owner=lyledass purpose=demo)

echo "Signed in as: $(az account show --query user.name --output tsv) on subscription $(az account show --query name --output tsv)"

# The agent VM is a Standard_B2s_v2, which needs two vCPUs from the "Standard Bsv2 Family" quota.
# (Standard_B2s ran out of capacity in South Africa North, so we moved to the v2 family.)
vcpuLimit=$(az vm list-usage --location "$location" --query "[?name.localizedValue=='Standard Bsv2 Family vCPUs'].limit | [0]" --output tsv)
if [ -z "$vcpuLimit" ] || [ "$vcpuLimit" -lt 2 ]; then
  echo "This subscription cannot create a B-series VM in $location (Standard Bsv2 Family vCPU limit is '${vcpuLimit:-unknown}', we need 2)."
  echo "Ask for a quota increase in the portal (Quotas > Compute) or move the hub to a region that has it, then rerun."
  exit 1
fi
echo "VM quota: $vcpuLimit Standard Bsv2 Family vCPUs available in $location."

# The database is a Standard_B1ms. The SKU listing is deeply nested, so a plain text
# search for the name is more robust than a query path that changes between API versions.
if ! az postgres flexible-server list-skus --location "$location" --output json | grep -q '"Standard_B1ms"'; then
  echo "PostgreSQL Flexible Server Standard_B1ms is not offered in $location for this subscription."
  echo "Pick another region for the environments or another SKU in azure/infra/modules, then rerun."
  exit 1
fi
echo "Database SKU: Standard_B1ms is available in $location."

for provider in Microsoft.Web Microsoft.DBforPostgreSQL Microsoft.KeyVault Microsoft.Network Microsoft.OperationalInsights Microsoft.Insights Microsoft.Compute Microsoft.Storage; do
  az provider register --namespace "$provider" --output none
done
echo "Resource providers registered."

az group create --name rg-kubapc-shared --location "$location" --tags "${tags[@]}" environment=shared --output none
az group create --name rg-kubapc-test --location "$location" --tags "${tags[@]}" environment=test --output none
az group create --name rg-kubapc-prod --location "$location" --tags "${tags[@]}" environment=prod --output none
echo "Resource groups ready: rg-kubapc-shared, rg-kubapc-test, rg-kubapc-prod."

echo
echo "Generated secrets. Paste each one into its Azure DevOps variable group and tick the padlock."
echo "Nothing else ever needs a password: the database uses Entra ID logins."
echo "JWT_SECRET for kubapc-test-secrets = $(openssl rand -base64 96 | tr -d '\n')"
echo "JWT_SECRET for kubapc-prod-secrets = $(openssl rand -base64 96 | tr -d '\n')"
