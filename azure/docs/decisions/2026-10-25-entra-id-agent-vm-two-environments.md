# Design: Entra ID database login, agent VM in a hub network, test and prod environments

Date: 2026-10-25. Status: approved and built on 2026-10-25; first deployment is Lyle's. Supersedes the single-environment, WSL-agent and password-login parts of `architecture.md`.

## Intent

The platform as committed on 2026-10-24 deploys one environment (`dev`), lets the API log in to PostgreSQL with a password kept in Key Vault, and runs the pipeline on an agent inside WSL on a laptop. Each of those is listed in `architecture.md` under "Production hardening not applied here". None of them was left out for cost. Nothing has been deployed yet, so they can be built in now and the guide written once, rather than deployed, reworked and redeployed.

The three changes, in one sentence each:

1. **Entra ID only on PostgreSQL.** No database password exists anywhere. The pipeline identity administers the database; the API's managed identity is the application login.
2. **Pipeline agent on a small Linux VM in a hub virtual network**, peered to each environment. The agent reaches databases and vaults over private endpoints. The "break-glass" firewall window disappears.
3. **Two environments, `test` and `prod`**, each in its own resource group with its own network, database, vault, API and site. `test` deploys without approval; `prod` waits for approval at each gate.

Audience for everything produced: Lyle, who is new to Azure and infrastructure as code, has to deploy, operate and explain all of it himself in an interview on 2026-11-03. Teaching is part of the deliverable, not an afterthought.

## Goals

- Every resource created by Bicep from a parameter file per environment, with `what-if` shown before every change.
- The database is never reachable from the internet, not even for a minute.
- The only secret a human ever handles is the token-signing key (`JWT_SECRET`), one per environment.
- A test run of the whole pipeline costs nothing extra in clicks: `test` flows through on its own.
- `prod` is created on the morning of the interview from the same templates, and a change can be shown going through all gates.
- Lyle can draw the whole thing on a whiteboard and defend each decision, including what is still missing compared with a bank.

## Non-goals

- Web application firewall, Front Door, Application Gateway. Cost.
- Key Vault purge protection. Blocks fast teardown and name reuse.
- Separate subscriptions, Azure Policy, Defender for Cloud. Cost and account scope.
- A second approver. One person.
- Any change to application features.

## Anti-goals

- A design that only works if the pipeline agent has a database password. If Entra ID login turns out not to work for a piece, the piece is fixed or dropped; a password is not reintroduced.
- Anything that requires opening the database or the vault to a public IP, even briefly.
- Click-only setup that cannot be repeated. Where a role assignment or a resource can be created by a script that Lyle runs and reads, it is.
- Explaining less than is needed to operate it. A step Lyle cannot explain in the interview is not finished.

## Constraints

- Budget: about R100 per day while everything runs, well under R40 per day when databases are stopped and the VM is deallocated. Everything is deleted after 2026-11-03.
- Region: South Africa North for everything except Static Web Apps (West Europe).
- Tools on the laptop: WSL Ubuntu with Azure CLI, Bicep, .NET 10 SDK, psql, git. These stay for one-off operator tasks only.
- Azure DevOps with a self-hosted agent (there is no hosted parallelism grant on a new organisation and the agent needs private network access anyway).
- No company code or data leaves the laptop except through the data-load path described below, which Lyle controls.
- Commit, code and document style as in the global engineering rules. No internal abbreviations in resource names or scripts.

## Shape of the deployed estate

Three resource groups in one subscription.

| Resource group | Holds | Created by |
|---|---|---|
| `rg-kubapc-shared` | hub VNet `vnet-kubapc-hub` (10.10.0.0/16) with subnet `snet-agents`; VM `vm-kubapc-agent` (B2s, Ubuntu 24.04, system-assigned managed identity, Standard public IP, NSG denying all inbound); the two private DNS zones `privatelink.postgres.database.azure.com` and `privatelink.vaultcore.azure.net`, linked to the hub and to every spoke; storage account `stkubapc<suffix>` with containers `data-loads-test` and `data-loads-prod` | Lyle, by hand from WSL, with `azure/scripts/deploy-shared.sh` running `azure/infra/shared/main.bicep` |
| `rg-kubapc-test` | spoke VNet `vnet-kubapc-test` (10.20.0.0/16) peered both ways to the hub; PostgreSQL, Key Vault, App Service plan and API, Static Web App, Log Analytics, Application Insights, private endpoints | Pipeline, `azure/infra/main.bicep` with `test.bicepparam` |
| `rg-kubapc-prod` | the same as test, 10.30.0.0/16 | Pipeline, `prod.bicepparam`, approvals at each gate |

Why a hub: one agent must reach both environments' private endpoints by name. Two private DNS zones with the same name cannot both serve one network, so the zones move to the hub and every environment's private endpoint registers its record in the shared zone. This is the "central DNS in the hub" pattern a bank runs, at toy scale.

Why a public IP on the agent: since 30 September 2025 a new VM has no internet access unless given an explicit path. A NAT gateway is the cleaner answer and costs about R20 per day; a Standard public IP with an NSG that denies all inbound costs about R2 per day and gives the same outbound. The VM has no SSH rule; operator access is through `az vm run-command`, which is logged in the activity log.

## Identity and access

Three Azure DevOps service connections, each a workload-identity-federated app registration, each scoped to one resource group where it is Contributor automatically:

- `sc-kubapc-shared` on `rg-kubapc-shared` (used only for optional re-deploys of the hub from the pipeline; the first deploy is from WSL as Lyle).
- `sc-kubapc-test` on `rg-kubapc-test`.
- `sc-kubapc-prod` on `rg-kubapc-prod`.

Extra, scoped role assignments, created by `azure/scripts/grant-pipeline-roles.sh <environment> <application (client) id>` so Lyle runs one command per environment and can read what it grants; the script resolves and prints the service principal's object id for the parameter file:

| Identity | Role | Scope | Why |
|---|---|---|---|
| `sc-kubapc-<env>` | Role Based Access Control Administrator, conditioned to the single role Key Vault Secrets User | `rg-kubapc-<env>` | Bicep assigns the API's managed identity the right to read vault secrets |
| `sc-kubapc-<env>` | Network Contributor | `rg-kubapc-shared` | the environment's Bicep creates the hub side of its peering; a module scoped to another resource group is a deployment in that group, so the role must sit at group scope |
| `sc-kubapc-<env>` | Private DNS Zone Contributor | `rg-kubapc-shared` | the environment's private endpoints write their A records and the spoke's zone links |
| `sc-kubapc-<env>` | Reader | `rg-kubapc-shared` | the migration step reads the agent VM's identity |
| `sc-kubapc-<env>` | Storage Blob Data Contributor | the `data-loads-<env>` container | the data-load pipeline below downloads and then deletes the dump; test can never read a prod dump |
| Lyle's user | Storage Blob Data Contributor | the storage account | upload a dump |

One database administrator (Entra ID) per server, declared in Bicep as an `administrators` child resource, API version 2024-08-01: `sc-kubapc-<env>`'s service principal (principal type `ServicePrincipal`). Its object id is a parameter in `<env>.bicepparam`; Lyle copies it from the app registration once. The login name is set to `sc-kubapc-<env>`, not the app registration's display name: the `administrators` resource lets the name be chosen, and the generated display name can exceed PostgreSQL's 63-character role-name limit.

Everything that runs SQL against a server runs as that server's one administrator, so one role owns every object: schema scripts, the application-login grant, and data loads. The agent VM's managed identity is not a database administrator. The migration step creates it as a login (`pgaadauth_create_principal_with_oid`) and grants it membership of `kubapc_app`, which is enough for the two operator tasks that remain (seed a first SuperAdmin, read tables through `SET LOCAL ROLE kubapc_administrator`) and nothing more.

Known risk: a 2023 report of `InternalServerError` when creating a service principal administrator through ARM on API version 2022-12-01. If that recurs on 2024-08-01 the migration step falls back to `az postgres flexible-server ad-admin create` before connecting; the script is written so the fallback is a one-line change and the guide says where.

Azure DevOps and the tenant: automatic workload-identity service connections need the Azure DevOps organisation connected to the same Entra tenant as the subscription. The guide creates the organisation while signed in with the Azure account and checks Organization settings → Microsoft Entra before creating any connection. If the connection cannot be automatic, the guide's fallback is a manual app registration with a federated credential, and `grant-pipeline-roles.sh` works the same either way.

Application login: the API connects as its own managed identity, user name equal to the App Service name (`app-kubapc-<env>-api-<suffix>`). The migration step, after applying the SQL scripts, runs as the Entra administrator:

```
SELECT * FROM pgaadauth_create_principal_with_oid('<api name>', '<api principal id>', false, false)  -- if the role does not exist
GRANT kubapc_app TO "<api name>";
```

`kubapc_app` keeps every grant it has today and stays `NOINHERIT` toward `kubapc_administrator`, so the existing `SET LOCAL ROLE kubapc_administrator` elevation in the services still works and the API still cannot see administrator-only tables without it. `001-database-roles.sql` is unchanged; `kubapc_app` keeps `LOGIN` but password login is off at the server, so it is inert.

## Secrets

- `JWT_SECRET`, one per environment, in Azure DevOps variable groups `kubapc-test-secrets` and `kubapc-prod-secrets`, written into Key Vault by Bicep, read by the API through a Key Vault reference. Generated by `bootstrap.sh`.
- No database password. `POSTGRES_ADMIN_PASSWORD` and `APP_DB_PASSWORD` cease to exist.
- The connection string contains no secret any more (`Host=...;Database=kubapc;Username=<api name>;SSL Mode=Require`) and becomes a plain App Service setting. Key Vault holds secrets only. `KubaPC-ConnectionString` leaves the vault.

## Application change

`KubaPC.Services/Data/NpgsqlDataAccess.cs` builds one `NpgsqlDataSource` at start-up instead of a connection per call from a string. When the configuration flag `Database:UseEntraIdAuthentication` is true (set by Bicep in App Service settings), the data source uses `UsePeriodicPasswordProvider` to fetch a token for scope `https://ossrdbms-aad.database.windows.net/.default` with `DefaultAzureCredential`, refreshed every 50 minutes, retried after 10 seconds on failure. When the flag is false or absent, behaviour is exactly today's: the connection string with its password, which is what local development uses. The decision "which mode am I in" is a pure function with unit tests. `Azure.Identity` is added to `KubaPC.Services`, pinned.

## Pipeline

`azure/pipelines/azure-pipelines.yml` keeps `Validate` and `Build` as they are today except that `Validate` runs `what-if` for both environments. The three deployment stages become a template, `azure/pipelines/templates/deploy-environment.yml`, with parameters for environment name, service connection, resource group, parameter file and variable group. It is called twice:

1. `test`: infrastructure, database, application. Azure DevOps environments `kubapc-test-infrastructure`, `-database`, `-application` exist for history but carry no approval check.
2. `prod`: same three, depends on test's application stage, each environment carries an approval check naming Lyle. The prod stages are included only when the runtime parameter `deployProd` is ticked, so weekday rehearsals end after test without a run left waiting or rejected; the interview-morning run is started by hand with the box ticked.

Both run only from `main`. Pull requests run `Validate` and `Build`.

`migrate.sh` loses the firewall window and the two passwords. It logs in with `az account get-access-token --resource-type oss-rdbms` as the pipeline identity, user name `sc-kubapc-<env>`, applies scripts as today, then creates and grants the API login and the agent VM login as above.

The agent pool is `vnet-agents`, one VM. The agent runs as a systemd service so it is back after each start of the VM.

## Operator scripts (run by Lyle from WSL)

| Script | Does |
|---|---|
| `bootstrap.sh` | checks South Africa North quota for a B-series VM and for Flexible Server B1ms and stops with a plain message if either is zero, registers providers, creates the three resource groups, prints one `JWT_SECRET` per environment |
| `deploy-shared.sh` | deploys `azure/infra/shared/main.bicep` with what-if first; prints the VM's managed identity object id |
| `agent-vm-setup.sh <organisation> <token>` | runs the agent install on the VM through `az vm run-command create` with the token as a protected parameter; installs .NET 10 SDK, Node 20, Azure CLI, Bicep, psql, zip, jq; registers the agent in pool `vnet-agents`; installs it as a service |
| `grant-pipeline-roles.sh <environment> <client id>` | the role assignments in the table above, and prints the object id for the parameter file |
| `stop-start.sh stop|start <environment>` | stops or starts that environment's database; `stop-start.sh stop agent` deallocates the VM |
| `upload-data.sh` | `pg_dump` of the local database (data only, same exclusions as today) uploaded to the `data-loads-<environment>` container as `<environment>.sql`, then prints the pipeline to run |
| `teardown.sh [environment|shared|all]` | deletes resource groups, purges soft-deleted vaults |

Data load is a second, manually started pipeline, `azure/pipelines/load-data.yml`, with one parameter: the environment. It runs on the agent as `sc-kubapc-<env>`: downloads the blob with the pipeline identity, applies it with `psql` logged in as the database administrator (the same role that owns the tables, so `--disable-triggers` works), then deletes the blob. The `prod` run uses the `kubapc-prod-database` environment and so waits for approval. Why a pipeline and not a `run-command` on the VM: the owner role is the pipeline identity, and a load through the pipeline is approved and logged in the same place as every other change to the database.

The storage account has a public endpoint, Entra ID authorisation only, shared-key access off, TLS 1.2 minimum, and blobs are deleted after each load. The anti-goal about public reachability covers the database and the vault; a dump sitting for a few minutes in an Entra-only container is accepted. A private endpoint for storage would add about R4 per day and a third DNS zone for no gain at this size.

The old `clone-local-data.sh` and the break-glass logic are removed, not kept as a fallback (see anti-goals).

## Documents

- `azure/docs/architecture.md`: new diagram text, decisions table, hardening table reduced to WAF, purge protection, separate approvers, subscription per environment, NAT gateway instead of VM public IP.
- `azure/docs/runbook.md`: rewritten to the scripts above.
- `azure/docs/glossary.md`: add hub and spoke, peering, Entra ID, token, managed identity login, run-command, NAT gateway, workload identity federation if missing.
- `C:\Users\ldass\Documents\AIProjects\interview-prep\KubaPC-Azure-Guide.docx`: Parts 1 to 9 rewritten against the new shape. Part 1 gains plain-English sections on "why a hub", "how a token replaces a password" and "why two environments". Parts 4 to 7 become the new ordered setup. Part 8 (interview morning) adds "create prod". Part 9.5 shrinks. Each part keeps the pattern: what you are about to do, why, click-by-click, how to check it worked, what to say about it. Diagrams 02, 03, 04, 07 and 08 redrawn.

## Order of work

1. Bicep: shared module set; environment template changes (peering, DNS zone ids as parameters, Entra-only server with administrators, connection string as app setting, flag for the API). `bicep build` and `bicep lint` clean; `what-if` cannot run until Lyle has a subscription signed in, so the first real validation is Lyle's.
2. Application change with unit tests; `dotnet test` green.
3. Scripts and pipeline template.
4. `azure/docs` and the guide.
5. Lyle: accounts, bootstrap, shared deploy, agent VM, service connections, roles, variable groups, environments, pipeline, first `test` run. Fix what breaks, together.
6. Interview morning: `prod`.

## Left to the builder

- Exact VM size within the B family and disk type, as long as .NET and Node builds complete in the time budget.
- Whether the agent install is cloud-init or a run-command script, as long as the token never lands in VM metadata or logs in clear.
- Bicep file layout under `azure/infra/shared/`.
- Parameterisation of address spaces versus hard-coding per environment.
- `bootstrap.sh` creates all three resource groups up front, so `Validate` can what-if `prod` from the first run.
- If the subscription has no B-series VM quota in South Africa North, the hub moves to the nearest region that has it (global peering works); the environments stay in South Africa North.
- Wording and diagrams in the guide, within the pattern above.

## What is still not bank-grade, for the interview

- No web application firewall in front of the API.
- Agent outbound through a public IP instead of a NAT gateway or firewall.
- One subscription, resource groups as the environment boundary.
- One approver, who is also the author.
- Key Vault purge protection off.
