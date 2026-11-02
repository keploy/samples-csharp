#!/bin/sh
# gen-certs.sh DIR: a test CA (ca.pem) and the fake providers' certificate
# (provider.pem, provider-key.pem) for payments.provider.test,
# lms.provider.test and localhost. Trust ca.pem on the record machine as a
# real provider's public CA is trusted (scripts/trust-ca.sh).
set -eu
dir=${1:?usage: gen-certs.sh DIR}
mkdir -p "$dir"
cd "$dir"
openssl req -x509 -newkey rsa:2048 -nodes -days 30 -subj "/CN=CRM sample test CA" \
  -keyout ca-key.pem -out ca.pem 2>/dev/null
openssl req -newkey rsa:2048 -nodes -subj "/CN=provider.test" \
  -keyout provider-key.pem -out provider.csr 2>/dev/null
printf 'subjectAltName=DNS:payments.provider.test,DNS:lms.provider.test,DNS:localhost,IP:127.0.0.1\nbasicConstraints=CA:FALSE\nextendedKeyUsage=serverAuth\n' >provider.ext
openssl x509 -req -in provider.csr -CA ca.pem -CAkey ca-key.pem -CAcreateserial -days 30 \
  -extfile provider.ext -out provider.pem 2>/dev/null
rm -f provider.csr provider.ext ca.srl
echo "gen-certs: $dir/ca.pem, $dir/provider.pem"
