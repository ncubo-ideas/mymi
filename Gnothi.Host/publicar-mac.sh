#!/usr/bin/env bash
# PUBLICA EL HOST EN UNA MAC Y LO FIRMA CON UNA IDENTIDAD ESTABLE.
#
# POR QUE NO ALCANZA CON dotnet publish. El binario que sale de publish queda firmado adhoc:
# sin Team ID, y TCC -el permiso de macOS- no tiene entonces identidad con que reconocerlo, asi
# que guarda la decision contra ruta + cdhash. Cada republicacion cambia el cdhash, TCC ve una
# app nueva, y --install vuelve a pedir "Gnothi.Host would like to access data from other apps"
# aunque ya se lo hayas concedido tres veces. Medido el 2026-09-14.
#
# El permiso lo pide HostInstaller.Find(), que abre ~/Library/Application Support/Claude/
# claude_desktop_config.json para registrar a Gnothi. Es Application Support de OTRA app, que es
# exactamente lo que cubre kTCCServiceSystemPolicyAppData. El acceso es legitimo y no hay como
# esquivarlo: para registrarse en Claude Desktop hay que abrir ese archivo. Lo que si se arregla
# es que el permiso SOBREVIVA, y para eso la firma tiene que ser la misma entre builds.
#
# Servir por MCP no toca nada de eso: el prompt sale en --install, no atendiendo preguntas.
#
#     ./publicar-mac.sh                 # publica en ~/Gnothi
#     ./publicar-mac.sh /tmp/prueba     # publica en otro lado, para probar sin tocar lo vivo
#
# La identidad sale del llavero, prefiriendo Developer ID sobre Apple Development. Se puede
# forzar con GNOTHI_SIGN_ID="Apple Development: fulano (TEAMID)".
set -euo pipefail
cd "$(dirname "$0")/.."

DESTINO="${1:-$HOME/Gnothi}"
# El identificador NO puede salir del nombre del archivo, que es lo que hace codesign solo: ahi
# el auto-generado cambia con el contenido. Fijo, es la mitad estable del designated requirement.
IDENTIFICADOR="com.ncubo.gnothi.host"

echo "-- publicando en $DESTINO"
# El dotnet de esta maquina es el x64 bajo Rosetta y produce el arm64 igual con -r osx-arm64.
dotnet publish Gnothi.Host -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true -o "$DESTINO"

BINARIO="$DESTINO/Gnothi.Host"
[ -x "$BINARIO" ] || { echo "!  no quedo un ejecutable en $BINARIO"; exit 1; }

# QUE IDENTIDAD. Developer ID es la que no caduca en un ano y sirve en otras maquinas; Apple
# Development alcanza para que el permiso sobreviva en ESTA. Cualquiera de las dos es mejor que
# adhoc, que es no tener ninguna.
if [ -n "${GNOTHI_SIGN_ID:-}" ]; then
	IDENTIDAD="$GNOTHI_SIGN_ID"
else
	IDENTIDAD=$(security find-identity -v -p codesigning 2>/dev/null \
		| sed -n 's/.*"\(Developer ID Application:[^"]*\)".*/\1/p' | head -1)
	[ -n "$IDENTIDAD" ] || IDENTIDAD=$(security find-identity -v -p codesigning 2>/dev/null \
		| sed -n 's/.*"\(Apple Development:[^"]*\)".*/\1/p' | head -1)
fi

if [ -z "$IDENTIDAD" ]; then
	# NO ES UN ERROR: el binario adhoc corre igual. Lo que no hace es conservar el permiso, y
	# eso hay que decirlo aca y no dejar que se descubra con el diálogo numero cuatro.
	echo
	echo "!  No hay identidad de firma en el llavero: queda adhoc."
	echo "   El Host funciona, pero macOS va a volver a pedir permiso en el proximo --install."
	echo "   Con una identidad: GNOTHI_SIGN_ID=\"Apple Development: ...\" ./publicar-mac.sh"
	exit 0
fi

echo "-- firmando con: $IDENTIDAD"
codesign --force --sign "$IDENTIDAD" --identifier "$IDENTIFICADOR" --timestamp=none "$BINARIO"

# EL VEREDICTO ES LO QUE QUEDO FIRMADO, no que codesign no haya fallado. Si esta linea dice
# adhoc o no dice TeamIdentifier, el permiso se va a volver a pedir y mas vale verlo ahora.
echo
codesign -dvvv "$BINARIO" 2>&1 | grep -E "^(Identifier|TeamIdentifier|Authority)=|flags="
echo
echo "Listo. Para registrarlo:  $BINARIO --install <carpeta-de-modulos>"
