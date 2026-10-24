#!/usr/bin/env bash
set -euo pipefail

resourceGroup="${1:-rg-kubapc-dev}"
location="${2:-southafricanorth}"

echo "Signed in as: $(az account show --query user.name --output tsv) on subscription $(az account show --query name --output tsv)"

for provider in Microsoft.Web Microsoft.DBforPostgreSQL Microsoft.KeyVault Microsoft.Network Microsoft.OperationalInsights Microsoft.Insights; do
  az provider register --namespace "$provider" --output none
done
echo "Resource providers registered."

az group create --name "$resourceGroup" --location "$location" --tags workload=kubapc environment=dev purpose=demo --output table

echo
echo "Generated secrets. Paste each one into the Azure DevOps variable group kubapc-dev-secrets and tick the padlock."
echo "POSTGRES_ADMIN_PASSWORD = $(openssl rand -base64 24 | tr -d '/+=' | cut -c1-28)Aa1"
echo "APP_DB_PASSWORD         = $(openssl rand -base64 24 | tr -d '/+=' | cut -c1-28)Aa1"
echo "JWT_SECRET              = $(openssl rand -base64 96 | tr -d '\n')"
