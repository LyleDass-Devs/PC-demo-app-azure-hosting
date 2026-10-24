# Runbook

How this environment is set up, deployed, refreshed and removed. Commands run in a WSL Ubuntu terminal unless stated. Prerequisites in WSL: Azure CLI 2.91+, Bicep 0.48+, .NET 10 SDK, git, `postgresql-client`, `jq`, `zip`, `curl`, `libicu-dev`, Node 20.

## 1. One-off setup

### 1.1 Azure sign-in and resource group

```
az login --use-device-code
az account show --output table
cd /mnt/c/Users/ldass/Documents/AIProjects/kubapc-demo-app
bash azure/scripts/bootstrap.sh
```

`bootstrap.sh` registers the resource providers, creates `rg-kubapc-dev` in South Africa North, and prints three generated secrets: `POSTGRES_ADMIN_PASSWORD`, `APP_DB_PASSWORD`, `JWT_SECRET`. They go into the Azure DevOps variable group in 1.4 and nowhere else.

### 1.2 Azure DevOps project

Create an organisation and a private project named `kubapc` at `https://dev.azure.com`.

### 1.3 Service connections

Project settings → Pipelines → Service connections.

- **GitHub**, OAuth, name `github-lyledass`, grant access to all pipelines.
- **Azure Resource Manager**, App registration (automatic), Workload identity federation, scope Subscription → resource group `rg-kubapc-dev`, name `sc-kubapc-dev`, grant access to all pipelines.

The Bicep assigns the API's managed identity a Key Vault role, so the pipeline identity also needs the right to assign that one role. In the portal: `rg-kubapc-dev` → Access control (IAM) → Add role assignment → Privileged administrator roles → **Role Based Access Control Administrator** → member: the service connection's identity (named `<organisation>-kubapc-<id>`) → Conditions: allow only the role **Key Vault Secrets User** → Review + assign.

### 1.4 Variable group

Pipelines → Library → + Variable group named `kubapc-dev-secrets` with the three secrets from 1.1, each marked secret (padlock). Grant the pipeline access.

### 1.5 Environments

Pipelines → Environments: create `kubapc-dev-infrastructure`, `kubapc-dev-database` and `kubapc-dev-application`, each with an Approvals check naming the approver.

### 1.6 Agent

Create a Personal access token with Agent Pools (Read & manage). Project settings → Agent pools → Add pool → Self-hosted → `wsl-agents`, grant access to all pipelines. Then:

```
bash azure/scripts/agent-setup.sh <organisation> <token> wsl-agents
cd ~/azagent && ./run.sh
```

The agent must be running (`Listening for Jobs`) whenever the pipeline runs.

### 1.7 Pipeline

Pipelines → Create Pipeline → GitHub → this repository → Existing Azure Pipelines YAML file → `/azure/pipelines/azure-pipelines.yml` → Save. Rename to `kubapc-dev`.

## 2. Deploy

Run the pipeline from `main`. Stages:

1. **Validate**: compiles and lints the Bicep, runs what-if against the resource group.
2. **Build**: restores, builds, runs unit tests, publishes the API, the client and the SQL scripts as artifacts.
3. **Deploy infrastructure** (approval): `az deployment group create` with `azure/infra/main.bicep` and `azure/infra/dev.bicepparam`.
4. **Apply database scripts** (approval): `migrate.sh` opens the break-glass window, applies unapplied scripts in order, records them in `schema_migrations`, sets the application login's password, closes the window.
5. **Deploy application** (approval): API zip to App Service, smoke test, client to Static Web Apps.

The first run creates about 16 resources and takes about 25 minutes. Later runs only change what differs.

## 3. Load data

To copy a local PostgreSQL database into the hosted one:

```
export LOCAL_PG_PASSWORD='<local password>'
export POSTGRES_ADMIN_PASSWORD='<from the variable group>'
bash azure/scripts/clone-local-data.sh
```

Set `LOCAL_PG_HOST` if the local server is not reachable as `localhost` from WSL (`ip route | awk '/default/ {print $3}'` gives the Windows host address).

To create a first SuperAdmin without copying data, open a break-glass window the same way the scripts do and run:

```
INSERT INTO accounts (email_address, full_name, password_hash, password_changed_at, must_change_password, temporary_password_expires_at)
VALUES ('<email>', '<name>', crypt('<first password>', gen_salt('bf', 12)), now(), TRUE, now() + interval '72 hours');
INSERT INTO account_roles (account_id, role_id)
SELECT a.id, r.id FROM accounts a, roles r WHERE a.email_address = '<email>' AND r.role_name = 'SuperAdmin';
```

## 4. Operate

- Site: the Static Web App's URL in the portal (`stapp-kubapc-dev-admin`).
- API health: `https://<api host>/health`.
- Logs: the Log Analytics workspace `log-kubapc-dev`; request telemetry in `appi-kubapc-dev`.
- Stop the database when idle: `bash azure/scripts/stop-start-db.sh stop`; start: `... start`.
- Change infrastructure: edit the Bicep, push to `main`, review the what-if, approve.

## 5. Remove

```
bash azure/scripts/teardown.sh
```

Deletes the resource group. The Key Vault name stays reserved in soft-delete for 7 days; purge with `az keyvault purge --name <vault> --location southafricanorth` before recreating. Afterwards delete the variable group and the `sc-kubapc-dev` service connection and revoke the agent token.

## 6. Troubleshooting

| Symptom | Cause | Action |
|---|---|---|
| `ResourceGroupNotFound` | Resource group missing | `bootstrap.sh` |
| `AuthorizationFailed ... roleAssignments/write` | 1.3 role not assigned | Assign Role Based Access Control Administrator |
| Key Vault name exists or soft-deleted | Earlier teardown | `az keyvault purge` |
| `LinuxFxVersion` not supported | .NET 10 not offered in the region | Use `DOTNETCORE\|9.0` in `appservice.bicep` and the pipeline |
| psql cannot connect during migration | Firewall rule not yet active or agent IP changed | Rerun the stage |
| `pgcrypto` not allow-listed | `azure.extensions` not applied | Server parameters → `azure.extensions` → PGCRYPTO |
| Smoke test never 200 | App starting, or Key Vault reference not resolved | Check Environment variables for green ticks; restart; rerun |
| Client cannot reach the API | CORS origin mismatch | `Cors__Origins__0` must equal the Static Web App URL |
| No agent available | Agent not running | `cd ~/azagent && ./run.sh` |
