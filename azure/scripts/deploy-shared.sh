#!/usr/bin/env bash
# Deploys the shared hub: the hub network, the agent VM, the two private DNS
# zones and the storage account for data loads. Run from the repository root in
# WSL after bootstrap.sh, and again whenever azure/infra/shared changes.
#
# The pipeline cannot do this first deployment because the pipeline agent is the
# VM this script creates. What-if runs first so you see every change before it
# happens; the deployment only proceeds if you confirm.
set -euo pipefail

resourceGroup="rg-kubapc-shared"
templateFile="azure/infra/shared/main.bicep"
parameterFile="azure/infra/shared/shared.bicepparam"
deploymentName="shared-$(date +%Y%m%d-%H%M%S)"
sshKeyFile="$HOME/.ssh/id_ed25519"

# The VM must be created with an SSH key even though the network blocks SSH
# (operator access is through run-command). The private half never leaves this laptop.
if [ ! -f "$sshKeyFile" ]; then
  echo "Creating an SSH key pair at $sshKeyFile for the agent VM."
  mkdir -p "$HOME/.ssh"
  ssh-keygen -t ed25519 -N "" -f "$sshKeyFile" -C "kubapc-agent" > /dev/null
fi
AGENT_SSH_PUBLIC_KEY="$(cat "$sshKeyFile.pub")"
export AGENT_SSH_PUBLIC_KEY

echo "Preview of changes to $resourceGroup:"
az deployment group what-if \
  --resource-group "$resourceGroup" \
  --template-file "$templateFile" \
  --parameters "$parameterFile" \
  --result-format FullResourcePayloads

echo
echo "Apply these changes? Type yes to continue:"
read -r confirmation
if [ "$confirmation" != "yes" ]; then
  echo "Nothing deployed."
  exit 1
fi

az deployment group create \
  --name "$deploymentName" \
  --resource-group "$resourceGroup" \
  --template-file "$templateFile" \
  --parameters "$parameterFile" \
  --output none
echo "Deployment $deploymentName finished."

readOutput() {
  az deployment group show --resource-group "$resourceGroup" --name "$deploymentName" \
    --query "properties.outputs.$1.value" --output tsv
}

storageAccountName=$(readOutput storageAccountName)

# You upload data dumps with your own account, so you need the data-plane role
# on the storage account (both containers). Owner of the subscription is not
# enough: blob data access is a separate permission because shared-key access is
# switched off.
subscriptionId=$(az account show --query id --output tsv)
signedInUserObjectId=$(az ad signed-in-user show --query id --output tsv)
storageAccountScope="/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Storage/storageAccounts/$storageAccountName"
existingAssignment=$(az role assignment list --assignee "$signedInUserObjectId" --role "Storage Blob Data Contributor" --scope "$storageAccountScope" --query "[0].id" --output tsv)
if [ -z "$existingAssignment" ]; then
  az role assignment create --assignee-object-id "$signedInUserObjectId" --assignee-principal-type User \
    --role "Storage Blob Data Contributor" --scope "$storageAccountScope" --output none
  echo "Granted you Storage Blob Data Contributor on $storageAccountName."
else
  echo "You already have Storage Blob Data Contributor on $storageAccountName."
fi

echo
echo "Deployment outputs:"
printf "%-22s %s\n" "Output" "Value"
printf "%-22s %s\n" "----------------------" "-----"
for outputName in agentVmName agentPrincipalId agentPublicIp storageAccountName hubVnetId postgresDnsZoneId keyVaultDnsZoneId; do
  printf "%-22s %s\n" "$outputName" "$(readOutput "$outputName")"
done
echo
echo "Next: azure/scripts/agent-vm-setup.sh <organisation> <personal access token>"
