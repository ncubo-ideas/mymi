#!/usr/bin/env bash
# Certificado para el listener https de URL1. Va a dar advertencia en el browser mientras
# no se instale la CA: eso es lo esperado, es la pata "sin certificado valido" del
# diagrama.
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
# ------------------------------------------------------------------ POR QUE HAY DOS
#
# Antes esto emitia UN certificado self-signed y ya. Ese certificado no lo podia confiar
# un iPhone, por dos razones que se contradicen entre si:
#
#   - para servir TLS, iOS 13+ exige ExtendedKeyUsage con serverAuth. El openssl req
#     -x509 de antes no la ponia, asi que iOS lo rechazaba como certificado de servidor;
#   - para ser ancla de confianza -lo que se instala y se marca en Ajustes- tiene que ser
#     una CA. El de antes salia con CA:TRUE, o sea que era una CA usada de hoja.
#
# Un solo certificado no puede ser las dos cosas bien. Asi que ahora son dos, que es como
# funciona TLS de verdad:
#
#   ca.crt         la CA. CA:TRUE. Es LO QUE SE INSTALA Y SE CONFIA en el telefono.
#   localhost.crt  la hoja que sirve nginx. CA:FALSE + EKU serverAuth + la IP en el SAN.
#
# Y hay un beneficio concreto: la CA se genera UNA VEZ y se reusa. Cuando el DHCP le
# cambia la IP a esta maquina, se vuelve a correr esto, se emite una hoja nueva con la IP
# nueva -y el telefono no hay que tocarlo, porque sigue confiando en la misma CA. Con el
# esquema de antes cada cambio de IP obligaba a reinstalar y reconfiar a mano.
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

# ------------------------------------------------------------------ la CA

# SE REUSA SI YA EXISTE, y eso es el punto: si se regenerara en cada corrida, el telefono
# tendria que reinstalarla cada vez. Para forzar una nueva, borrar certs/ca.crt.
if [ -f certs/ca.crt ] && [ -f certs/ca.key ]; then
    echo "reusando la CA que ya existe en certs/ca.crt (el telefono ya la confia)"
else
    echo "generando una CA nueva"
    openssl req -x509 -newkey rsa:2048 -sha256 -days 3650 -nodes \
      -keyout certs/ca.key -out certs/ca.crt \
      -subj "/CN=Examenes CA local" \
      -addext "basicConstraints=critical,CA:TRUE" \
      -addext "keyUsage=critical,keyCertSign,cRLSign"
fi

# ------------------------------------------------------------------ la hoja

# Se emite en cada corrida, porque es la que lleva la IP y la IP cambia.
#
# Los tres extras no son decoracion: sin serverAuth iOS no la acepta como certificado de
# servidor, sin CA:FALSE es una CA disfrazada de hoja, y sin el SAN el nombre no coincide
# con el host. Los 365 dias estan debajo del maximo de 825 que exige Apple.
openssl req -newkey rsa:2048 -sha256 -nodes \
  -keyout certs/localhost.key -out certs/localhost.csr \
  -subj "/CN=localhost"

openssl x509 -req -in certs/localhost.csr -sha256 -days 365 \
  -CA certs/ca.crt -CAkey certs/ca.key -CAcreateserial \
  -out certs/localhost.crt \
  -extfile <(printf '%s\n' \
      "basicConstraints=critical,CA:FALSE" \
      "keyUsage=critical,digitalSignature,keyEncipherment" \
      "extendedKeyUsage=serverAuth" \
      "subjectAltName=$SAN")

rm -f certs/localhost.csr

# nginx sirve la cadena completa: la hoja primero y la CA detras. Con la CA ya instalada
# en el telefono no hace falta, pero es lo que corresponde y no cuesta nada.
cat certs/localhost.crt certs/ca.crt > certs/cadena.crt

echo
echo "certs/ca.crt          la CA — ESTO es lo que se instala en el telefono"
echo "certs/localhost.crt   la hoja que sirve nginx"
echo "certs/cadena.crt      hoja + CA, que es lo que apunta nginx.conf"
echo "cubre: $SAN"
echo
echo "en el iPhone, una sola vez:"
echo "  1. pasale certs/ca.crt (AirDrop o mail) y abrilo"
echo "  2. Ajustes › Perfil descargado › Instalar"
echo "  3. Ajustes › General › Información › Ajustes de confianza de certificados"
echo "     y activá «Examenes CA local»   <- este paso es el que casi siempre falta"
[ -n "$IP" ] && echo && echo "despues, desde el telefono: https://$IP:8453"
