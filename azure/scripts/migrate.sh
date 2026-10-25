#!/usr/bin/env bash
# Applies the numbered SQL scripts to an environment's database and makes sure
# the application and the agent VM can log in. The pipeline's Database stage runs
# this inside an AzureCLI@2 task on the agent VM; you do not run it by hand.
#   migrate.sh <resource group> <folder with the .sql files> <test|prod>
#
# There is no database password anywhere. The pipeline identity
# (sc-kubapc-<env>) is the server's Entra ID administrator, so it asks Azure for
# a short-lived token and hands that to psql as the password. The agent reaches
# the server over the private endpoint through the peered hub network, so no
# firewall rule is opened or closed here.
set -euo pipefail

resourceGroup="${1:?resource group}"
scriptsFolder="${2:?folder holding the numbered .sql files}"
environmentName="${3:?test or prod}"
database="kubapc"
sharedResourceGroup="rg-kubapc-shared"
agentVirtualMachineName="vm-kubapc-agent"

server=$(az postgres flexible-server list --resource-group "$resourceGroup" --query "[0].name" --output tsv)
fqdn=$(az postgres flexible-server show --resource-group "$resourceGroup" --name "$server" --query fullyQualifiedDomainName --output tsv)

# The administrator login is declared in Bicep (the `administrators` child resource
# of the server) with the login name sc-kubapc-<env>. Fallback, should that resource
# ever fail to deploy (an InternalServerError was reported for service principal
# administrators on an older API version): create the administrator here, before
# connecting, with
#   az postgres flexible-server ad-admin create --resource-group <resource group> --server-name <server> \
#     --display-name sc-kubapc-<env> --object-id <service principal object id> --type ServicePrincipal
# and nothing else in this script needs to change.
export PGUSER="sc-kubapc-$environmentName"
PGPASSWORD=$(az account get-access-token --resource-type oss-rdbms --query accessToken --output tsv)
export PGPASSWORD
# verify-full: psql checks the server certificate against the system root store and
# that its name matches the host, so a token can never be handed to an impostor.
export PGSSLMODE=verify-full
export PGSSLROOTCERT=/etc/ssl/certs/ca-certificates.crt
psqlArgs=(--host "$fqdn" --port 5432 --dbname "$database" --set ON_ERROR_STOP=1 --quiet)

# Right after a deployment the server can still be finishing a configuration
# change, or the private DNS record can be seconds old. Give it a little time.
for attempt in $(seq 1 12); do
  if psql "${psqlArgs[@]}" --command "SELECT 1" > /dev/null 2>&1; then
    break
  fi
  if [ "$attempt" = "12" ]; then
    echo "Could not connect to $fqdn as $PGUSER after 12 attempts."
    exit 1
  fi
  echo "Waiting for the database to accept connections ($attempt)."
  sleep 10
done
echo "Connected to $fqdn as $PGUSER."

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

# An Entra ID login is a PostgreSQL role whose name is the identity's display name
# and whose identity is its object id. Creating it is a one-off; the GRANT is
# repeated harmlessly on every run so a lost grant heals itself.
grantApplicationLogin() {
  local loginName="$1" objectId="$2"
  local roleCount
  roleCount=$(psql "${psqlArgs[@]}" --tuples-only --no-align --command "SELECT count(*) FROM pg_roles WHERE rolname = '$loginName'")
  if [ "$roleCount" = "0" ]; then
    echo "Creating Entra ID login: $loginName"
    psql "${psqlArgs[@]}" --command "SELECT * FROM pgaadauth_create_principal_with_oid('$loginName', '$objectId', false, false)" > /dev/null
  else
    echo "Entra ID login exists: $loginName"
  fi
  psql "${psqlArgs[@]}" --command "GRANT kubapc_app TO \"$loginName\""
}

# The API connects as its own managed identity, whose login name is the App Service name.
apiName=$(az webapp list --resource-group "$resourceGroup" --query "[0].name" --output tsv)
apiPrincipalId=$(az webapp list --resource-group "$resourceGroup" --query "[0].identity.principalId" --output tsv)
grantApplicationLogin "$apiName" "$apiPrincipalId"

# The agent VM's identity gets the same application-level access for the two
# operator tasks that remain (seed the first SuperAdmin, read tables), and no more.
agentPrincipalId=$(az vm show --resource-group "$sharedResourceGroup" --name "$agentVirtualMachineName" --query identity.principalId --output tsv)
grantApplicationLogin "$agentVirtualMachineName" "$agentPrincipalId"

echo "Database scripts applied and application logins granted."
