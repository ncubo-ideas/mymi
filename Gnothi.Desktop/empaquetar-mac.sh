#!/usr/bin/env bash
# LA VENTANA DE ESCRITORIO COMO APP DE macOS. Arma Mymi.app: publica la ventana autocontenida
# adentro de Contents/MacOS/, le pone el Info.plist de al lado, y le genera el icono .icns.
#
#     ./empaquetar-mac.sh                 # desde Gnothi.Desktop/, deja bin/mac/Mymi.app
#     ./empaquetar-mac.sh ~/Applications  # o donde se quiera
#
# POR QUE HACE FALTA. Sin bundle la ventana corre igual, pero el Dock la nombra «Gnothi.Desktop»
# y le pone el icono de GIMP. El por que de cada una de esas dos cosas -y por que ninguna se
# arregla desde C#- esta medido y escrito en Info.plist, que es donde se arreglan.
#
# ESTE ES EL PRIMER EMPAQUETADO DE MAC DEL REPO. Gnothi.Installer son tres .wxs de WiX, o sea
# Windows, y publicar-mac.sh publica el HOST -headless, sin ventana- y ahi un bundle no pinta
# nada. Este script es a la ventana lo que aquel es al Host.
#
# NO REEMPLAZA A `dotnet run`. El camino de desarrollo sigue siendo
#
#     dotnet run --project Gnothi.Desktop -- <carpeta-de-modulos>
#
# que es lo que dicen PRUEBA-MANUAL-DESKTOP.md y los guiones, y no cambia: este script no toca
# el csproj ni el AssemblyName. Empaquetar es un paso de mas, no un paso en el medio.
#
# AUTOCONTENIDO, y no por gusto: una .app que al doble clic pide instalar .NET no es una app.
# Pesa ~90 MB y ese es el precio. Framework-dependent seria mas chico y solo correria en una Mac
# con el SDK, que es la maquina de quien la compila y de nadie mas.
set -euo pipefail
cd "$(dirname "$0")"

say() { printf '\n\033[1m%s\033[0m\n' "$*"; }
ok()  { printf '  \033[32m✔ %s\033[0m\n' "$*"; }
bad() { printf '  \033[31m✘ %s\033[0m\n' "$*"; exit 1; }

[ "$(uname -s)" = "Darwin" ] || bad "esto es un bundle de macOS: en Windows el instalador es Gnothi.Installer"
command -v iconutil >/dev/null || bad "falta iconutil, que viene con las herramientas de linea de comandos de Xcode"
command -v dotnet   >/dev/null || bad "falta el SDK de .NET 9"

DESTINO="${1:-bin/mac}"
APP="$DESTINO/Mymi.app"
# EL NOMBRE DEL EJECUTABLE ES CONTRATO: tiene que ser el AssemblyName del csproj Y el
# CFBundleExecutable del plist. Se lee del csproj en vez de repetirlo, para que el dia que
# alguien lo cambie esto falle aca y no con un doble clic que no hace nada.
EJECUTABLE="$(sed -n 's:.*<AssemblyName>\(.*\)</AssemblyName>.*:\1:p' Gnothi.Desktop.csproj | head -1)"
DECLARADO="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' Info.plist)"
[ "$EJECUTABLE" = "$DECLARADO" ] || bad "el csproj dice '$EJECUTABLE' y el Info.plist dice '$DECLARADO'"

case "$(uname -m)" in
	arm64)  RID=osx-arm64 ;;
	x86_64) RID=osx-x64 ;;
	*)      bad "no se que RID le toca a $(uname -m)" ;;
esac

say "1 · publicar la ventana ($RID, autocontenida)"
rm -rf "$APP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
# SIN PublishSingleFile, a diferencia del Host: el unico archivo se descomprime en /var/folders
# en cada arranque, y lo que la persona ve nombrado y firmado deja de ser lo que hay adentro del
# bundle. Una .app ya es un solo icono; el archivo unico no compra nada aca y complica la firma.
#
# ErrorOnDuplicatePublishOutputFiles ES NECESARIO, Y NO ES UN PARCHE A CIEGAS. Medido: sin el,
# el publish muere con NETSDK1152 porque Gnothi.Host SE COMPILA DOS VECES, con RID y sin el.
# La cadena es Desktop -> Host y Desktop -> Assistant -> Host: el SDK le pasa el RID a un
# ProjectReference que es Exe -Host- y NO a uno que es biblioteca -Assistant-, asi que el
# Assistant se construye sin RID y arrastra un segundo Host sin RID. De ahi salen dos apphost,
# dos deps.json y dos runtimeconfig.json con la misma ruta relativa. Lo que se duplica es del
# HOST y no de la ventana; el Gnothi.Desktop.deps.json que manda al arranque es uno solo.
dotnet publish . -c Release -r "$RID" --self-contained true \
	-p:ErrorOnDuplicatePublishOutputFiles=false -o "$APP/Contents/MacOS" --nologo | tail -3
[ -x "$APP/Contents/MacOS/$EJECUTABLE" ] || bad "el publish no dejo un ejecutable en Contents/MacOS/$EJECUTABLE"

# Y EL POLIZON SE BAJA ACA. De esa duplicacion queda adentro un ejecutable Gnothi.Host que gano
# la version SIN RID: framework-dependent, o sea que en una Mac sin .NET no corre. Un binario
# que no corre, con el nombre justo para que alguien lo busque, es una trampa. El Host de verdad
# lo publica y lo firma Gnothi.Host/publicar-mac.sh; la ventana no lo lanza, enlaza su DLL y
# llama al Dispatcher en este mismo proceso. Gnothi.Host.dll SE QUEDA: eso es el nucleo.
rm -f "$APP/Contents/MacOS/Gnothi.Host" "$APP/Contents/MacOS/Gnothi.Host.deps.json" \
      "$APP/Contents/MacOS/Gnothi.Host.runtimeconfig.json"
[ -f "$APP/Contents/MacOS/Gnothi.Host.dll" ] || bad "se fue Gnothi.Host.dll, que es el nucleo"
ok "$(du -sh "$APP" | cut -f1) en $APP"

say "2 · el icono"
# EL MISMO DIBUJO QUE LOS DOS TELEFONOS, enlazado y no copiado: la marca vive en un solo archivo
# y el .icns es material generado, como la dylib del puente. Si se copiara aca, el dia que la
# marca cambie habria tres caras y una cuarta vieja.
FUENTE="../Gnothi.Mobile/Assets/img/mymi-hub.png"
[ -f "$FUENTE" ] || bad "no encuentro el dibujo de Mymi en $FUENTE"
LADO="$(sips -g pixelWidth "$FUENTE" | sed -n 's/.*pixelWidth: //p')"
ICONSET="$(mktemp -d)/mymi.iconset"
mkdir -p "$ICONSET"
# HASTA 512 Y NADA MAS. El .icns admite 1024 (icon_512x512@2x) y el dibujo mide 512: agrandarlo
# seria inventar pixeles y se ve. macOS escala solo lo que falta, y de bajar nunca se pierde.
for PAR in "16 icon_16x16" "32 icon_16x16@2x" "32 icon_32x32" "64 icon_32x32@2x" \
           "128 icon_128x128" "256 icon_128x128@2x" "256 icon_256x256" "512 icon_256x256@2x" \
           "512 icon_512x512"; do
	PX="${PAR% *}"; NOMBRE="${PAR#* }"
	[ "$PX" -le "$LADO" ] || continue
	sips -z "$PX" "$PX" "$FUENTE" --out "$ICONSET/$NOMBRE.png" >/dev/null
done
TAMANOS="$(ls "$ICONSET" | wc -l | tr -d ' ')"
iconutil -c icns "$ICONSET" -o "$APP/Contents/Resources/mymi.icns"
rm -rf "$(dirname "$ICONSET")"
ok "mymi.icns con $TAMANOS tamanos, de un original de ${LADO}x${LADO}"

say "3 · el bundle"
cp Info.plist "$APP/Contents/Info.plist"
# Los cuatro bytes que Carbon leia antes de que existiera el Info.plist. Hoy no hacen falta,
# pero cuestan nada y algunas herramientas viejas todavia los miran.
printf 'APPL????' > "$APP/Contents/PkgInfo"
plutil -lint "$APP/Contents/Info.plist" >/dev/null || bad "el Info.plist no parsea"
ok "Info.plist · CFBundleName=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleName' "$APP/Contents/Info.plist")"

say "4 · firmar"
# LA MISMA REGLA QUE publicar-mac.sh, y por la misma razon: adhoc corre pero cambia de cdhash
# en cada build, asi que el sistema ve una app nueva cada vez -y con ella se van el permiso que
# ya diste y el lugar que la app tenia en el Dock. Se puede forzar con GNOTHI_SIGN_ID.
IDENTIDAD="${GNOTHI_SIGN_ID:-}"
if [ -z "$IDENTIDAD" ]; then
	IDENTIDAD=$(security find-identity -v -p codesigning 2>/dev/null \
		| sed -n 's/.*"\(Developer ID Application:[^"]*\)".*/\1/p' | head -1)
	[ -n "$IDENTIDAD" ] || IDENTIDAD=$(security find-identity -v -p codesigning 2>/dev/null \
		| sed -n 's/.*"\(Apple Development:[^"]*\)".*/\1/p' | head -1)
fi
if [ -z "$IDENTIDAD" ]; then
	IDENTIDAD="-"
	printf '  \033[33m! sin identidad en el llavero: queda adhoc. Corre igual; macOS la va a\n    tratar como una app distinta en cada build.\033[0m\n'
fi
# --deep ESTA DEPRECADO Y ACA SIGUE SIENDO LO CORRECTO. Lo que hay adentro son las dylib de
# Photino y del runtime, que el publish deja firmadas ADHOC. Sin --deep la app corre igual y
# hasta verifica -medido: `codesign --verify --strict` pasa-, pero adentro quedan dos
# identidades: la de afuera y la de nadie. Eso es justo lo que rechaza la notarizacion el dia
# que esta app tenga que salir de la Mac que la compilo.
codesign --force --deep --sign "$IDENTIDAD" --identifier "$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$APP/Contents/Info.plist")" \
	--timestamp=none "$APP" 2>&1 | grep -v "replacing existing signature" || true
codesign -dv "$APP" 2>&1 | grep -E "^(Identifier|TeamIdentifier|Authority)=|Signature=" || true
codesign --verify --strict "$APP" && ok "firma valida"

say "listo"
cat <<FIN
  $APP

  Al doble clic busca los modulos en Contents/MacOS/Modulos -la forma instalada, la misma que
  el MSI arma en Windows. Para apuntarla a otra carpeta sin moverla:

      open "$APP" --args <carpeta-de-modulos>

  Y para verla en el Dock con su nombre y su cara, arrastrala a /Applications.
FIN
