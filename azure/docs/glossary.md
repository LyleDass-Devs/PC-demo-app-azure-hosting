# Glossary

**Agent.** The machine that runs pipeline steps. This project uses a self-hosted agent registered in the `wsl-agents` pool.

**App Service / App Service plan.** Azure's managed web host and the machine size it runs on. The API runs on a Linux B1 plan.

**Approval.** A check on an Azure DevOps environment that requires a named person to approve before a deployment stage runs.

**ARM template.** The JSON format that Bicep compiles into and that Azure Resource Manager deploys.

**Artifact.** Files published by one pipeline stage and downloaded by a later one.

**Bicep.** Azure's declarative infrastructure language. A template describes the end state; Azure computes the changes.

**Break-glass.** A temporary, logged exception to a network rule. `migrate.sh` enables public access to PostgreSQL for one IP address for the duration of the migration and closes it afterwards.

**Environment (Azure DevOps).** A named deployment target with checks attached.

**Idempotent.** Applying the same template repeatedly yields the same result; re-running changes nothing.

**Key Vault.** Secret storage with RBAC permissions and an audit log. Secrets here: the database connection string and the JWT signing key.

**Key Vault reference.** An App Service setting of the form `@Microsoft.KeyVault(SecretUri=...)`, resolved at start-up with the app's managed identity.

**Managed identity.** An Entra ID identity attached to an Azure resource, used to authenticate to other services without a stored credential.

**Microsoft Entra ID.** The identity directory behind Azure.

**Network security group.** Allow and deny rules attached to a subnet.

**Private DNS zone.** A DNS zone linked to a VNet that resolves a service's host name to its private endpoint address.

**Private endpoint.** A network interface in a subnet that fronts a platform service, allowing the service's public access to be disabled.

**RBAC.** Role-based access control: a role granted to an identity at a scope.

**Resource group.** The container for all resources of one environment; deleting it deletes them.

**Service connection.** The pipeline's credential to an external system. The Azure connection uses workload identity federation.

**Smoke test.** A post-deployment check that the service answers and the expected protections are present.

**Static Web Apps.** Hosting for static sites, used for the Blazor client.

**Variable group.** Named pipeline values; secret values are masked.

**VNet integration.** The App Service feature that routes the app's outbound traffic through a subnet.

**What-if.** `az deployment group what-if`: the preview of changes a template would make.

**Workload identity federation.** Short-lived, federated tokens between Azure DevOps and Entra ID in place of a client secret.
