#!/usr/bin/env bash
set -euo pipefail

resourceGroup="${1:?resource group}"
scriptsFolder="${2:?folder holding the numbered .sql files}"
: "${POSTGRES_ADMIN_PASSWORD:?set POSTGRES_ADMIN_PASSWORD}"
: "${APP_DB_PASSWORD:?set APP_DB_PASSWORD}"

server=$(az postgres flexible-server list --resource-group "$resourceGroup" --query "[0].name" --output tsv)
fqdn=$(az postgres flexible-server show --resource-group "$resourceGroup" --name "$server" --query fullyQualifiedDomainName --output tsv)
adminLogin=$(az postgres flexible-server show --resource-group "$resourceGroup" --name "$server" --query administratorLogin --output tsv)
database="kubapc"
agentIp=$(curl -s https://api.ipify.org)
ruleName="pipeline-break-glass"

closeWindow() {
  echo "Closing the break-glass window."
  az postgres flexible-server firewall-rule delete --resource-group "$resourceGroup" --name "$server" --rule-name "$ruleName" --yes --output none || true
  az postgres flexible-server update --resource-group "$resourceGroup" --name "$server" --public-access Disabled --output none
}
trap closeWindow EXIT

echo "Opening the break-glass window for $agentIp."
az postgres flexible-server update --resource-group "$resourceGroup" --name "$server" --public-access Enabled --output none
az postgres flexible-server firewall-rule create --resource-group "$resourceGroup" --name "$server" --rule-name "$ruleName" --start-ip-address "$agentIp" --end-ip-address "$agentIp" --output none

export PGPASSWORD="$POSTGRES_ADMIN_PASSWORD"
export PGSSLMODE=require
psqlArgs=(--host "$fqdn" --port 5432 --username "$adminLogin" --dbname "$database" --set ON_ERROR_STOP=1 --quiet)

for attempt in $(seq 1 12); do
  if psql "${psqlArgs[@]}" --command "SELECT 1" > /dev/null 2>&1; then
    break
  fi
  echo "Waiting for the firewall rule to take effect ($attempt)."
  sleep 10
done

psql "${psqlArgs[@]}" --command "CREATE TABLE IF NOT EXISTS schema_migrations (file_name text PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now());"

for file in $(ls "$scriptsFolder"/*.sql | sort); do
  name=$(basename "$file")
  applied=$(psql "${psqlArgs[@]}" --tuples-only --no-align --command "SELECT count(*) FROM schema_migrations WHERE file_name = '$name'")
  if [ "$applied" = "1" ]; then
    echo "Already applied: $name"
    continue
  fi
  echo "Applying: $name"
  psql "${psqlArgs[@]}" --single-transaction --file "$file"
  psql "${psqlArgs[@]}" --command "INSERT INTO schema_migrations (file_name) VALUES ('$name')"
done

psql "${psqlArgs[@]}" --set app_password="$APP_DB_PASSWORD" --command "ALTER ROLE kubapc_app WITH PASSWORD :'app_password';"
echo "Application login password set."
