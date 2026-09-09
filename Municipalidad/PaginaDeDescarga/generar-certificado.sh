#!/usr/bin/env bash
set -e
cd "$(dirname "$0")"

export MSYS_NO_PATHCONV=1
export MSYS2_ARG_CONV_EXCL="*"

IP=""

for interfaz in en0 en1 en2; do
    IP=$(ipconfig getifaddr "$interfaz" 2>/dev/null | grep -oE "^[0-9]+(\.[0-9]+){3}$" || true)
    [ -n "$IP" ] && break
done

if [ -z "$IP" ]; then
    IP=$(hostname -I 2>/dev/null | awk '{print $1}' || true)
fi

if [ -z "$IP" ]; then
    IP=$(ipconfig 2>/dev/null \
        | grep -oE "IPv4[^:]*: *[0-9]+(\.[0-9]+){3}" \
        | grep -oE "[0-9]+(\.[0-9]+){3}" \
        | grep -E "^(10\.|192\.168\.|172\.(1[6-9]|2[0-9]|3[01])\.)" \
        | head -1 || true)
fi

SAN="DNS:localhost,IP:127.0.0.1"
if [ -n "$IP" ]; then
    SAN="$SAN,IP:$IP"
    echo "incluyendo la IP de esta maquina: $IP"
else
    echo "no se detecto IP en la LAN; el certificado va a servir solo para localhost"
fi

mkdir -p certs
openssl req -x509 -newkey rsa:2048 -sha256 -days 365 -nodes \
  -keyout certs/localhost.key -out certs/localhost.crt \
  -subj "/CN=localhost" \
  -addext "subjectAltName=$SAN"

echo "certs/localhost.crt y certs/localhost.key generados"
echo "cubre: $SAN"
[ -n "$IP" ] && echo "desde un telefono en la misma red: https://$IP:8483"
