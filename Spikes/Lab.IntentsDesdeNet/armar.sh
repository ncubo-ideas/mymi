#!/usr/bin/env bash
# ¿PUEDE UNA APP DE .NET LLEVAR APP INTENTS ADENTRO? Este script lo mide en cuatro pasos y
# dice, en cada uno, que se espera y que significa cada resultado. Correr en la Mac, desde
# esta carpeta, con un simulador ya abierto.
#
#     ./armar.sh
#
# LEER LA SALIDA DE ARRIBA HACIA ABAJO: cada paso imprime un veredicto. El primero que falle
# es la respuesta, y los de despues no valen.
set -euo pipefail
cd "$(dirname "$0")"

say()  { printf '\n\033[1m%s\033[0m\n' "$*"; }
ok()   { printf '  \033[32m✔ %s\033[0m\n' "$*"; }
bad()  { printf '  \033[31m✘ %s\033[0m\n' "$*"; }
info() { printf '  · %s\n' "$*"; }

# ── 0 · el aparato y las herramientas ──────────────────────────────────────────────────────
# Se deduce la arquitectura de la Mac: Apple Silicon simula arm64, Intel simula x86_64. Se
# puede forzar con ARCH=x86_64 ./armar.sh
ARCH="${ARCH:-$(uname -m)}"
case "$ARCH" in
  arm64)  RID="iossimulator-arm64" ;;
  x86_64) RID="iossimulator-x64" ;;
  *) bad "arquitectura desconocida: $ARCH"; exit 1 ;;
esac
TRIPLE="${ARCH}-apple-ios16.0-simulator"
SDK="$(xcrun --sdk iphonesimulator --show-sdk-path)"
TC="$(xcode-select -p)/Toolchains/XcodeDefault.xctoolchain"
# El procesador pide la version de BUILD de Xcode (16C5032a), no la de marketing (16.2).
XCV="$(xcodebuild -version | awk '/Build version/ {print $3}')"

say "0 · entorno"
info "arch=$ARCH  rid=$RID  triple=$TRIPLE"
info "sdk=$SDK"
info "xcode build=$XCV"
xcrun --find appintentsmetadataprocessor >/dev/null 2>&1 \
  && ok "appintentsmetadataprocessor esta en el toolchain" \
  || { bad "no encuentro appintentsmetadataprocessor -- ¿Xcode completo instalado, no solo Command Line Tools?"; exit 1; }

# ── 1 · Swift → libreria estatica + const values ───────────────────────────────────────────
# swiftc DIRECTO, sin proyecto de Xcode, para controlar cada flag. El que importa es
# -emit-const-values-path: es lo que el procesador de metadata lee. Xcode lo pone solo cuando
# ve AppIntents enlazado; aca lo ponemos nosotros.
say "1 · compilar Swift a libreria estatica"
rm -rf Swift/out && mkdir -p Swift/out
xcrun swiftc \
  -parse-as-library -wmo \
  -module-name GnothiIntents \
  -target "$TRIPLE" -sdk "$SDK" \
  -emit-library -static -o Swift/out/libGnothiIntents.a \
  -emit-module -emit-module-path Swift/out/GnothiIntents.swiftmodule \
  -emit-const-values-path Swift/out/GnothiIntents.swiftconstvalues \
  Swift/GnothiIntents.swift
ok "libGnothiIntents.a  $(stat -f%z Swift/out/libGnothiIntents.a) bytes"
if [ -s Swift/out/GnothiIntents.swiftconstvalues ]; then
  ok "GnothiIntents.swiftconstvalues  $(stat -f%z Swift/out/GnothiIntents.swiftconstvalues) bytes"
else
  bad "no salio el .swiftconstvalues -- sin el, el procesador no tiene de donde extraer"
  exit 1
fi

# ── 2 · la app de .NET, con la libreria enlazada ───────────────────────────────────────────
say "2 · dotnet build"
dotnet build -f net9.0-ios -r "$RID" -c Debug -v:quiet -clp:NoSummary
APP="bin/Debug/net9.0-ios/$RID/Lab.IntentsDesdeNet.app"
BIN="$APP/Lab.IntentsDesdeNet"
[ -d "$APP" ] || { bad "no aparecio $APP"; exit 1; }
ok "$APP"

# ¿Sobrevivio la conformance al linker? Sin ForceLoad la borra porque nadie la llama por
# codigo. Los nombres Swift van mangleados pero contienen el nombre del tipo, asi que grep
# alcanza.
if nm "$BIN" 2>/dev/null | grep -q ProbarDesdeNetIntent; then
  ok "ProbarDesdeNetIntent esta en el ejecutable (ForceLoad funciono)"
else
  bad "ProbarDesdeNetIntent NO esta en el ejecutable: el linker la borro. Revisar ForceLoad en el csproj."
  exit 1
fi
if otool -L "$BIN" | grep -q AppIntents.framework; then
  ok "el ejecutable carga AppIntents.framework"
else
  bad "el ejecutable NO carga AppIntents.framework: el runtime ni va a mirar la metadata. Revisar <Frameworks> en el csproj."
  exit 1
fi

# ── 3 · LA HIPOTESIS: ¿el build de .NET genero la metadata? ────────────────────────────────
say "3 · ¿hay Metadata.appintents en el bundle, tal como lo dejo dotnet build?"
if [ -d "$APP/Metadata.appintents" ]; then
  ok "SI esta. La hipotesis era FALSA: el build de .NET si la genera. #24863 es otra cosa."
  ls -la "$APP/Metadata.appintents"
else
  bad "NO esta. Hipotesis CONFIRMADA: el build de .NET no corre appintentsmetadataprocessor."
  info "Esto es lo que el reportero de #24863 nunca miro. Sigue el paso 4."
fi

# ── 4 · generarla aparte y meterla al bundle ───────────────────────────────────────────────
# La invocacion es la de Bazel (rules_apple, app_intents.bzl), que corre en produccion fuera
# de Xcode. --binary-file lleva una ruta FALSA a proposito: con --compile-time-extraction el
# procesador no lee el binario, solo los fuentes y los const values, pero el flag es
# obligatorio igual.
say "4 · generar Metadata.appintents con appintentsmetadataprocessor e inyectarla"
rm -rf "$APP/Metadata.appintents"
xcrun appintentsmetadataprocessor \
  --compile-time-extraction \
  --binary-file /ruta/falsa/a/proposito \
  --module-name GnothiIntents \
  --output "$APP" \
  --source-files Swift/GnothiIntents.swift \
  --swift-const-vals Swift/out/GnothiIntents.swiftconstvalues \
  --sdk-root "$SDK" \
  --platform-family iOS \
  --deployment-target 16.0 \
  --target-triple "$TRIPLE" \
  --xcode-version "$XCV" \
  --toolchain-dir "$TC" \
  || info "el procesador devolvio error; se revisa igual si dejo algo"

# Bazel pasa el DIRECTORIO PADRE y el procesador crea Metadata.appintents adentro. Si esta
# version lo interpreto al reves, se reintenta apuntando al directorio final.
if [ ! -d "$APP/Metadata.appintents" ]; then
  info "no aparecio en $APP; reintento con --output apuntando al directorio final"
  xcrun appintentsmetadataprocessor \
    --compile-time-extraction --binary-file /ruta/falsa/a/proposito \
    --module-name GnothiIntents --output "$APP/Metadata.appintents" \
    --source-files Swift/GnothiIntents.swift \
    --swift-const-vals Swift/out/GnothiIntents.swiftconstvalues \
    --sdk-root "$SDK" --platform-family iOS --deployment-target 16.0 \
    --target-triple "$TRIPLE" --xcode-version "$XCV" --toolchain-dir "$TC" || true
fi

if [ -f "$APP/Metadata.appintents/extract.actionsdata" ]; then
  ok "extract.actionsdata generado e inyectado"
  info "contenido (lo que Atajos va a leer):"
  plutil -p "$APP/Metadata.appintents/extract.actionsdata" 2>/dev/null \
    || python3 -m json.tool "$APP/Metadata.appintents/extract.actionsdata" 2>/dev/null \
    || cat "$APP/Metadata.appintents/extract.actionsdata"
else
  bad "el procesador no produjo extract.actionsdata. Pegar la salida de arriba tal cual: el error dice que le falto."
  exit 1
fi

# Se toco el bundle despues de firmarlo, asi que se vuelve a firmar. En simulador la firma es
# ad-hoc (-); en un aparato real iria la identidad del equipo.
codesign --force --sign - --deep "$APP" && ok "bundle re-firmado (ad-hoc, simulador)"

# ── 5 · instalar y lanzar ──────────────────────────────────────────────────────────────────
say "5 · instalar en el simulador abierto"
xcrun simctl uninstall booted com.ncubo.gnothi.lab-intents >/dev/null 2>&1 || true
xcrun simctl install booted "$APP" && ok "instalada"
info "lanzando; las lineas GNOTHI: dicen si la app ve su propia metadata"
xcrun simctl launch --console-pty booted com.ncubo.gnothi.lab-intents 2>&1 | grep GNOTHI | head -5 || true

# ── 6 · lo que se mide a mano ──────────────────────────────────────────────────────────────
say "6 · AHORA SE MIDE A MANO, y son dos cosas"
cat <<'EOF'
  a) Abrir la app Atajos en el simulador → + → buscar «Gnothi Intents».
     ¿Aparece la accion «Probar desde .NET»?

        SI  → la metadata inyectada FUNCIONA. Atajos descubre una intent de
              Swift adentro de una app de .NET. #24863 era la metadata que
              faltaba, y se puede generar aparte.
        NO  → la metadata esta pero no la lee. Reportar el contenido de
              extract.actionsdata de arriba: ahi se ve si el nombre del
              modulo o del tipo no coincide con lo que quedo en el binario.

  b) Si aparece: agregarla a un atajo y CORRERLA.
     ¿Sale «Hola desde Swift, adentro de una app de .NET. La metadata entro.»?

        SI  → el runtime resolvio el tipo Swift dentro del Mach-O de .NET.
              UNA SOLA APP EN iOS ES POSIBLE, con Mono y carga dinamica.
        NO  → Atajos la ve pero no la puede instanciar. Es la incognita que
              quedaba: si el runtime busca el tipo solo en el ejecutable
              principal (que aca ES donde esta, por el enlace estatico) o
              en otro lado. Reportar el error exacto que muestre Atajos.

  Reportar: el veredicto del paso 3, el del 4, y los dos SI/NO de aca.
EOF
