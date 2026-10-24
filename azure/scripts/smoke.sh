#!/usr/bin/env bash
set -euo pipefail

apiUrl="${1:?api url, for example https://app-kubapc-dev-api-abc123.azurewebsites.net}"

echo "Waiting for $apiUrl/health"
for attempt in $(seq 1 30); do
  status=$(curl -s -o /dev/null -w "%{http_code}" "$apiUrl/health" || true)
  if [ "$status" = "200" ]; then
    echo "Health check answered 200."
    break
  fi
  sleep 10
done

if [ "$status" != "200" ]; then
  echo "Health check never answered 200 (last status $status)."
  exit 1
fi

headers=$(curl -s -D - -o /dev/null "$apiUrl/health")
for required in "x-content-type-options: nosniff" "x-frame-options: DENY" "strict-transport-security"; do
  if ! echo "$headers" | tr -d '\r' | grep -qi "$required"; then
    echo "Missing response header: $required"
    exit 1
  fi
done
echo "Security headers present."

status=$(curl -s -o /dev/null -w "%{http_code}" "$apiUrl/contracts")
if [ "$status" != "401" ]; then
  echo "Expected 401 without a token on /contracts, got $status."
  exit 1
fi
echo "Protected endpoint refuses anonymous calls."
