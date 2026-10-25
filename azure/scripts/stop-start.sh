#!/usr/bin/env bash
# Stops or starts the parts of the estate that cost money while idle. Run from WSL:
#   azure/scripts/stop-start.sh stop test     stops the test database
#   azure/scripts/stop-start.sh start prod    starts the prod database
#   azure/scripts/stop-start.sh stop agent    deallocates the agent VM
#
# A stopped database is not billed for compute (Azure restarts it by itself
# after 7 days). A deallocated VM is not billed at all except for its disk and
# public IP; the agent service starts again on its own when the VM starts.
set -euo pipefail

action="${1:?stop or start}"
target="${2:?test, prod or agent}"
case "$action" in
  stop|start) ;;
  *) echo "The first argument must be stop or start."; exit 1 ;;
esac

if [ "$target" = "agent" ]; then
  # deallocate, not stop: a stopped-but-allocated VM keeps being billed.
  if [ "$action" = "stop" ]; then
    az vm deallocate --resource-group rg-kubapc-shared --name vm-kubapc-agent --output none
    echo "Agent VM deallocated. Billing for its compute has stopped; pipelines will not run until you start it."
  else
    az vm start --resource-group rg-kubapc-shared --name vm-kubapc-agent --output none
    echo "Agent VM started. Give the agent a minute to show as online in the vnet-agents pool."
  fi
  exit 0
fi

case "$target" in
  test|prod) ;;
  *) echo "The second argument must be test, prod or agent."; exit 1 ;;
esac

resourceGroup="rg-kubapc-$target"
server=$(az postgres flexible-server list --resource-group "$resourceGroup" --query "[0].name" --output tsv)
az postgres flexible-server "$action" --resource-group "$resourceGroup" --name "$server" --output none
echo "PostgreSQL server $server: $action requested. A stopped server is not billed for compute and restarts itself after 7 days."
