# Architecture

KubaPC runs on Azure in a single resource group, `rg-kubapc-dev`, in South Africa North. Everything is created from the Bicep in `azure/infra` by the pipeline in `azure/pipelines`.

```
                              internet
                                 |
          +----------------------+----------------------+
          |                                             |
  Static Web Apps (Free, westeurope)           App Service B1 (Linux, southafricanorth)
  Blazor Admin client                          KubaPC.API, system-assigned managed identity
  HTTPS, security headers                      HTTPS only, TLS 1.2, health check /health
          |                                             |
          | browser calls the API                       | VNet integration: outbound into snet-app
          +--------------------------------------------->
                                                        |
                                 vnet-kubapc-dev 10.20.0.0/16
                                   snet-app 10.20.1.0/24 (delegated to App Service)
                                   snet-private-endpoints 10.20.2.0/24 (NSG: VNet in, deny rest)
                                        |                               |
                              private endpoint                 private endpoint
                                        |                               |
                       PostgreSQL Flexible Server B1ms          Key Vault (RBAC, public access off)
                       public access off, SSL required          KubaPC-ConnectionString
                       database kubapc                          KubaPC-JwtSecret
                                        |                               |
                 privatelink.postgres.database.azure.com   privatelink.vaultcore.azure.net
                                   (private DNS zones linked to the VNet)

                       Log Analytics workspace  <-- diagnostics from App Service, Key Vault, PostgreSQL
                       Application Insights     <-- request telemetry from the API
```

## Request flow

1. The browser loads the Blazor client from Static Web Apps.
2. The client calls the API on App Service over HTTPS. App Service terminates TLS and forwards the request with `X-Forwarded-*` headers, which the API honours through `ASPNETCORE_FORWARDEDHEADERS_ENABLED`.
3. The API's connection string and token signing key are App Service settings that are Key Vault references. App Service resolves them at start-up with the API's managed identity, which holds Key Vault Secrets User on the one vault. The vault has public access disabled, so the lookup goes through its private endpoint.
4. The API connects to PostgreSQL by host name. Inside the VNet the private DNS zone resolves that name to the private endpoint; the server's public access is disabled.

## Decisions

| Area | Choice | Reason |
|---|---|---|
| Compute | App Service Linux B1 | Smallest size with VNet integration and diagnostics; runs .NET 10 without a container image |
| Client | Static Web Apps Free | The published client is static files; no secrets; free |
| Database | PostgreSQL Flexible Server 17, B1ms, public access disabled, private endpoint | Matches the application; the server can be stopped when idle; private endpoint keeps it off the internet while still allowing a logged break-glass window for migrations |
| Secrets | Key Vault, RBAC, public access disabled, private endpoint, soft delete | One audited place for secrets; reachable only from the VNet |
| Identity | System-assigned managed identity on the API; workload identity federation for the pipeline | No stored credentials anywhere |
| Network | One VNet, two subnets, NSG on the private endpoint subnet, two private DNS zones | Private endpoints by name, nothing hard-coded |
| Infrastructure as code | Bicep modules per concern, one parameters file per environment, what-if on every change | Declarative, idempotent, no state file |
| Delivery | Azure DevOps YAML pipeline, self-hosted agent, three environments with approvals | Separate gates for infrastructure, database and application |
| Monitoring | Log Analytics with a 1 GB daily cap, Application Insights | Diagnostics from every resource at no cost at this size |

## Production hardening not applied here

| Control | Production | Here |
|---|---|---|
| Web application firewall and private inbound on App Service | Front Door Premium with WAF and Private Link | Public App Service endpoint with the API's own rate limiting and headers |
| Key Vault purge protection | On | Off, so the environment can be removed quickly |
| PostgreSQL authentication | Entra ID, password authentication off | Password authentication, secret in Key Vault |
| Pipeline agents | Inside the VNet | Self-hosted agent on a workstation |
| Environments and guardrails | Dev, test and prod subscriptions with Azure Policy and Defender for Cloud | One environment |
| Approvals | Separate approvers per gate, no self-approval | One approver |

## Cost

Pay-as-you-go, South Africa North, October 2026.

| Resource | Per day |
|---|---|
| App Service plan B1 | about R8 |
| PostgreSQL B1ms (no compute charge while stopped) | about R10 |
| Two private endpoints | about R9 |
| Key Vault, DNS zones, VNet, logs, Static Web Apps | under R1 |
| Total | about R28 |

`azure/scripts/stop-start-db.sh stop` removes the database compute charge between uses; `azure/scripts/teardown.sh` deletes the resource group and ends all charges.
