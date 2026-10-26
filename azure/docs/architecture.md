# Architecture

KubaPC runs on Azure in South Africa North, in three resource groups: one shared hub and two environments, `test` and `prod`. The environments are built from the same Bicep in `azure/infra` by the pipeline in `azure/pipelines`, each with its own parameter file. The hub is built by hand from `azure/infra/shared`, because the pipeline agent lives in it.

```
 rg-kubapc-shared  (hub, created by deploy-shared.sh)
 +--------------------------------------------------------------------------+
 |  vnet-kubapc-hub 10.10.0.0/16                                            |
 |    snet-agents 10.10.1.0/24, NSG: deny all inbound                       |
 |      vm-kubapc-agent (Ubuntu, B2s, managed identity)                     |
 |      pipeline agent in pool vnet-agents, outbound via a public IP        |
 |                                                                          |
 |  Private DNS zones, linked to the hub and to every spoke:                |
 |    privatelink.postgres.database.azure.com                               |
 |    privatelink.vaultcore.azure.net                                       |
 |                                                                          |
 |  Storage account stkubapc..., Entra ID access only                       |
 |    containers data-loads-test, data-loads-prod                           |
 +------------------+-------------------------------+-----------------------+
                    | peering (both ways)           | peering (both ways)
                    |                               |
 rg-kubapc-test     |                rg-kubapc-prod |   (identical shape)
 +------------------+---------------+  +------------+---------------------+
 | vnet-kubapc-test 10.20.0.0/16    |  | vnet-kubapc-prod 10.30.0.0/16    |
 |   snet-app 10.20.1.0/24          |  |   snet-app 10.30.1.0/24          |
 |   snet-private-endpoints         |  |   snet-private-endpoints         |
 |     10.20.2.0/24                 |  |     10.30.2.0/24                 |
 +----------------------------------+  +----------------------------------+

 Inside each environment's resource group:

         internet
            |
   +--------+-----------------------------+
   |                                      |
 Static Web Apps (Free, eastus2)    App Service B1 (Linux)
 Blazor Admin client                  KubaPC.API, managed identity
   |  browser calls the API           HTTPS only, TLS 1.2, health check /health
   +------------------------------->      | VNet integration into snet-app
                                           |
                       +-------------------+-------------------+
                       | private endpoint                      | private endpoint
              PostgreSQL Flexible Server B1ms         Key Vault (RBAC, public access off)
              public access off, SSL required         KubaPC-JwtSecret
              Entra ID login only, no password
              database kubapc

   Log Analytics workspace  <-- diagnostics from App Service, Key Vault, PostgreSQL
   Application Insights     <-- request telemetry from the API
```

## Request flow

1. The browser loads the Blazor client from Static Web Apps.
2. The client calls the API on App Service over HTTPS. App Service terminates TLS and forwards the request with `X-Forwarded-*` headers, which the API honours through `ASPNETCORE_FORWARDEDHEADERS_ENABLED`.
3. The token signing key is an App Service setting that is a Key Vault reference. App Service resolves it at start-up with the API's managed identity, which holds Key Vault Secrets User on that environment's vault. The vault has public access disabled, so the lookup goes through its private endpoint.
4. The API connects to PostgreSQL by host name with no password. It asks Azure for a short-lived token for its managed identity, uses the token as the password, and refreshes it every 50 minutes. The login name is the App Service name. The connection string holds no secret and is an ordinary App Service setting.
5. The private DNS zones are linked to the environment's network, so the host name resolves to the private endpoint address. The server's public access is disabled.

## Delivery flow

1. The pipeline runs on the agent in the hub and signs in to Azure as the environment's service connection (`sc-kubapc-test` or `sc-kubapc-prod`).
2. Bicep creates the environment, including its side of the peering, the hub's side, the links from the shared DNS zones to the environment's network, and the private endpoints' records in those zones.
3. The database stage connects to the private endpoint through the peering. The service connection is the server's Entra ID administrator, so it asks for a token and runs the SQL scripts as the role that owns every table. It then creates the API's login and the agent VM's login and grants both membership of `kubapc_app`.
4. `test` runs without approval. `prod` is in a run only when the run is started with Deploy prod ticked, and each of its three Azure DevOps environments waits for approval.

## Decisions

| Area | Choice | Reason |
|---|---|---|
| Environments | `test` and `prod`: own resource group, network, database, vault, API and site | Same templates twice; a change is proven in `test` before `prod` |
| Network shape | Hub and spoke: one hub, one spoke per environment, peered both ways | One agent reaches both environments' private endpoints by name |
| DNS | Two private DNS zones in the hub, linked to every network | Two zones with the same name cannot both serve one network, so the zones are shared |
| Compute | App Service Linux B1 | Smallest size with VNet integration and diagnostics; runs .NET 10 without a container image |
| Client | Static Web Apps Free | The published client is static files; no secrets; free |
| Database | PostgreSQL Flexible Server 17, B1ms, public access disabled, private endpoint | Matches the application; can be stopped when idle; never reachable from the internet |
| Database login | Entra ID only, password login off | No database password exists anywhere; tokens expire within the hour |
| Secrets | Key Vault holds the token signing key only; RBAC, public access disabled, private endpoint, soft delete | One audited place for the one remaining secret |
| Pipeline agent | Linux VM in the hub, no inbound traffic, operated through run-command | Reaches private endpoints without opening a firewall; nothing to connect to |
| Data load | Dump uploaded to storage, loaded by a manually started pipeline on the agent | Same approval and log as every other database change |
| Infrastructure as code | Bicep modules per concern, one parameter file per environment, what-if on every change | Declarative, idempotent, no state file |
| Delivery | Azure DevOps YAML, one deployment template used for both environments | Same three gates per environment, approvals on `prod` only |
| Monitoring | Log Analytics with a 1 GB daily cap, Application Insights | Diagnostics from every resource at no cost at this size |

## Identity and access

| Identity | Roles and rights | Scope |
|---|---|---|
| Lyle (operator) | Subscription owner; Storage Blob Data Contributor | The subscription; both data-load containers |
| `sc-kubapc-shared` | Contributor, given by the service connection | `rg-kubapc-shared` |
| `sc-kubapc-<env>` | Contributor, given by the service connection | `rg-kubapc-<env>` |
| `sc-kubapc-<env>` | Role Based Access Control Administrator, conditioned to the single role Key Vault Secrets User | `rg-kubapc-<env>` |
| `sc-kubapc-<env>` | Network Contributor, Private DNS Zone Contributor, Reader | `rg-kubapc-shared` |
| `sc-kubapc-<env>` | Storage Blob Data Contributor | Its own container only, `data-loads-<env>` |
| `sc-kubapc-<env>` | Entra ID administrator of that environment's PostgreSQL server | The server |
| API managed identity | Key Vault Secrets User; login on PostgreSQL, member of `kubapc_app` | That environment's vault and database |
| Agent VM managed identity | No Azure roles; login on each PostgreSQL server, member of `kubapc_app` | Both databases |

Network Contributor and Private DNS Zone Contributor are granted on the whole shared resource group, not on the one network and two zones, because a deployment that writes into another resource group needs permission to create deployments there.

## Production hardening not applied here

| Control | Production | Here |
|---|---|---|
| Web application firewall and private inbound on App Service | Front Door Premium with WAF and Private Link | Public App Service endpoint with the API's own rate limiting and headers |
| Agent outbound traffic | NAT gateway or firewall | Standard public IP on the VM, no inbound rules |
| Key Vault purge protection | On | Off, so an environment can be removed and recreated quickly |
| Environments and guardrails | A subscription per environment with Azure Policy and Defender for Cloud | One subscription, resource groups as the boundary |
| Approvals | Separate approvers per gate, no self-approval | One approver, who is also the author |

## Cost

Pay-as-you-go, South Africa North, October 2026.

| Resource | Running, per day | Stopped, per day |
|---|---|---|
| Agent VM with disk and public IP | about R25 | about R5 (deallocated) |
| Per environment: App Service plan B1 | about R8 | about R8 |
| Per environment: PostgreSQL B1ms | about R10 | R0 (no compute charge) |
| Per environment: two private endpoints | about R9 | about R9 |
| Per environment: Key Vault, DNS zones, VNet, logs, Static Web Apps | under R2 | under R2 |

With the VM and one environment running the total is about R54 per day; with both environments about R83. With everything stopped it is about R24 for one environment and about R43 for both.

`azure/scripts/stop-start.sh stop <test|prod|agent>` removes the database or VM compute charge between uses. `azure/scripts/teardown.sh` deletes resource groups and ends their charges.
