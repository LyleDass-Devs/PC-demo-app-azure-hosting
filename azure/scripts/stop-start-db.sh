#!/usr/bin/env bash
set -euo pipefail

action="${1:?stop or start}"
resourceGroup="${2:-rg-kubapc-dev}"

server=$(az postgres flexible-server list --resource-group "$resourceGroup" --query "[0].name" --output tsv)
az postgres flexible-server "$action" --resource-group "$resourceGroup" --name "$server" --output none
echo "PostgreSQL server $server: $action requested. A stopped server is not billed for compute and restarts itself after 7 days."
