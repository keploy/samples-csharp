#!/bin/sh
# deps.sh up|down: the record-time dependencies (docker compose: DynamoDB
# local, ElasticMQ for SQS, Pulsar standalone) and the fake HTTPS providers
# (node), and the app's table and queue (CrmLeads.dll --bootstrap, from the
# app published to app/out). Run from crm-playwright/. CERT_DIR (default .certs) holds the
# providers' certificate (scripts/gen-certs.sh, its CA trusted with scripts/trust-ca.sh).
set -eu
cd "$(dirname "$0")/.."
cert_dir=${CERT_DIR:-.certs}
case "${1:-}" in
up)
  # The providers' certificate must be in date and chain to a CA this machine trusts: record's calls to them
  # check both. deps.sh makes none itself, since a CA made here is not trusted until trust-ca.sh runs as root.
  if ! err=$(openssl verify -purpose sslserver "$cert_dir/provider.pem" 2>&1); then
    echo "deps: $cert_dir/provider.pem does not verify against this machine's trusted CAs:" >&2
    echo "$err" >&2
    echo "deps: run sh scripts/gen-certs.sh $cert_dir, then sudo sh scripts/trust-ca.sh $cert_dir/ca.pem" >&2
    exit 1
  fi
  docker compose up -d --wait
  node providers/server.mjs "$cert_dir" >providers.log 2>&1 &
  echo $! >providers.pid
  # Wait for each endpoint's own answer (any HTTP status), not a sleep.
  for url in http://127.0.0.1:8000/ http://127.0.0.1:9324/ https://127.0.0.1:8443/ https://127.0.0.1:9443/; do
    n=0
    until curl -sk -o /dev/null "$url"; do
      n=$((n + 1))
      [ "$n" -lt 300 ] || { echo "deps: $url did not answer; providers.log:" >&2; cat providers.log >&2; exit 1; }
      sleep 0.2
    done
  done
  # From app/out, the app's content root, where its appsettings.json is.
  (cd app/out && dotnet CrmLeads.dll --bootstrap)
  ;;
down)
  if [ -f providers.pid ]; then
    kill "$(cat providers.pid)" 2>/dev/null || true
    rm -f providers.pid
  fi
  docker compose down -v --remove-orphans
  ;;
*)
  echo "usage: deps.sh up|down" >&2
  exit 2
  ;;
esac
