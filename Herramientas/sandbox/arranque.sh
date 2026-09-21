#!/bin/sh
# EL ARRANQUE DE LA CAJA. Orden: display, escritorio, alta del modulo, VNC, puente al
# navegador, y recien al final Mymi en primer plano.
#
# Mymi va ULTIMO y sin `&` a proposito: es el proceso que le da vida al contenedor. Si la
# ventana se cierra o se cae, el contenedor termina y el broker lo recicla en vez de dejar una
# caja viva con una pantalla negra adentro.
set -e

MODULOS=${MODULOS:-/opt/mymi/Modulos}
JOURNALS=${JOURNALS:-$HOME/journals}
PANTALLA=${PANTALLA:-1440x900x24}
MINUTOS=${MINUTOS:-15}

mkdir -p "$MODULOS" "$JOURNALS"

echo "[caja] display $DISPLAY a $PANTALLA"
Xvfb "$DISPLAY" -screen 0 "$PANTALLA" -nolisten tcp -ac &
# Esperar a que el display exista de verdad. Un `sleep 2` alcanza casi siempre, y "casi
# siempre" en un arranque en frio bajo carga significa una ventana que no abre sin decir por
# que. xdpyinfo no esta instalado, asi que se espera por el socket.
i=0
while [ ! -e "/tmp/.X11-unix/X${DISPLAY#:}" ]; do
    i=$((i + 1))
    [ "$i" -gt 100 ] && echo "[caja] el display no levanto" >&2 && exit 1
    sleep 0.1
done

# Un gestor de ventanas hace falta aunque no se vea: sin el, la ventana de Photino abre sin
# decoracion, no toma el foco del teclado y los dialogos modales salen detras.
openbox &

# EL DBUS DE SESION. WebKitGTK lo busca; sin el llena stderr de avisos y en algunas versiones
# no arranca el proceso de red.
eval "$(dbus-launch --sh-syntax)"
export DBUS_SESSION_BUS_ADDRESS

# ---------------------------------------------------------------- el alta del modulo
#
# Se hace ACA y no en el build: el alta escribe `installation.json`, que es el archivo de la
# persona. Cada caja es una persona distinta y empieza con su propia alta, con la carpeta
# concedida por el Host y el consentimiento en su estado inicial.
if [ ! -d "$MODULOS/examenes" ]; then
    echo "[caja] dando de alta examenes.gmod"
    /opt/mymi/host/Gnothi.Host --install-module /opt/mymi/paquetes/examenes.gmod "$MODULOS" \
        || echo "[caja] el alta fallo -- la ventana va a abrir sin modulos" >&2
fi

# ---------------------------------------------------------------- la pantalla, por un socket
#
# UN SOCKET UNIX Y NINGUN PUERTO, y esto es el corazon del aislamiento: la caja corre con
# `network_mode: none`, o sea SIN una sola interfaz mas que `lo`. No tiene como salir a
# internet, pero -- lo que mas importa -- tampoco puede alcanzar al host que la contiene ni
# escanear la LAN donde viven los vClusters.
#
# Una red "interna" de Docker no alcanza para eso: no da salida a internet, pero el host sigue
# siendo alcanzable en la IP de la bridge. Sin interfaz no hay a quien hablarle.
#
# El puente al navegador vive AFUERA, en un sidecar que lee este socket por un volumen
# compartido. Lo unico que cruza es un flujo de pixeles.
#
# -nopw no es descuido: nadie puede conectarse al socket salvo quien tenga el volumen montado.
# Quien decide si se puede ver esta caja es el token del broker, no una contrasena de VNC.
echo "[caja] vnc en socket unix"
x11vnc -display "$DISPLAY" -unixsock /sock/vnc.sock -forever -shared -nopw -quiet -bg

# ---------------------------------------------------------------- el reloj de la sesion
#
# La caja se muere sola. Sin esto, una pestana olvidada deja un contenedor tomando RAM hasta
# que alguien lo note -- y el host que la sirve ya estuvo a 10 GB del OOM una vez.
( sleep $((MINUTOS * 60)); echo "[caja] se acabaron los $MINUTOS minutos"; kill 1 ) &

echo "[caja] Mymi"
cd /opt/mymi/bin
exec ./Gnothi.Desktop "$MODULOS" --journals "$JOURNALS"
