#!/bin/sh
# trust-ca.sh CA_PEM: adds the fake providers' test CA to the system trust
# store, where .NET on Linux (OpenSSL) and curl read it, as a real
# provider's public CA already is. Needs root. Debian/Ubuntu and Fedora/RHEL.
set -eu
ca=${1:?usage: trust-ca.sh CA_PEM}
if command -v update-ca-certificates >/dev/null 2>&1; then
  cp "$ca" /usr/local/share/ca-certificates/crm-sample-test-ca.crt
  update-ca-certificates >/dev/null
elif command -v update-ca-trust >/dev/null 2>&1; then
  cp "$ca" /etc/pki/ca-trust/source/anchors/crm-sample-test-ca.pem
  update-ca-trust extract
else
  echo "trust-ca: neither update-ca-certificates nor update-ca-trust is installed" >&2
  exit 1
fi
