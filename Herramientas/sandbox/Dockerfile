# MYMI EN UNA CAJA, para la demo del sitio.
#
# La imagen NO compila: recibe binarios ya publicados en `publicado/`. Es a proposito.
# `NuGet.config` apunta al feed privado de Azure donde vive Ncubo.Puppeteer, asi que un
# `dotnet restore` aca adentro necesitaria un PAT metido en la imagen o en un build secret.
# Publicar afuera -donde el feed ya esta autenticado- y copiar el resultado deja la imagen sin
# un solo secreto, y de paso baja de ~1.1 GB (SDK) a ~450 MB.
#
# Ver README.md para el `dotnet publish` que hay que correr antes.
#
# LO QUE ESTA IMAGEN ES: un escritorio Linux minimo con la ventana de Mymi abierta, y su
# pantalla servida al navegador por VNC sobre WebSocket. El visitante no baja nada.
FROM mcr.microsoft.com/dotnet/runtime:9.0-bookworm-slim

# ---------------------------------------------------------------- dependencias
#
# libwebkit2gtk-4.1-0 es LA dependencia que decide si esto arranca: Photino.Native no pinta
# nada sin ella, y arrastra gtk3 sola. En bookworm existe 4.1; en trixie tambien, pero varios
# paquetes de gtk cambiaron de nombre por la transicion a time_t de 64 bits (`libgtk-3-0t64`),
# asi que la base se queda en bookworm hasta que haga falta moverla.
#
# ATENCION: Mymi nunca corrio en Linux antes de este archivo. El repo no tiene una sola
# mencion de webkit2gtk, y la prueba manual cubre Windows y macOS. Si la ventana no abre, el
# primer sospechoso es la version de webkit contra la que enlaza Photino.Native.
#
# dbus-x11 y xauth no son decoracion: sin un dbus de sesion WebKit tira errores y a veces no
# arranca, y Xvfb sin xauth deja el display sin cookie.
#
# VERIFICADO el 2026-09-21 con `ldd Photino.Native.so` en una Ubuntu 26.04: la nativa pide
# libwebkit2gtk-4.1.so.0, libjavascriptcoregtk-4.1.so.0 (vienen las dos con el paquete 4.1),
# libgtk-3.so.0 y **libnotify.so.4**. Esa ultima no estaba en esta lista, y sin ella la ventana
# no abre con un DllNotFoundException que no dice cual falta.
RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
        libwebkit2gtk-4.1-0 libnotify4 \
        xvfb x11vnc openbox \
        novnc websockify \
        dbus-x11 xauth \
        fonts-dejavu-core fontconfig \
        ca-certificates tini \
    && rm -rf /var/lib/apt/lists/*

# ---------------------------------------------------------------- la persona del contenedor
#
# NUNCA root. El visitante controla lo que corre adentro: un navegador con una pestaña abierta
# es una consola, y si eso corre como root cualquier escape es un escape a root.
RUN useradd --create-home --shell /usr/sbin/nologin visita

# ---------------------------------------------------------------- Mymi y el modulo
#
# `publicado/mymi` y `publicado/host` los deja el publish de afuera. El .gmod viaja aparte y
# se instala en el arranque, no en el build: el alta escribe `installation.json`, que es un
# archivo DE LA PERSONA -- hornearlo en la imagen seria decidir por ella de fabrica.
WORKDIR /opt/mymi
COPY publicado/mymi/ ./bin/
COPY publicado/host/ ./host/
COPY paquetes/ ./paquetes/
COPY arranque.sh /usr/local/bin/arranque
RUN chmod +x /usr/local/bin/arranque /opt/mymi/bin/Gnothi.Desktop /opt/mymi/host/Gnothi.Host \
    && mkdir -p /opt/mymi/Modulos && chown -R visita:visita /opt/mymi

USER visita
ENV HOME=/home/visita \
    DISPLAY=:99 \
    PANTALLA=1440x900x24 \
    DOTNET_EnableWriteXorExecute=0 \
    # EL TECHO DEL GC, y es la leccion que costo 71 GB en el host de stages: un .NET sin
    # limite ve toda la RAM de la maquina como su presupuesto y no devuelve nada. En un
    # contenedor con `mem_limit` el GC ya lo respeta, pero declararlo hace que el numero este
    # escrito en un solo lugar y no dependa de como lo arranquen.
    DOTNET_GCHeapHardLimit=0x18000000

EXPOSE 6080

# tini como PID 1: sin el, los hijos que deja el arranque (Xvfb, x11vnc, websockify) quedan
# zombis y el contenedor no muere limpio cuando el broker lo termina.
ENTRYPOINT ["/usr/bin/tini", "--", "/usr/local/bin/arranque"]
