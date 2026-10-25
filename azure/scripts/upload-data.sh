#!/usr/bin/env bash
# Takes a data-only dump of your local KubaPC database and uploads it to the
# shared data-loads-<environment> container as <environment>.sql, ready for the
# kubapc-load-data pipeline to apply. Run from WSL:
#   azure/scripts/upload-data.sh test
# Local connection settings come from LOCAL_PG_HOST, LOCAL_PG_PORT,
# LOCAL_PG_DATABASE, LOCAL_PG_USER and LOCAL_PG_PASSWORD.
#
# Your laptop cannot reach the database (it is only reachable from inside the
# Azure network), so the dump travels through storage and the pipeline, which
# runs on the agent VM, applies it. The upload uses your Entra ID login; the
# storage account accepts nothing else.
set -euo pipefail

environmentName="${1:?test or prod}"
case "$environmentName" in
  test|prod) ;;
  *) echo "The first argument must be test or prod."; exit 1 ;;
esac

localHost="${LOCAL_PG_HOST:-localhost}"
localPort="${LOCAL_PG_PORT:-5432}"
localDatabase="${LOCAL_PG_DATABASE:-kubapc}"
localUser="${LOCAL_PG_USER:-postgres}"
: "${LOCAL_PG_PASSWORD:?set LOCAL_PG_PASSWORD to your local PostgreSQL password}"
sharedResourceGroup="rg-kubapc-shared"
containerName="data-loads-$environmentName"
blobName="$environmentName.sql"

dumpFile=$(mktemp /tmp/kubapc-data-XXXXXX)
trap 'rm -f "$dumpFile"' EXIT

# Data only: the schema comes from the migration scripts, which the pipeline has
# already applied. schema_migrations and error_logs belong to the target, not to
# the laptop.
echo "Dumping data from $localHost:$localPort/$localDatabase"
PGPASSWORD="$LOCAL_PG_PASSWORD" pg_dump --host "$localHost" --port "$localPort" --username "$localUser" --dbname "$localDatabase" \
  --data-only --no-owner --no-privileges --disable-triggers \
  --exclude-table=schema_migrations --exclude-table=error_logs \
  --file "$dumpFile"

storageAccountName=$(az storage account list --resource-group "$sharedResourceGroup" --query "[?starts_with(name, 'stkubapc')].name | [0]" --output tsv)
if [ -z "$storageAccountName" ]; then
  echo "No storage account found in $sharedResourceGroup. Run deploy-shared.sh first."
  exit 1
fi

echo "Uploading to $storageAccountName/$containerName/$blobName"
az storage blob upload --auth-mode login --account-name "$storageAccountName" \
  --container-name "$containerName" --name "$blobName" --file "$dumpFile" --overwrite --output none

echo "Uploaded. Now run the kubapc-load-data pipeline for $environmentName"
