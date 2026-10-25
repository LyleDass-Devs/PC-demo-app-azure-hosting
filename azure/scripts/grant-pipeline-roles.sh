#!/usr/bin/env bash
# Grants one environment's pipeline identity the few permissions its service
# connection does not give it. Run from WSL once per environment, after the
# service connection sc-kubapc-<env> exists:
#   azure/scripts/grant-pipeline-roles.sh test <application (client) id>
# The client id is shown on the service connection's page in Azure DevOps.
#
# The service connection is Contributor on its own resource group and nothing
# else. The environment's Bicep also has to: assign the API's managed identity
# the right to read vault secrets (needs a role-assignment role, limited here to
# that single role), create its side of the peering on the hub network, write
# private endpoint records into the shared DNS zones, read the shared group, and
# fetch and delete data dumps from its own data-loads container. Each grant is
# printed before it is made so you can read exactly what the pipeline may do.
#
# The peering and DNS grants are made on the whole shared resource group rather
# than on the hub network and the zones one by one: a Bicep module that targets
# another resource group is itself a deployment in that group, and writing a
# deployment there is a permission at group level that the narrower scopes
# would not give.
set -euo pipefail

environmentName="${1:?test or prod}"
clientId="${2:?application (client) id of the sc-kubapc-<env> service connection}"
case "$environmentName" in
  test|prod) ;;
  *) echo "The first argument must be test or prod."; exit 1 ;;
esac

# Role assignments need the service principal's object id, which differs from the
# application id shown in Azure DevOps. Look it up once and use it everywhere.
principalObjectId=$(az ad sp show --id "$clientId" --query id --output tsv)
echo "Service principal sc-kubapc-$environmentName has object id $principalObjectId."

subscriptionId=$(az account show --query id --output tsv)
sharedResourceGroupScope="/subscriptions/$subscriptionId/resourceGroups/rg-kubapc-shared"
environmentResourceGroupScope="/subscriptions/$subscriptionId/resourceGroups/rg-kubapc-$environmentName"
storageAccountName=$(az storage account list --resource-group rg-kubapc-shared --query "[?starts_with(name, 'stkubapc')].name | [0]" --output tsv)
if [ -z "$storageAccountName" ]; then
  echo "No storage account found in rg-kubapc-shared. Run deploy-shared.sh first."
  exit 1
fi
dataLoadsContainerScope="$sharedResourceGroupScope/providers/Microsoft.Storage/storageAccounts/$storageAccountName/blobServices/default/containers/data-loads-$environmentName"

# 4633458b-17de-408a-b874-0445c86b69e6 is the built-in role Key Vault Secrets User.
# The condition below means "you may create or delete a role assignment only if it
# is for this one role", so the pipeline can never grant itself or anything else a
# wider role.
roleAssignmentCondition="((!(ActionMatches{'Microsoft.Authorization/roleAssignments/write'})) OR (@Request[Microsoft.Authorization/roleAssignments:RoleDefinitionId] ForAnyOfAnyValues:GuidEquals {4633458b-17de-408a-b874-0445c86b69e6})) AND ((!(ActionMatches{'Microsoft.Authorization/roleAssignments/delete'})) OR (@Resource[Microsoft.Authorization/roleAssignments:RoleDefinitionId] ForAnyOfAnyValues:GuidEquals {4633458b-17de-408a-b874-0445c86b69e6}))"

grantRole() {
  local roleName="$1" scope="$2" condition="${3:-}"
  echo "Granting '$roleName' to sc-kubapc-$environmentName at $scope"
  local existingAssignment
  existingAssignment=$(az role assignment list --assignee "$principalObjectId" --role "$roleName" --scope "$scope" --query "[0].id" --output tsv)
  if [ -n "$existingAssignment" ]; then
    echo "  already granted."
    return
  fi
  if [ -n "$condition" ]; then
    az role assignment create --assignee-object-id "$principalObjectId" --assignee-principal-type ServicePrincipal \
      --role "$roleName" --scope "$scope" --condition "$condition" --condition-version "2.0" --output none
  else
    az role assignment create --assignee-object-id "$principalObjectId" --assignee-principal-type ServicePrincipal \
      --role "$roleName" --scope "$scope" --output none
  fi
  echo "  done."
}

grantRole "Role Based Access Control Administrator" "$environmentResourceGroupScope" "$roleAssignmentCondition"
grantRole "Network Contributor" "$sharedResourceGroupScope"
grantRole "Private DNS Zone Contributor" "$sharedResourceGroupScope"
grantRole "Reader" "$sharedResourceGroupScope"
# Contributor rather than Reader on the container: the data-load pipeline deletes
# the dump after applying it, and Reader cannot delete blobs. Only this
# environment's container, so test can never read a prod dump.
grantRole "Storage Blob Data Contributor" "$dataLoadsContainerScope"

echo
echo "All roles granted for $environmentName. Role assignments can take a few minutes to take effect."
echo "Paste this into azure/infra/$environmentName.bicepparam as pipelinePrincipalObjectId: $principalObjectId"
