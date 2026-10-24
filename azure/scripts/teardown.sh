#!/usr/bin/env bash
set -euo pipefail

resourceGroup="${1:-rg-kubapc-dev}"

echo "This deletes every resource in $resourceGroup. Type the resource group name to confirm:"
read -r confirmation
if [ "$confirmation" != "$resourceGroup" ]; then
  echo "Nothing deleted."
  exit 1
fi

az group delete --name "$resourceGroup" --yes --output none
echo "Resource group deletion started. Billing stops once it finishes."
echo "Soft-deleted Key Vaults keep their name for 7 days; purge with: az keyvault purge --name <vault> --location southafricanorth"
