#!/usr/bin/env bash
# Certificado self-signed para el listener https de URL1. Va a dar advertencia en el
# browser: eso es lo esperado, es la pata "sin certificado valido" del diagrama.
set -e
cd "$(dirname "$0")"
mkdir -p certs
openssl req -x509 -newkey rsa:2048 -sha256 -days 365 -nodes \
  -keyout certs/localhost.key -out certs/localhost.crt \
  -subj "/CN=localhost" \
  -addext "subjectAltName=DNS:localhost,IP:127.0.0.1"
echo "certs/localhost.crt y certs/localhost.key generados"
