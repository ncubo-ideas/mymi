#!/usr/bin/env bash
# LA VM QUE SE DESCARGA: Ubuntu con Mymi, los cinco modulos y Claude Desktop adentro.
#
# Se corre UNA VEZ sobre una Ubuntu 24.04 limpia (Desktop minimo o Server + xfce). Despues la
# VM se exporta a OVA y eso es lo que baja la persona.
#
# POR QUE AHORA SI SE PUEDE ENTREGAR UNA VM. La objecion original era Windows: no se puede
# redistribuir. Ubuntu si, sin pedirle permiso a nadie, y el peso baja de 10-15 GB a ~3 GB
# (~1.5 comprimido). Lo que era un apano se volvio una opcion legitima.
#
# POR QUE UBUNTU 24.04 Y NO DEBIAN. Es la unica base que cubre los dos clientes de chat a la
# vez: Claude Desktop pide Ubuntu 22.04+ o Debian 12+, y ChatGPT Desktop pide Ubuntu 24.04/26.04
# o Debian 13. 24.04 LTS entra en las dos ventanas y ademas es lo mas probado para .NET.
#
# Uso:
#   sudo ./provision.sh /ruta/al/publicado /ruta/a/los/gmod [/ruta/claude.deb]
#
# El .deb de Claude NO se descarga solo a proposito: la URL cambia y no vamos a adivinarla.
# Se baja de claude.ai/download y se pasa como tercer argumento. Sin el, la VM queda igual y
# el cliente de chat se instala despues.
set -euo pipefail

PUBLICADO=${1:?falta la carpeta con Gnothi.Desktop y Gnothi.Host publicados}
PAQUETES=${2:?falta la carpeta con los .gmod}
CLAUDE_DEB=${3:-}

PERSONA=${PERSONA:-demo}
DESTINO=/opt/mymi

echo "══ 1/7 · paquetes del sistema"
export DEBIAN_FRONTEND=noninteractive
apt-get update
# LA DEPENDENCIA QUE DECIDE SI ARRANCA: sin libwebkit2gtk Photino no pinta nada. En 24.04 el
# paquete es el 4.1 y arrastra gtk3 solo.
#
# OJO: Mymi nunca corrio en Linux antes de este archivo -- el repo no tiene una sola mencion de
# webkit2gtk. Si la ventana no abre, el primer sospechoso es la version contra la que enlaza
# Photino.Native 4.0.22.
#
# VERIFICADO el 2026-09-21: `ldd Photino.Native.so` pide libwebkit2gtk-4.1.so.0,
# libjavascriptcoregtk-4.1.so.0 (ambas vienen con el paquete 4.1), libgtk-3.so.0 y
# **libnotify.so.4**. La ultima faltaba en esta lista y sin ella la ventana no abre.
apt-get install -y --no-install-recommends \
    libwebkit2gtk-4.1-0 libnotify4 libicu74 \
    xfce4 xfce4-terminal dbus-x11 \
    lightdm lightdm-gtk-greeter \
    fonts-dejavu-core ca-certificates \
    xdg-utils

echo "══ 2/7 · Mymi"
install -d "$DESTINO"
cp -r "$PUBLICADO/mymi" "$DESTINO/bin"
cp -r "$PUBLICADO/host" "$DESTINO/host"
chmod +x "$DESTINO/bin/Gnothi.Desktop" "$DESTINO/host/Gnothi.Host"
install -d "$DESTINO/Modulos" "$DESTINO/paquetes"
cp "$PAQUETES"/*.gmod "$DESTINO/paquetes/"

echo "══ 3/7 · alta de los modulos"
# EL ALTA CORRE ACA, en la construccion, y es distinto del sandbox de contenedor.
#
# En el sandbox cada caja es una persona nueva y el alta va en el arranque. Acá la VM ES de una
# persona: la baja, la abre y espera encontrar sus instituciones ya puestas, como si se las
# hubieran entregado. Si el alta fuera en el arranque, la primera impresion seria una ventana
# vacia.
for g in "$DESTINO"/paquetes/*.gmod; do
    echo "   · $(basename "$g")"
    sudo -u "$PERSONA" "$DESTINO/host/Gnothi.Host" --install-module "$g" "$DESTINO/Modulos" \
        || echo "   ! fallo el alta de $(basename "$g")" >&2
done

echo "══ 4/7 · el cliente de chat"
if [[ -n "$CLAUDE_DEB" && -f "$CLAUDE_DEB" ]]; then
    apt-get install -y "$CLAUDE_DEB"
    # EL CONECTOR LO ESCRIBE EL HOST, no nosotros a mano: `--install` ya conoce el
    # claude_desktop_config.json de Claude Desktop y el ~/.codex/config.toml de Codex, y elige
    # segun lo que encuentre instalado. Escribirlo a mano seria mantener el formato en dos
    # lugares.
    sudo -u "$PERSONA" "$DESTINO/host/Gnothi.Host" --install "$DESTINO/Modulos" \
        || echo "   ! el registro del conector fallo" >&2
else
    echo "   (sin .deb de Claude: se instala despues y se corre 'Gnothi.Host --install')"
fi

echo "══ 5/7 · que Mymi abra sola"
# Autologin: la persona baja la VM para ver Mymi, no para escribir una contrasena que ademas
# tendriamos que publicar en la pagina de descarga.
install -d /etc/lightdm/lightdm.conf.d
cat > /etc/lightdm/lightdm.conf.d/50-mymi.conf <<EOF
[Seat:*]
autologin-user=$PERSONA
autologin-user-timeout=0
user-session=xfce
EOF

install -d "/home/$PERSONA/.config/autostart"
cat > "/home/$PERSONA/.config/autostart/mymi.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=Mymi
Exec=$DESTINO/bin/Gnothi.Desktop $DESTINO/Modulos
X-GNOME-Autostart-enabled=true
EOF

# Y un lanzador en el escritorio, para cuando la cierren y quieran volver a abrirla.
install -d "/home/$PERSONA/Desktop"
cat > "/home/$PERSONA/Desktop/Mymi.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=Mymi
Comment=Tus instituciones, en esta computadora
Exec=$DESTINO/bin/Gnothi.Desktop $DESTINO/Modulos
Terminal=false
EOF
chmod +x "/home/$PERSONA/Desktop/Mymi.desktop"

echo "══ 6/7 · la nota en el escritorio"
cat > "/home/$PERSONA/Desktop/LEEME.txt" <<'EOF'
Mymi, en una computadora que no es la tuya.

Lo que hay acá adentro:

  · Mymi, el Host. Una sola app para todas tus instituciones.
  · Cinco módulos ya dados de alta: el laboratorio, el supermercado,
    la municipalidad, el concesionario y las cargas.
  · Claude Desktop, conectado a Mymi por MCP.

Tres cosas que vale la pena probar:

  1. Abrí Mymi y entrá a una institución. La pantalla que ves la dibujó
     ella, no Mymi: viene dentro de su paquete.

  2. Abrí Claude y preguntale algo sobre tus exámenes o tus compras.
     Va a consultarle a Mymi, que lee la carpeta de esta máquina.
     Nadie le preguntó nada a ninguna empresa.

  3. Desconectá la red de la máquina virtual y repetí el paso 1.
     El expediente sigue ahí. Ese es el punto de todo esto.

Para el paso 2 hace falta tu propia cuenta de Claude: la sesión es tuya,
no viene con la imagen.

El registro vive en:  ~/Documents/Gnothi/
Los módulos, en:      /opt/mymi/Modulos/
EOF

chown -R "$PERSONA:$PERSONA" "/home/$PERSONA"

echo "══ 7/7 · limpieza para que la imagen no pese de gusto"
apt-get clean
rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/*
# El log del journal y la cache de fuentes se regeneran; en la imagen son peso muerto.
journalctl --vacuum-time=1s 2>/dev/null || true
# Llenar de ceros el espacio libre hace que el OVA comprima de verdad (~3 GB → ~1.5 GB).
# Si esto no se corre, la imagen arrastra todo lo que se borro durante la construccion.
dd if=/dev/zero of=/CEROS bs=1M 2>/dev/null || true
rm -f /CEROS
sync

echo
echo "Listo. Apagá la VM y exportála a OVA."
