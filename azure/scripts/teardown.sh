#!/usr/bin/env bash
# Deletes resource groups and stops the bills. Run from WSL:
#   azure/scripts/teardown.sh test      deletes rg-kubapc-test
#   azure/scripts/teardown.sh prod      deletes rg-kubapc-prod
#   azure/scripts/teardown.sh shared    deletes rg-kubapc-shared (the hub, the agent VM, the DNS zones)
#   azure/scripts/teardown.sh all       deletes all three
#
# Delete the environments before the shared group: their private endpoints hold
# records in the shared DNS zones and peerings into the hub. A deleted Key Vault
# is kept "soft-deleted" for 7 days and blocks reuse of its name, so an
# environment's vault is purged right after its group is gone. Purge protection
# is deliberately off on this demo so that is possible.
set -euo pipefail

target="${1:?test, prod, shared or all}"
location="southafricanorth"

case "$target" in
  test|prod|shared) resourceGroups=("rg-kubapc-$target") ;;
  all) resourceGroups=(rg-kubapc-test rg-kubapc-prod rg-kubapc-shared) ;;
  *) echo "The argument must be test, prod, shared or all."; exit 1 ;;
esac

echo "This deletes every resource in: ${resourceGroups[*]}"
echo "Type '$target' to confirm:"
read -r confirmation
if [ "$confirmation" != "$target" ]; then
  echo "Nothing deleted."
  exit 1
fi

purgeSoftDeletedVaults() {
  local resourceGroup="$1"
  local vaultNames
  # The vault has already been deleted with its group, so look it up in the
  # soft-deleted list by the group it used to live in.
  vaultNames=$(az keyvault list-deleted --resource-type vault \
    --query "[?contains(properties.vaultId, '/resourceGroups/$resourceGroup/')].name" --output tsv)
  for vaultName in $vaultNames; do
    echo "Purging soft-deleted Key Vault $vaultName so the name can be reused."
    az keyvault purge --name "$vaultName" --location "$location" --output none
  done
}

for resourceGroup in "${resourceGroups[@]}"; do
  if [ "$(az group exists --name "$resourceGroup")" != "true" ]; then
    echo "$resourceGroup does not exist, skipping."
    continue
  fi
  echo "Deleting $resourceGroup (this waits until it is gone, usually a few minutes)."
  az group delete --name "$resourceGroup" --yes --output none
  echo "$resourceGroup deleted."
  if [ "$resourceGroup" != "rg-kubapc-shared" ]; then
    purgeSoftDeletedVaults "$resourceGroup"
  fi
done

echo "Teardown finished. Billing for the deleted groups has stopped."
