#!/usr/bin/env bash
# Installs the Azure DevOps agent on vm-kubapc-agent. Run from the repository
# root in WSL, once, after deploy-shared.sh and after creating the agent pool
# and a personal access token (scope: Agent Pools, read and manage).
#
# The VM accepts no inbound traffic, not even SSH, so the installation is sent
# through `az vm run-command`, which Azure delivers over its own management
# channel and records in the activity log. The script that actually runs on the
# VM is azure/scripts/agent-vm-install.sh.
set -euo pipefail

organisation="${1:?Azure DevOps organisation name, the part after dev.azure.com/}"
token="${2:?personal access token with Agent Pools read and manage}"
pool="${3:-vnet-agents}"
resourceGroup="rg-kubapc-shared"
virtualMachineName="vm-kubapc-agent"

echo "Installing the agent on $virtualMachineName for https://dev.azure.com/$organisation, pool $pool."
echo "This takes about ten minutes; the command returns when the VM has finished."

# The command's output comes back as a JSON array of messages. We print each one
# so anything the installer said (stdout and stderr) is visible here.
az vm run-command invoke \
  --resource-group "$resourceGroup" \
  --name "$virtualMachineName" \
  --command-id RunShellScript \
  --scripts @azure/scripts/agent-vm-install.sh \
  --parameters "$organisation" "$token" "$pool" \
  --query "value[].message" \
  --output tsv

echo
echo "Check the agent is online: Azure DevOps > Organization settings > Agent pools > $pool."
echo "The token has done its job. Revoke it now: User settings > Personal access tokens."
