#!/usr/bin/env bash
# Certificado self-signed para el listener https de URL1. Va a dar advertencia en el browser: eso
# es lo esperado, es la pata "sin certificado valido" del diagrama.
#
# INCLUYE LA IP DE ESTA MAQUINA EN LA LAN, y no es un lujo: es lo que permite abrir la app desde
# un telefono.
#
# Por que importa. Las dos APIs de almacenamiento que la app necesita -- el picker de carpeta y
# OPFS -- exigen CONTEXTO SEGURO: https, o el literal localhost. Un telefono no puede usar
# localhost para llegar a esta maquina, tiene que entrar por la IP; y por http:// esa pagina NO es
# contexto seguro, asi que el browser no define ninguna de las dos y la app se queda sin poder
# guardar nada. La pantalla distingue las dos causas ('inseguro' vs 'navegador') justamente para
# no mandar a nadie a buscar otro browser que va a fallar igual.
#
# Con la IP en el SAN, https://<ip>:8454 sirve el certificado correcto para ese host. Sigue siendo
# self-signed, asi que hay que aceptar la advertencia.
set -e
cd "$(dirname "$0")"

# GIT BASH EN WINDOWS TRADUCE LOS ARGUMENTOS QUE EMPIEZAN CON '/' A RUTAS DE WINDOWS, asi que
# -subj "/CN=localhost" le llega a openssl como "C:/Program Files/Git/CN=localhost" y falla con
# "missing equal sign". Estas dos variables lo apagan, y en macOS o Linux no significan nada.
export MSYS_NO_PATHCONV=1
export MSYS2_ARG_CONV_EXCL="*"

# La IP de esta maquina en la LAN. Se prueban las tres formas habituales -- macOS, Linux y
# Windows/Git-Bash -- y si ninguna contesta el certificado igual sirve para localhost.
#
# En Windows existe un 'ipconfig' que NO es el de macOS y contesta con su propia ayuda a
# cualquier argumento, asi que la sonda de macOS se silencia entera antes de pasar a la
# siguiente.
IP=""

for interfaz in en0 en1 en2; do
    IP=$(ipconfig getifaddr "$interfaz" 2>/dev/null | grep -oE "^[0-9]+(\.[0-9]+){3}$" || true)
    [ -n "$IP" ] && break
done

if [ -z "$IP" ]; then
    IP=$(hostname -I 2>/dev/null | awk '{print $1}' || true)
fi

if [ -z "$IP" ]; then
    # Windows: la salida de ipconfig trae varias; se toma la primera IPv4 privada.
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
[ -n "$IP" ] && echo "desde un telefono en la misma red: https://$IP:8454"
