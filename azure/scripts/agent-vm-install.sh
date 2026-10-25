#!/usr/bin/env bash
# Installs the build tools and the Azure DevOps agent ON the agent VM.
# Do not run this on your laptop: agent-vm-setup.sh sends it to the VM through
# `az vm run-command`, where it runs as root with three positional arguments:
#   $1 Azure DevOps organisation, $2 personal access token, $3 agent pool.
#
# Everything the pipeline runs needs to exist here: .NET 10 SDK and Node 20 to
# build the application, Azure CLI and Bicep to deploy, psql to apply SQL, zip and
# jq for packaging. The agent runs as its own unprivileged user, azagent, under
# systemd so it comes back on its own after every VM start. Rerunning this script
# is safe: each step checks whether its work is already done.

# run-command may start us with /bin/sh. Everything below is bash, so re-launch
# under bash if that happened.
if [ -z "${BASH_VERSION:-}" ]; then
  exec bash "$0" "$@"
fi
set -euo pipefail

organisation="${1:?Azure DevOps organisation name}"
token="${2:?personal access token}"
pool="${3:-vnet-agents}"
agentName="vm-kubapc-agent"
agentUser="azagent"
agentHome="/opt/azagent"

export DEBIAN_FRONTEND=noninteractive
apt-get update -qq
# dotnet-sdk-10.0 comes from Ubuntu's own package feed, so it updates with the OS.
apt-get install -y -qq git curl jq zip unzip postgresql-client libicu-dev ca-certificates gnupg dotnet-sdk-10.0

if ! command -v node > /dev/null; then
  curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
  apt-get install -y -qq nodejs
fi

if ! command -v az > /dev/null; then
  curl -sL https://aka.ms/InstallAzureCLIDeb | bash
fi

# A dedicated user keeps pipeline jobs away from root. The home folder doubles as
# the agent folder so Bicep (installed under ~/.azure) is found by the same user.
if ! id "$agentUser" > /dev/null 2>&1; then
  useradd --system --create-home --home-dir "$agentHome" --shell /bin/bash "$agentUser"
fi
mkdir -p "$agentHome"
chown "$agentUser:$agentUser" "$agentHome"
sudo -u "$agentUser" -H az bicep install

if [ ! -f "$agentHome/config.sh" ]; then
  latestTag=$(curl -fsSL https://api.github.com/repos/microsoft/azure-pipelines-agent/releases/latest | jq -r .tag_name)
  agentVersion="${latestTag#v}"
  echo "Downloading agent $agentVersion."
  curl -fsSL -o /tmp/agent.tar.gz "https://download.agent.dev.azure.com/agent/${agentVersion}/vsts-agent-linux-x64-${agentVersion}.tar.gz"
  tar -zxf /tmp/agent.tar.gz -C "$agentHome"
  rm -f /tmp/agent.tar.gz
  chown -R "$agentUser:$agentUser" "$agentHome"
fi

cd "$agentHome"
# The .agent file is written by config.sh; its presence means the agent is already
# registered and config.sh would refuse to run again.
if [ ! -f .agent ]; then
  sudo -u "$agentUser" -H ./config.sh --unattended \
    --url "https://dev.azure.com/${organisation}" \
    --auth pat --token "$token" \
    --pool "$pool" \
    --agent "$agentName" \
    --replace \
    --acceptTeeEula
fi

# svc.sh writes a .service marker when the systemd unit exists.
if [ ! -f .service ]; then
  ./svc.sh install "$agentUser"
fi
./svc.sh start
# status is informational; a non-zero exit here must not fail the whole install.
./svc.sh status || true

echo "Agent $agentName is installed as a service in pool $pool."
echo "dotnet: $(dotnet --version)  node: $(node --version)  az: $(az version --query '"azure-cli"' --output tsv)  psql: $(psql --version)"
