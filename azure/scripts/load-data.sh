#!/usr/bin/env bash
# Loads a data dump uploaded by upload-data.sh into an environment's database.
# The kubapc-load-data pipeline runs this inside an AzureCLI@2 task on the agent
# VM; you do not run it by hand.
#   load-data.sh <resource group> <test|prod>
#
# It runs as the pipeline identity, which is the server's Entra ID administrator
# and owns every table, so --disable-triggers in the dump is allowed. The whole
# load is one transaction: either all rows arrive or none do. The blob is deleted
# afterwards, whether or not the load succeeded, so company data never sits in
# storage longer than the pipeline run.
set -euo pipefail

resourceGroup="${1:?resource group}"
environmentName="${2:?test or prod}"
database="kubapc"
sharedResourceGroup="rg-kubapc-shared"
containerName="data-loads-$environmentName"
blobName="$environmentName.sql"

storageAccountName=$(az storage account list --resource-group "$sharedResourceGroup" --query "[?starts_with(name, 'stkubapc')].name | [0]" --output tsv)
dumpFile=$(mktemp /tmp/kubapc-data-XXXXXX)

cleanUp() {
  rm -f "$dumpFile"
  echo "Deleting $containerName/$blobName from storage."
  az storage blob delete --auth-mode login --account-name "$storageAccountName" \
    --container-name "$containerName" --name "$blobName" --output none || echo "Blob was already gone."
}
trap cleanUp EXIT

echo "Downloading $containerName/$blobName from $storageAccountName"
az storage blob download --auth-mode login --account-name "$storageAccountName" \
  --container-name "$containerName" --name "$blobName" --file "$dumpFile" --output none

server=$(az postgres flexible-server list --resource-group "$resourceGroup" --query "[0].name" --output tsv)
fqdn=$(az postgres flexible-server show --resource-group "$resourceGroup" --name "$server" --query fullyQualifiedDomainName --output tsv)

# Same token login as migrate.sh: a short-lived token stands in for the password.
export PGUSER="sc-kubapc-$environmentName"
PGPASSWORD=$(az account get-access-token --resource-type oss-rdbms --query accessToken --output tsv)
export PGPASSWORD
# verify-full: psql checks the server certificate against the system root store and
# that its name matches the host, so a token can never be handed to an impostor.
export PGSSLMODE=verify-full
export PGSSLROOTCERT=/etc/ssl/certs/ca-certificates.crt

echo "Loading data into $fqdn/$database as $PGUSER"
psql --host "$fqdn" --port 5432 --dbname "$database" --set ON_ERROR_STOP=1 --quiet --single-transaction --file "$dumpFile"
echo "Data loaded into $environmentName."
