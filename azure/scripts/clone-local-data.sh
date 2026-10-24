#!/usr/bin/env bash
set -euo pipefail

resourceGroup="${1:-rg-kubapc-dev}"
localHost="${LOCAL_PG_HOST:-localhost}"
localPort="${LOCAL_PG_PORT:-5432}"
localDatabase="${LOCAL_PG_DATABASE:-kubapc}"
localUser="${LOCAL_PG_USER:-postgres}"
: "${LOCAL_PG_PASSWORD:?set LOCAL_PG_PASSWORD to your local PostgreSQL password}"
: "${POSTGRES_ADMIN_PASSWORD:?set POSTGRES_ADMIN_PASSWORD to the Azure administrator password}"

dump="/tmp/kubapc-data.sql"
server=$(az postgres flexible-server list --resource-group "$resourceGroup" --query "[0].name" --output tsv)
fqdn=$(az postgres flexible-server show --resource-group "$resourceGroup" --name "$server" --query fullyQualifiedDomainName --output tsv)
adminLogin=$(az postgres flexible-server show --resource-group "$resourceGroup" --name "$server" --query administratorLogin --output tsv)
myIp=$(curl -s https://api.ipify.org)
ruleName="clone-local-data"

echo "Dumping data from $localHost:$localPort/$localDatabase"
PGPASSWORD="$LOCAL_PG_PASSWORD" pg_dump --host "$localHost" --port "$localPort" --username "$localUser" --dbname "$localDatabase" \
  --data-only --no-owner --no-privileges --disable-triggers \
  --exclude-table=schema_migrations --exclude-table=error_logs \
  --file "$dump"

closeWindow() {
  echo "Closing the break-glass window."
  az postgres flexible-server firewall-rule delete --resource-group "$resourceGroup" --name "$server" --rule-name "$ruleName" --yes --output none || true
  az postgres flexible-server update --resource-group "$resourceGroup" --name "$server" --public-access Disabled --output none
  rm -f "$dump"
}
trap closeWindow EXIT

echo "Opening the break-glass window for $myIp."
az postgres flexible-server update --resource-group "$resourceGroup" --name "$server" --public-access Enabled --output none
az postgres flexible-server firewall-rule create --resource-group "$resourceGroup" --name "$server" --rule-name "$ruleName" --start-ip-address "$myIp" --end-ip-address "$myIp" --output none

export PGPASSWORD="$POSTGRES_ADMIN_PASSWORD"
export PGSSLMODE=require
for attempt in $(seq 1 12); do
  if psql --host "$fqdn" --username "$adminLogin" --dbname kubapc --command "SELECT 1" > /dev/null 2>&1; then
    break
  fi
  echo "Waiting for the firewall rule ($attempt)."
  sleep 10
done

echo "Loading data into Azure."
psql --host "$fqdn" --username "$adminLogin" --dbname kubapc --set ON_ERROR_STOP=1 --quiet --single-transaction --file "$dump"
echo "Done. Your local operators, contracts, routes, trips, accounts and penalties now exist in Azure."
