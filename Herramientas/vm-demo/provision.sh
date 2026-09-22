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
#   sudo ./provision.sh /ruta/al/publicado /ruta/a/los/gmod [/ruta/claude.deb] [/ruta/a/los/mocks]
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
#
# Y OJO CON --no-install-recommends PARA EL ESCRITORIO: con esa bandera, `xfce4` NO arrastra
# `xorg` y el greeter no arrastra lo suyo. El resultado es una imagen donde `virt-ls` muestra
# xfwm4, xfce4-session y lightdm instalados -- todo presente, nada funcional --, que arranca
# bien, alcanza graphical.target, y se queda en tty1 porque no hay servidor X que lanzar.
# Costó tres arranques descubrirlo (2026-09-21). Por eso el grupo del escritorio va CON
# recommends, y `xorg` va nombrado aparte para que sea explícito.
apt-get install -y --no-install-recommends \
    libwebkit2gtk-4.1-0 libnotify4 libicu74 \
    dbus-x11 fonts-dejavu-core ca-certificates xdg-utils

apt-get install -y \
    xorg xfce4 xfce4-terminal \
    lightdm lightdm-gtk-greeter

# EL IDIOMA, y no es cosmético: el catálogo de textos del Host resuelve por el locale del
# sistema. Sin esto la cloud image queda en C/POSIX, Mymi sale en inglés («My Connections»,
# «Add an institution…») y la demo muestra una cara que no es la que ve la persona en Costa
# Rica. Con es_CR salen los textos de `Gnothi.Host/textos.json`: «Mis conexiones», «Lo que cada
# institución te entregó», «Agregar una institución…».
#
# Y LAS DOS PIEZAS QUE NO SE VEN HASTA QUE FALTAN, las dos medidas el 2026-09-21 en esta imagen:
#
# `fonts-noto-color-emoji`: cuando una institución no declara logo, Mymi cae al dibujo de su
# rubro, que es un emoji (🩺 laboratorio, 🛒 supermercado, 🏦 banca…). Sin esta fuente el glifo
# sale como un CUADRO VACÍO -medido el 2026-09-21 en esta misma imagen-, y una institución sin
# marca queda con una caja rota al lado del nombre.
#
# `language-pack-gnome-es`: el selector de archivos lo dibuja GTK dentro del proceso de Mymi, y
# no lo traduce el catálogo del Host. Sin esto el título sale en español -es del Host- y el
# resto del diálogo en inglés: «Recent», «Home», «Size», «Other Locations».
apt-get install -y --no-install-recommends \
    locales language-pack-es language-pack-gnome-es fonts-noto-color-emoji
sed -i 's/^# *es_CR.UTF-8/es_CR.UTF-8/' /etc/locale.gen 2>/dev/null || true
grep -q "^es_CR.UTF-8" /etc/locale.gen 2>/dev/null || echo "es_CR.UTF-8 UTF-8" >> /etc/locale.gen
locale-gen es_CR.UTF-8 >/dev/null 2>&1 || true
update-locale LANG=es_CR.UTF-8 LANGUAGE=es_CR:es LC_ALL=es_CR.UTF-8 2>/dev/null || true
# Y también para la sesión gráfica, que no lee /etc/default/locale en todos los casos.
printf 'LANG=es_CR.UTF-8\nLANGUAGE=es_CR:es\n' > /etc/environment

echo "══ 1b/7 · el runtime de .NET 9"
# POR QUE EL RUNTIME Y NO SELF-CONTAINED, y es una decision forzada por el csproj del desktop:
# su TargetFramework condiciona sobre `IsOSPlatform('Windows')` -- el sistema donde se COMPILA,
# no el destino -- asi que publicar para Linux desde Windows no funciona. Se intentaron cinco
# variantes (forzar el framework, --force, restore explicito, limpiar artefactos sin RID, arbol
# de build aislado) y cada una destapa el sintoma siguiente del mismo origen: NETSDK1152 por
# salidas duplicadas, CS0579 por AssemblyInfo duplicado.
#
# Lo que SI funciona, y esta probado de punta a punta en WSL el 2026-09-21 -- ventana, alta de un
# hibrido y la cara de Delfos pintada --: el output de `dotnet build -r linux-x64`, que es
# framework-dependent. De ahi que la imagen traiga el runtime.
#
# EL ARREGLO DE FONDO es una linea en Gnothi.Desktop.csproj: que la condicion mire el
# RuntimeIdentifier y no el sistema anfitrion. Con eso el cross-publish anda y esta imagen puede
# volver a ser self-contained, sin depender de que Canonical empaquete .NET 9 ese anio.
#
# Y EL RUNTIME ES EL 10, NO EL 9, aunque el repo compile para net9.0.
#
# .NET 9 es STS y su soporte terminó en mayo de 2026: desapareció del feed de Microsoft para
# noble -- que hoy sólo ofrece 8.0 y 10.0, los dos LTS -- y Ubuntu 26.04 tampoco lo empaqueta.
# No hay de dónde sacarlo, y no vale la pena: un binario net9.0 corre sobre el runtime 10 con
# `DOTNET_ROLL_FORWARD=Major`, y así está probado (WSL, 2026-09-21: ventana, alta de un híbrido
# y la cara de Delfos pintada, todo sobre Microsoft.NETCore.App 10.0.12).
#
# OJO: el roll-forward tiene que estar en el ENTORNO DE LA APP, no sólo acá. Va en el autostart
# y en el lanzador del escritorio, más abajo.
if ! dotnet --list-runtimes 2>/dev/null | grep -qE "Microsoft.NETCore.App (9|10)"; then
    apt-get install -y --no-install-recommends wget
    wget -q https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O /tmp/ms.deb
    apt-get install -y /tmp/ms.deb && rm -f /tmp/ms.deb
    apt-get update
    apt-get install -y --no-install-recommends dotnet-runtime-10.0
fi

echo "══ 2/7 · Mymi"
install -d "$DESTINO"
cp -r "$PUBLICADO/mymi" "$DESTINO/bin"
cp -r "$PUBLICADO/host" "$DESTINO/host"
chmod +x "$DESTINO/bin/Gnothi.Desktop" "$DESTINO/host/Gnothi.Host"
# MODULOS ES DE LA PERSONA, NO DE ROOT, y esto costó un alta fallida. El script corre como
# root, así que todo lo que crea queda de root; pero el alta la hace la persona desde la
# ventana, y escribir el módulo en su carpeta es lo primero que intenta. Con Modulos de root
# el alta muere con «Access to the path ... is denied» en el primer clic -- y la inspección del
# disco no lo ve, porque los archivos SÍ están donde tienen que estar.
install -d -o "$PERSONA" -g "$PERSONA" "$DESTINO/Modulos"
install -d "$DESTINO/paquetes"
cp "$PAQUETES"/*.gmod "$DESTINO/paquetes/"

echo "══ 3/7 · los paquetes, a la vista"
#
# LA PERSONA DA EL ALTA, Y NO ESTE SCRIPT. Dos razones, y la segunda es la que manda:
#
# 1. `--install-module` es la puerta CARGO del Host: rechaza los hibridos por diseno
#    («this installer takes cargo modules only»). El alta de un hibrido entra por
#    `ModuleEnrollment`, y en el escritorio se llega por la pantalla de alta o por `--alta`.
#
# 2. `installation.json` es el archivo DE LA PERSONA. Hornearlo en la imagen seria decidir por
#    ella de fabrica, y justamente el gesto que vale mostrar en una demo es el otro: elegir el
#    paquete, leer la hoja de propuesta -forma hibrida, 19 archivos, MOTOR=0, la carpeta que el
#    Host le concede- y recien ahi aceptar.
#
# Asi que los .gmod quedan en una carpeta a la vista y se agregan desde Mymi con «Elegir el
# archivo…», que es como llega un paquete de verdad: por correo, por USB, por un enlace.
install -d -o "$PERSONA" -g "$PERSONA" "/home/$PERSONA/Entregas"
cp "$PAQUETES"/*.gmod "/home/$PERSONA/Entregas/" 2>/dev/null || true
chown "$PERSONA:$PERSONA" "/home/$PERSONA/Entregas/"*.gmod 2>/dev/null || true
ls -1 "/home/$PERSONA/Entregas" 2>/dev/null | sed 's/^/   · /'

echo "══ 3b/7 · (el alta la hace la persona, no este script)"
# EL TEMPORAL EN EL MISMO FILESYSTEM QUE EL DESTINO: el alta termina moviendo el modulo desde el
# temporal a su carpeta, y un move entre dos filesystems falla con EXDEV («Invalid cross-device
# link»). Medido el 2026-09-21: /tmp suele ser tmpfs y /opt ext4, asi que sin esto el alta baja
# el bundle, lo examina bien, y muere al instalar.
install -d -m 1777 -o "$PERSONA" -g "$PERSONA" "$DESTINO/tmp"
export TMPDIR="$DESTINO/tmp"

echo "══ 3c/7 · los cinco publicadores, adentro"
#
# QUE ES ESTO Y POR QUE VA EN LA IMAGEN. Cada institucion tiene su sistema -- el laboratorio,
# el super, el concesionario, la municipalidad, la estacion --, y el modulo no inventa datos:
# sale a buscarlos. Sin un publicador al otro lado, las cinco caras se dan de alta y se quedan
# vacias, y la demo pierde justo lo que tiene para mostrar.
#
# Corren en 127.0.0.1, cada uno en su puerto, que es el que el manifiesto de su .gmod declara:
#
#     examenes  :8091    dealership     :8092    compras  :8101
#     cargas    :8111    municipalidad  :8121
#
# NO HAY QUE SEMBRARLES NADA, y esto costo una vuelta en falso: los cinco mocks **nacen con su
# historia adentro**. Correrles el `InyectorDeEventosDemo historia` encima POSTEA EL MISMO GUION
# con ids nuevos y duplica todo. El inyector es para agregar un hecho EN VIVO, mientras alguien
# mira, no para llenar la imagen.
#
# Guardan en memoria, asi que lo inyectado en vivo se pierde al apagar y la historia de fabrica
# vuelve siempre. Para una demo es la conducta que se quiere.
MOCKS=${4:-}
if [[ -n "$MOCKS" && -d "$MOCKS" ]]; then
    install -d "/opt/mocks"
    declare -A PUERTO=( [examenes]=8091 [concesionario]=8092 [compras]=8101 [cargas]=8111 [municipalidad]=8121 )
    for n in "${!PUERTO[@]}"; do
        [[ -d "$MOCKS/$n/mock" ]] || continue
        cp -r "$MOCKS/$n" "/opt/mocks/$n"
        bin=$(find "/opt/mocks/$n/mock" -maxdepth 1 -type f ! -name '*.so' ! -name '*.json' ! -name '*.dll' ! -name '*.pdb' | head -1)
        [[ -n "$bin" ]] || continue
        chmod +x "/opt/mocks/$n"/*/* 2>/dev/null || true
        cat > "/etc/systemd/system/$n-mock.service" <<UNIT
[Unit]
Description=Publicador simulado de $n -- el negocio, no Mymi
After=network.target

[Service]
Type=simple
User=$PERSONA
WorkingDirectory=/opt/mocks/$n/mock
Environment=ASPNETCORE_URLS=http://127.0.0.1:${PUERTO[$n]}
Environment=DOTNET_ROLL_FORWARD=Major
ExecStart=$bin
Restart=on-failure
RestartSec=3

[Install]
WantedBy=multi-user.target
UNIT
        systemctl enable "$n-mock.service" >/dev/null 2>&1 || true
        echo "   · $n en :${PUERTO[$n]}"
    done
    chown -R "$PERSONA:$PERSONA" /opt/mocks

    # LOS APORTES DE LA PERSONA SOBRE EL REGISTRO DE FARO. La estacion manda litros y precios;
    # el ODOMETRO no lo manda nadie, porque no lo sabe nadie mas que ella -- y sin dos tanques
    # llenos con kilometraje no hay tramo cerrado ni km/L. Esto se los anota para que la demo
    # muestre la serie (12,6 km/L cayendo a 9,4) en vez de quedarse en «todavia no puedo
    # calcular». No inventa nada que una institucion debiera haber mandado.
    #
    # VA POR TIMER Y NO POR .path, y el .path se probo primero: systemd necesita que el
    # directorio PADRE exista para poner el inotify, y `~/Documentos/Gnothi/` no existe hasta
    # que la persona da el alta. La unidad arranca, no encuentra a quien vigilar, y se queda
    # callada sin error -- que es la peor forma de fallar.
    if [[ -d "/opt/mocks/cargas/consola" ]]; then
        install -o "$PERSONA" -g "$PERSONA" -m 0755 \
            "$(dirname "$0")/faro-aportes.sh" /opt/mocks/cargas/sembrar-aportes.sh 2>/dev/null || true
        cat > /etc/systemd/system/faro-aportes.service <<'UNIT'
[Unit]
Description=Anota los aportes de la persona sobre el registro de Faro
ConditionPathExists=!/opt/mocks/cargas/.aportes-hechos

[Service]
Type=oneshot
User=demo
ExecStart=/opt/mocks/cargas/sembrar-aportes.sh
UNIT
        cat > /etc/systemd/system/faro-aportes.timer <<'UNIT'
[Unit]
Description=Pregunta por el registro de Faro hasta que tenga sus cargas

[Timer]
OnBootSec=45s
OnUnitActiveSec=45s
AccuracySec=5s
Unit=faro-aportes.service

[Install]
WantedBy=timers.target
UNIT
        systemctl enable faro-aportes.timer >/dev/null 2>&1 || true
        echo "   · los aportes de Faro se anotan solos al minuto de que traiga sus cargas"
    fi
else
    echo "   ! sin carpeta de mocks (4to argumento): las cinco caras van a salir vacias" >&2
fi

echo "══ 4/7 · el cliente de chat"
# YA NO HACE FALTA CONSEGUIR EL .deb A MANO. Desde junio de 2026 Anthropic publica Claude
# Desktop para Linux (beta) en su propio repositorio apt, asi que se instala como cualquier
# paquete y las actualizaciones llegan con las del sistema. El tercer argumento se sigue
# aceptando por si hay que fijar una version concreta o la maquina no llega al repositorio.
#
# `--no-install-recommends` A PROPOSITO: los recomendados de claude-desktop son qemu-system-x86,
# ovmf y virtiofsd -- porque Cowork corre sus tareas en una VM propia --, o sea media maquina
# virtual adentro de esta. Son cientos de MB sobre un OVA que ya pesa 1 GB, y anidar
# virtualizacion adentro de una demo no va a andar igual. El chat y el conector MCP no los
# necesitan; Cowork queda fuera y la pantalla lo dice cuando se abre.
if [[ -n "$CLAUDE_DEB" && -f "$CLAUDE_DEB" ]]; then
    apt-get install -y "$CLAUDE_DEB"
else
    apt-get install -y --no-install-recommends curl gnupg
    curl -fsSLo /usr/share/keyrings/claude-desktop-archive-keyring.asc \
        https://downloads.claude.ai/claude-desktop/key.asc
    # LA HUELLA SE COMPRUEBA, no se asume: una clave equivocada hace que `apt update` falle mas
    # tarde con NO_PUBKEY y el error no dice que el problema fue la descarga.
    if ! gpg --show-keys /usr/share/keyrings/claude-desktop-archive-keyring.asc 2>/dev/null \
         | tr -d ' ' | grep -q 31DDDE24DDFAB679F42D7BD2BAA929FF1A7ECACE; then
        echo "   ! la clave de Anthropic no es la esperada; no instalo Claude" >&2
        CLAUDE_DEB=""
    else
        echo "deb [arch=amd64,arm64 signed-by=/usr/share/keyrings/claude-desktop-archive-keyring.asc] https://downloads.claude.ai/claude-desktop/apt/stable stable main" \
            > /etc/apt/sources.list.d/claude-desktop.list
        apt-get update -qq
        apt-get install -y --no-install-recommends claude-desktop
    fi
fi
if command -v claude-desktop >/dev/null 2>&1; then

    # EL ARCHIVO TIENE QUE EXISTIR ANTES, y esto es lo que hace que la VM salga con el conector
    # puesto. HostInstaller busca ~/.config/Claude/claude_desktop_config.json y lo agrega SOLO si
    # ya existe (`if (File.Exists(unpackaged) ...)`), porque en una maquina normal lo crea la app
    # en su primer arranque. En una imagen recien provisionada Claude nunca se abrio, asi que sin
    # esto `--install` no lo encuentra y la VM sale sin conector -- con el sintoma clasico:
    # «configure Gnothi y el asistente no lo ve».
    install -d -o "$PERSONA" -g "$PERSONA" "/home/$PERSONA/.config/Claude"
    [ -f "/home/$PERSONA/.config/Claude/claude_desktop_config.json" ] || \
        echo '{ "mcpServers": {} }' > "/home/$PERSONA/.config/Claude/claude_desktop_config.json"
    chown "$PERSONA:$PERSONA" "/home/$PERSONA/.config/Claude/claude_desktop_config.json"

    # EL CONECTOR LO ESCRIBE EL HOST, no nosotros a mano: `--install` ya conoce el
    # claude_desktop_config.json de Claude Desktop y el ~/.codex/config.toml de Codex, y elige
    # segun lo que encuentre instalado. Escribirlo a mano seria mantener el formato en dos
    # lugares.
    # LAS DOS COSAS QUE HACIAN QUE ESTA LINEA NO REGISTRARA NADA, las dos medidas el 2026-09-22
    # y las dos fallando EN SILENCIO -- el comando terminaba con exito y el conector no quedaba:
    #
    # 1. `su - demo -c` Y NO `sudo -u demo`. Sin `-H`, sudo deja HOME=/root, y `HostInstaller`
    #    resuelve el config con `ApplicationData` -- o sea `$HOME/.config` --, asi que buscaba el
    #    de root, no lo encontraba, y no ofrecia ningun asistente.
    #
    # 2. `--install` ES INTERACTIVO: elige asistente de una lista y pide confirmacion `[y/N]`.
    #    Sin stdin contesta que NO y cierra diciendo «I wrote nothing», con exit 0. De ahi el
    #    `printf "1\ny\n"`: la opcion 1 es Claude Desktop cuando esta instalado.
    su - "$PERSONA" -c "printf '1\ny\n' | '$DESTINO/host/Gnothi.Host' --install '$DESTINO/Modulos'" \
        || echo "   ! el registro del conector fallo" >&2
    # Y COMPROBARLO, porque las dos fallas de arriba no se notan: si la clave no quedo, la VM
    # sale con el sintoma clasico -- «configure Gnothi y el asistente no lo ve».
    if grep -q '"gnothi"' "/home/$PERSONA/.config/Claude/claude_desktop_config.json" 2>/dev/null; then
        echo "   · conector 'gnothi' registrado en Claude Desktop"
    else
        echo "   ! el conector NO quedo en claude_desktop_config.json" >&2
    fi
else
    echo "   ! Claude Desktop no quedo instalado: instalalo y corre 'Gnothi.Host --install'" >&2
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
Exec=env DOTNET_ROLL_FORWARD=Major TMPDIR=$DESTINO/tmp $DESTINO/bin/Gnothi.Desktop $DESTINO/Modulos
X-GNOME-Autostart-enabled=true
EOF

# EL ESCRITORIO SE LLAMA COMO EL IDIOMA DIGA, y hay que preguntarlo, no suponerlo. Crear
# «Desktop» a mano deja una carpeta en inglés entre «Documentos», «Descargas» e «Imágenes»:
# `xdg-user-dirs-update` crea las demás traducidas en la primera sesión, pero RESPETA la que ya
# existe en vez de renombrarla. Funciona -- el lanzador y la nota igual se ven --, pero se ve
# mal. Medido el 2026-09-21 en esta imagen, después de instalar `language-pack-gnome-es`.
sudo -u "$PERSONA" env LANG=es_CR.UTF-8 xdg-user-dirs-update --force 2>/dev/null || true
ESCRITORIO=$(sudo -u "$PERSONA" env LANG=es_CR.UTF-8 xdg-user-dir DESKTOP 2>/dev/null || true)
[ -n "$ESCRITORIO" ] || ESCRITORIO="/home/$PERSONA/Escritorio"

# Y un lanzador en el escritorio, para cuando la cierren y quieran volver a abrirla.
install -d "$ESCRITORIO"
cat > "$ESCRITORIO/Mymi.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=Mymi
Comment=Tus instituciones, en esta computadora
Exec=env DOTNET_ROLL_FORWARD=Major TMPDIR=$DESTINO/tmp $DESTINO/bin/Gnothi.Desktop $DESTINO/Modulos
Terminal=false
EOF
chmod +x "$ESCRITORIO/Mymi.desktop"

echo "══ 6/7 · la nota en el escritorio"
cat > "$ESCRITORIO/LEEME.txt" <<'EOF'
                        M Y M I  ·  demo

Tus instituciones, en esta computadora. Mymi no le pregunta nada a ninguna
empresa: lee lo que cada una ya dejo aca, en una carpeta tuya.


QUE PROBAR, EN ORDEN
────────────────────

1. DAR DE ALTA UNA INSTITUCION.  En Mymi: «Elegir el archivo...» y busca
   uno de los cinco paquetes en la carpeta Entregas de tu home.

   Lee la hoja que sale ANTES de instalar: quien es, que va a poder
   contestar, donde va a vivir tu registro y con quien va a hablar. Esa
   pantalla es el producto.

2. ABRI SU PANTALLA.  Es la cara que diseño la institucion, corriendo
   adentro de Mymi con el motor prestado. Al abrirla sale a buscar lo que
   haya nuevo y lo escribe en TU carpeta.

3. CONSULTA.  Los dos botones «Consultar» son la otra puerta: la misma que
   usa un asistente por MCP. Leen el mismo registro que pinto la pantalla.


LAS CINCO QUE VIENEN
────────────────────

   Delfos              laboratorio    tus examenes, con su historia
   La Guaria           supermercado   87 compras, que subio y que cambiaste
   Sotaventos Motors   concesionario  el plan de tu YARIS-2019
   Monteverde          municipalidad  por que subio cada rubro
   Faro                combustible    tus cargas, tramo a tramo

Los cinco publicadores corren adentro, cada uno en su puerto de 127.0.0.1.
No sale nada de esta maquina.


FARO PIDE ALGO QUE SOLO VOS TENES
─────────────────────────────────

La primera vez que abras Faro va a decir que todavia no puede calcular el
rendimiento. No es un error: sus 28 cargas llegaron, pero el KILOMETRAJE no
lo manda la estacion -nadie sabe el odometro de tu carro salvo vos-, y sin
dos tanques llenos con kilometraje no hay tramo que medir.

Es, de hecho, lo que el producto quiere mostrar: hay datos que no son de
ninguna institucion.

Para que no tengas que teclear 17 formularios, esta demo los anota sola:
al minuto de que Faro traiga sus cargas, quedan puestos.

   PARA VERLOS, CERRA MYMI Y VOLVE A ABRIRLA  (el lanzador esta en el
   escritorio). Mymi tiene tu registro abierto mientras corre, asi que lo
   que se anota por fuera lo ve al reabrirse, no en el acto.

Despues de eso Faro muestra la serie entera: venias rindiendo 12,6 km/L y
el ultimo tramo dio 9,4, un 25 % menos.

Si preferis anotarlos vos, o volver a correrlo:

   cd /opt/mocks/cargas/consola
   DOTNET_ROLL_FORWARD=Major ./Cargas.Consola sembrar-aportes \
       ~/Documentos/Gnothi/cargas/cargas


AGREGAR ALGO EN VIVO, MIENTRAS ALGUIEN MIRA
───────────────────────────────────────────

Cada negocio trae su inyector en /opt/mocks/<negocio>/inyector. Agregan un
hecho al publicador; despues se aprieta Actualizar en la institucion y
aparece.

   cd /opt/mocks/compras/inyector
   DOTNET_ROLL_FORWARD=Major ./Compras.InyectorDeEventosDemo

Sin argumentos imprime su ayuda. En el de Delfos, el ANTIGENO PROSTATICO no
esta en la historia a proposito: inyectarlo y despues declarar que la
persona es mujer muestra que la app lo esconde.


EL ASISTENTE
────────────

Claude Desktop viene con el conector de Mymi ya registrado. La cuenta es
tuya: la sesion no viaja en esta imagen.


   usuario demo · contrasena mymi
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
