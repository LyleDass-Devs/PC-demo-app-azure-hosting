#!/usr/bin/env bash
set -euo pipefail

organisation="${1:?Azure DevOps organisation name, the part after dev.azure.com/}"
pat="${2:?personal access token with Agent Pools read and manage}"
pool="${3:-wsl-agents}"
agentName="${4:-$(hostname)-wsl}"
version="4.258.1"

sudo apt-get update -qq
sudo apt-get install -y -qq curl jq zip unzip postgresql-client libicu-dev ca-certificates gnupg

if ! command -v node > /dev/null; then
  curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
  sudo apt-get install -y -qq nodejs
fi

mkdir -p "$HOME/azagent"
cd "$HOME/azagent"
if [ ! -f config.sh ]; then
  curl -fsSL -o agent.tar.gz "https://download.agent.dev.azure.com/agent/${version}/vsts-agent-linux-x64-${version}.tar.gz"
  tar -zxf agent.tar.gz
  rm agent.tar.gz
fi

./config.sh --unattended \
  --url "https://dev.azure.com/${organisation}" \
  --auth pat --token "$pat" \
  --pool "$pool" \
  --agent "$agentName" \
  --replace \
  --acceptTeeEula

echo
echo "Agent configured. Start it with:  cd ~/azagent && ./run.sh"
echo "Leave that terminal open while pipelines run."
