# Glossary

**ABAC / conditioned role assignment.** Attribute-based access control: a role assignment with a condition attached. The pipeline's Role Based Access Control Administrator role is conditioned so it can only assign or remove Key Vault Secrets User, never a wider role.

**Agent.** The machine that runs pipeline steps. This project uses a self-hosted agent on a Linux VM in the hub, registered in the `vnet-agents` pool.

**App Service / App Service plan.** Azure's managed web host and the machine size it runs on. The API runs on a Linux B1 plan.

**Approval.** A check on an Azure DevOps environment that requires a named person to approve before a deployment stage runs. Only the `prod` environments carry one.

**ARM template.** The JSON format that Bicep compiles into and that Azure Resource Manager deploys.

**Artifact.** Files published by one pipeline stage and downloaded by a later one.

**Bicep.** Azure's declarative infrastructure language. A template describes the end state; Azure computes the changes.

**Deployment template.** `azure/pipelines/templates/deploy-environment.yml`: one pipeline definition of the three deployment stages (infrastructure, database, application), called once for `test` and once for `prod` with different parameters.

**Entra ID authentication to PostgreSQL.** Logging in to the database with an Entra ID identity and a token instead of a password. Password login is switched off on every server, so no database password exists.

**Environment (Azure DevOps).** A named deployment target with checks attached.

**Hub and spoke.** A network layout with one central network (the hub) and one network per environment (the spokes), each connected to the hub. Here the hub holds the agent VM, the private DNS zones and the storage account; the spokes are `vnet-kubapc-test` and `vnet-kubapc-prod`.

**Idempotent.** Applying the same template repeatedly yields the same result; re-running changes nothing.

**Key Vault.** Secret storage with RBAC permissions and an audit log. The one secret kept here is the token signing key.

**Key Vault reference.** An App Service setting of the form `@Microsoft.KeyVault(SecretUri=...)`, resolved at start-up with the app's managed identity.

**Managed identity.** An Entra ID identity attached to an Azure resource, used to authenticate to other services without a stored credential. The API and the agent VM each have one, and each is also a login on the database.

**Microsoft Entra ID.** The identity directory behind Azure.

**NAT gateway.** An Azure service that gives a subnet's outbound traffic a fixed address and keeps resources behind it unreachable from outside. The production answer for the agent's internet access; here the VM has a public IP with all inbound traffic denied instead.

**Network security group.** Allow and deny rules attached to a subnet.

**Peering.** A two-way connection between two virtual networks so resources in each can reach the other by private address. Each spoke is peered with the hub; the spokes are not peered with each other.

**Private DNS zone.** A DNS zone linked to a VNet that resolves a service's host name to its private endpoint address. The two zones live in the hub and are linked to every network.

**Private endpoint.** A network interface in a subnet that fronts a platform service, allowing the service's public access to be disabled.

**Promotion.** Moving a change from `test` to `prod`: the same code and templates, deployed to `prod` only after they passed in `test`, with an approval at each stage.

**RBAC.** Role-based access control: a role granted to an identity at a scope.

**Resource group.** The container for related resources; deleting it deletes them. There are three: shared, test and prod.

**Run-command.** `az vm run-command`: Azure runs a script on a VM through its management channel, with no network connection to the VM and a record in the activity log. It is how the agent is installed and how operator tasks reach the VM.

**Service connection.** The pipeline's credential to an external system. The Azure connections use workload identity federation, one per resource group.

**Smoke test.** A post-deployment check that the service answers and the expected protections are present.

**Static Web Apps.** Hosting for static sites, used for the Blazor client.

**Token.** A short-lived proof of identity issued by Entra ID. The API and the pipeline send one to PostgreSQL in place of a password; it expires within the hour and is refreshed automatically.

**Variable group.** Named pipeline values; secret values are masked. Each environment has one holding `JWT_SECRET`.

**VNet integration.** The App Service feature that routes the app's outbound traffic through a subnet.

**What-if.** `az deployment group what-if`: the preview of changes a template would make.

**Workload identity federation.** Short-lived, federated tokens between Azure DevOps and Entra ID in place of a client secret.
