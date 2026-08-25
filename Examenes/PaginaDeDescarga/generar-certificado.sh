#!/usr/bin/env bash
# Certificado self-signed para el listener https de URL1. Va a dar advertencia en el
# browser: eso es lo esperado, es la pata "sin certificado valido" del diagrama.
#
# INCLUYE LA IP DE ESTA MAQUINA EN LA LAN, y no es un lujo: es lo que permite abrir la
# app desde un telefono.
#
# Por que importa. Las dos APIs de almacenamiento que la app necesita -el picker de
# carpeta y OPFS- exigen CONTEXTO SEGURO: https, o el literal localhost. Un telefono no
# puede usar localhost para llegar a esta maquina, tiene que entrar por la IP; y por
# http:// esa pagina NO es contexto seguro, asi que el browser no define ninguna de las
# dos y la app se queda sin poder guardar nada.
#
# MEDIDO: en un iPhone entrando por http a la IP, la app reportaba que el navegador no
# permitia guardar. La misma Safari en la Mac, por localhost, si podia. No era Safari.
#
# Con la IP en el SAN, https://<ip>:8453 sirve el certificado correcto para ese host.
# Sigue siendo self-signed, asi que hay que aceptar la advertencia -y en iOS conviene
# instalar y confiar el certificado en Ajustes para que no moleste en cada carga.
set -e
cd "$(dirname "$0")"

# La primera interfaz con IP de las habituales en una Mac. Si no hay ninguna, el
# certificado igual sirve para localhost.
IP=""
for interfaz in en0 en1 en2; do
    IP=$(ipconfig getifaddr "$interfaz" 2>/dev/null || true)
    [ -n "$IP" ] && break
done

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
[ -n "$IP" ] && echo "desde un telefono en la misma red: https://$IP:8453"
