# Runbook

How the platform is set up, deployed, loaded with data, operated and removed. Commands run in a WSL Ubuntu terminal from the repository root unless stated. Prerequisites in WSL: Azure CLI 2.91+, Bicep 0.48+, .NET 10 SDK, git, `postgresql-client`, `openssh-client`, `openssl`, `jq`, `zip`, `curl`, `libicu-dev`, Node 20. Your Azure account needs the Owner role on the subscription, because setup creates role assignments.

## 1. One-off setup

Do the steps in order. Each one needs the one before it.

### 1.1 Check the subscription and create the resource groups

```
az login --use-device-code
az account show --output table
cd /mnt/c/Users/ldass/Documents/AIProjects/kubapc-demo-app
bash azure/scripts/bootstrap.sh
```

`bootstrap.sh` stops with a plain message if the region has no quota for a B-series VM or no PostgreSQL B1ms. Otherwise it registers the resource providers, creates `rg-kubapc-shared`, `rg-kubapc-test` and `rg-kubapc-prod`, and prints one `JWT_SECRET` for each environment. Keep both for step 1.6. They are the only secrets anyone handles; there is no database password.

### 1.2 Deploy the shared hub

```
bash azure/scripts/deploy-shared.sh
```

Creates an SSH key if you have none, shows what-if, and deploys after you type `yes`: hub network, agent VM, the two private DNS zones, and the storage account with its two containers. It also gives you Storage Blob Data Contributor on both containers and prints the outputs, including the storage account name. Takes about 10 minutes.

### 1.3 Azure DevOps project and service connections

Create an organisation and a private project named `kubapc` at `https://dev.azure.com`, signed in with the same Azure account. Check Organization settings → Microsoft Entra shows the same tenant as the subscription, or automatic connections will fail.

Project settings → Pipelines → Service connections:

- **GitHub**, OAuth, name `github-lyledass`, grant access to all pipelines.
- **Azure Resource Manager**, App registration (automatic), Workload identity federation, scope Subscription → resource group, grant access to all pipelines. Create three:

| Name | Resource group |
|---|---|
| `sc-kubapc-shared` | `rg-kubapc-shared` |
| `sc-kubapc-test` | `rg-kubapc-test` |
| `sc-kubapc-prod` | `rg-kubapc-prod` |

Each connection is Contributor on its resource group and nothing else. Open the `test` and `prod` connections, choose Manage App registration, and copy each **Application (client) ID**.

### 1.4 Grant the extra pipeline roles

```
bash azure/scripts/grant-pipeline-roles.sh test <client id of sc-kubapc-test>
bash azure/scripts/grant-pipeline-roles.sh prod <client id of sc-kubapc-prod>
```

Each grant is printed before it is made. For one environment the script grants:

- Role Based Access Control Administrator on its own resource group, conditioned to the single role Key Vault Secrets User.
- Network Contributor, Private DNS Zone Contributor and Reader on `rg-kubapc-shared`.
- Storage Blob Data Contributor on its own container, `data-loads-test` or `data-loads-prod`.

The script ends by printing the service principal's object id. Paste it as `pipelinePrincipalObjectId` in `azure/infra/test.bicepparam` and `azure/infra/prod.bicepparam`, replacing the zeros, and commit both files. Role assignments can take a few minutes to take effect.

### 1.5 Install the agent on the VM

In Azure DevOps: Project settings → Agent pools → Add pool → Self-hosted → `vnet-agents`, grant access to all pipelines. Then create a Personal access token with Agent Pools (Read & manage) and run:

```
bash azure/scripts/agent-vm-setup.sh <organisation> <token>
```

The VM accepts no inbound traffic, so the script sends the installation through `az vm run-command`. It installs the build tools and registers the agent as a service, and takes about 10 minutes. Check the agent shows Online in the pool, then revoke the token under User settings → Personal access tokens.

### 1.6 Variable groups

Pipelines → Library → create two variable groups, each holding one secret variable `JWT_SECRET` (padlock ticked), with the matching value from step 1.1: `kubapc-test-secrets` and `kubapc-prod-secrets`. Grant the pipeline access to both.

### 1.7 Environments

Pipelines → Environments: create `kubapc-test-infrastructure`, `kubapc-test-database` and `kubapc-test-application` with no checks. Create `kubapc-prod-infrastructure`, `kubapc-prod-database` and `kubapc-prod-application`, each with an Approvals check naming the approver.

### 1.8 Pipelines

Pipelines → Create Pipeline → GitHub → this repository → Existing Azure Pipelines YAML file. Create two:

| Name | File |
|---|---|
| `kubapc` | `/azure/pipelines/azure-pipelines.yml` |
| `kubapc-load-data` | `/azure/pipelines/load-data.yml` |

## 2. Deploy

Run `kubapc` from `main`. Pull requests run only Validate and Build. The agent VM must be running, because every stage runs on it.

1. **Validate**: compiles and lints both Bicep templates, then runs what-if against `rg-kubapc-test` and `rg-kubapc-prod`.
2. **Build**: restores, builds, runs unit tests, publishes the API, the client and the SQL scripts as artifacts.
3. **Test_Infrastructure, Test_Database, Test_Application**: no approval. Bicep deployment with `test.bicepparam`; then `migrate.sh`, which applies unapplied SQL scripts as the Entra ID administrator, records them in `schema_migrations`, and creates and grants the logins of the API and the agent VM; then the API zip, smoke test and client upload.
4. **Prod_Infrastructure, Prod_Database, Prod_Application**: the same, with `prod.bicepparam`, after `Test_Application`. These stages exist only when the run is started manually with **Deploy prod** ticked, and each waits for approval.

The first run of an environment creates the whole environment and takes about 25 minutes. Later runs only change what differs.

## 3. Load data

To copy a local PostgreSQL database into an environment:

```
export LOCAL_PG_PASSWORD='<local password>'
bash azure/scripts/upload-data.sh test
```

Set `LOCAL_PG_HOST` if the local server is not reachable as `localhost` from WSL (`ip route | awk '/default/ {print $3}'` gives the Windows host address). The script uploads `test.sql` to the `data-loads-test` container with your own sign-in. Then run the `kubapc-load-data` pipeline and choose the environment. It downloads the dump, applies it in one transaction as the database administrator, and deletes the blob. The `prod` run waits for approval on `kubapc-prod-database`.

To create a first SuperAdmin without copying data, run these lines on the agent VM through run-command. The VM's login is a member of `kubapc_app`, which is allowed to add accounts. Replace the server name with `postgresServerFqdn` from the deployment outputs.

```
az login --identity --allow-no-subscriptions
export PGUSER=vm-kubapc-agent PGSSLMODE=require
export PGPASSWORD=$(az account get-access-token --resource-type oss-rdbms --query accessToken --output tsv)
psql --host <server fqdn> --dbname kubapc --command "
INSERT INTO accounts (email_address, full_name, password_hash, password_changed_at, must_change_password, temporary_password_expires_at)
VALUES ('<email>', '<name>', crypt('<first password>', gen_salt('bf', 12)), now(), TRUE, now() + interval '72 hours');
INSERT INTO account_roles (account_id, role_id)
SELECT a.id, r.id FROM accounts a, roles r WHERE a.email_address = '<email>' AND r.role_name = 'SuperAdmin';"
```

Send it with `az vm run-command invoke --resource-group rg-kubapc-shared --name vm-kubapc-agent --command-id RunShellScript --scripts "<the lines above>"`.

## 4. Operate

- Site: the Static Web App's URL in the portal (`stapp-kubapc-<env>-admin`).
- API health: `https://<api host>/health`.
- Logs: the Log Analytics workspace `log-kubapc-<env>`; request telemetry in `appi-kubapc-<env>`.
- Stop what costs money when idle:

```
bash azure/scripts/stop-start.sh stop test      # database
bash azure/scripts/stop-start.sh stop prod
bash azure/scripts/stop-start.sh stop agent     # deallocates the VM
bash azure/scripts/stop-start.sh start agent    # and the same with start
```

A stopped database restarts itself after 7 days. Start the agent before running any pipeline, and give it a minute to show Online.

- Change infrastructure: edit the Bicep, push to `main`, review the what-if, and watch the change go through `test` first.
- Change the hub: edit `azure/infra/shared` and rerun `bash azure/scripts/deploy-shared.sh`.

## 5. Remove

```
bash azure/scripts/teardown.sh test
bash azure/scripts/teardown.sh prod
bash azure/scripts/teardown.sh shared
```

Or `teardown.sh all`. You type the target name to confirm. Delete the environments before the shared group, because their private endpoints hold records in the shared DNS zones. After an environment's group is gone the script purges its soft-deleted Key Vault so the name can be reused. Afterwards delete the three service connections, the two variable groups and the `vnet-agents` pool in Azure DevOps.

## 6. Troubleshooting

| Symptom | Cause | Action |
|---|---|---|
| `bootstrap.sh` says no B-series quota or no B1ms | New subscription with quota of zero, or size not offered | Request quota in the portal (Quotas, Compute), or move the hub to the nearest region that has it and rerun |
| `ResourceGroupNotFound` | Resource groups missing | `bootstrap.sh` |
| `AuthorizationFailed` on a role assignment, peering, DNS zone or storage, straight after step 1.4 | Role assignments have not propagated | Wait five minutes and rerun the stage |
| `AuthorizationFailed ... roleAssignments/write` after that | Step 1.4 not run for this environment | `grant-pipeline-roles.sh` |
| Server `administrators` resource fails with `InternalServerError` | Known fault creating a service principal administrator through Bicep | Create it by hand, then rerun: `az postgres flexible-server ad-admin create --resource-group rg-kubapc-<env> --server-name <server> --display-name sc-kubapc-<env> --object-id <service principal object id> --type ServicePrincipal` |
| Key Vault name exists or soft-deleted | Earlier teardown | `az keyvault purge --name <vault> --location southafricanorth` |
| `LinuxFxVersion` not supported | .NET 10 not offered in the region | Use `DOTNETCORE\|9.0` in `appservice.bicep` and the pipeline |
| `could not translate host name` from psql or the API | The DNS zones are not linked to that network, or the peering is not connected | Check the zone's Virtual network links and both peerings show Connected; `nslookup <server fqdn>` through run-command on the VM should return a 10.x address |
| `password authentication failed` or no login for a name | Login not created, or the Entra ID administrator is missing | Rerun the Database stage; confirm the server's Authentication page lists `sc-kubapc-<env>` |
| `pgcrypto` not allow-listed | `azure.extensions` not applied | Server parameters → `azure.extensions` → PGCRYPTO |
| Smoke test never 200 | App starting, or Key Vault reference not resolved | Check Environment variables for green ticks; restart; rerun |
| Client cannot reach the API | CORS origin mismatch | `Cors__Origins__0` must equal the Static Web App URL |
| Job waits for an agent, or the pool shows Offline | VM deallocated, or the agent service stopped | `stop-start.sh start agent`; if still offline, run `cd /opt/azagent && ./svc.sh status` through run-command, then `./svc.sh start` |
