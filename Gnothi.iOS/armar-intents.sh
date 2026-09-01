#!/usr/bin/env bash
# LOS APP INTENTS DE GNOTHI, ADENTRO DE LA APP DE .NET. Compila el Swift, arma la app, genera
# la metadata que el build de .NET no genera, la inyecta, re-firma e instala en el simulador.
# Correr en la Mac, desde esta carpeta, con un simulador ya abierto:
#
#     ./armar-intents.sh
#
# Cada paso imprime un veredicto. El primero que falle es la respuesta.
#
# QUE NO TOCA: la sonda ni la Puerta 2. Si este script falla en el paso 1 o 4, `dotnet build
# -t:Run` a secas sigue dando la app de siempre; la NativeReference esta condicionada a que
# exista la .a.
set -euo pipefail
cd "$(dirname "$0")"

say()  { printf '\n\033[1m%s\033[0m\n' "$*"; }
ok()   { printf '  \033[32m✔ %s\033[0m\n' "$*"; }
bad()  { printf '  \033[31m✘ %s\033[0m\n' "$*"; }
info() { printf '  · %s\n' "$*"; }

MODULO="GnothiIntents"
TIPO="LoQueGnothiTieneIntent"
BUNDLE_ID="com.ncubo.gnothi.host"

# ── 0 · entorno ────────────────────────────────────────────────────────────────────────────
ARCH="${ARCH:-$(uname -m)}"
case "$ARCH" in
  arm64)  RID="iossimulator-arm64" ;;
  x86_64) RID="iossimulator-x64" ;;
  *) bad "arquitectura desconocida: $ARCH"; exit 1 ;;
esac
TRIPLE="${ARCH}-apple-ios16.0-simulator"
SDK="$(xcrun --sdk iphonesimulator --show-sdk-path)"
TC="$(xcode-select -p)/Toolchains/XcodeDefault.xctoolchain"
XCV="$(xcodebuild -version | awk '/Build version/ {print $3}')"

say "0 · entorno"
info "arch=$ARCH  rid=$RID  triple=$TRIPLE  xcode build=$XCV"
xcrun --find appintentsmetadataprocessor >/dev/null 2>&1 \
  && ok "appintentsmetadataprocessor esta en el toolchain" \
  || { bad "no encuentro appintentsmetadataprocessor: hace falta Xcode completo, no solo Command Line Tools"; exit 1; }
[ -d ../Gnothi.Mobile/paquete ] \
  && ok "hay paquete: la app va a tener journals reales" \
  || info "NO hay ../Gnothi.Mobile/paquete: la app arranca sin modulos y la foto sale vacia. Pedirle el zip a Edgar."

# ── 1 · Swift → libreria estatica + const values ───────────────────────────────────────────
say "1 · compilar Intents/GnothiIntents.swift a libreria estatica"
rm -rf Intents/out && mkdir -p Intents/out
xcrun swiftc \
  -parse-as-library -wmo \
  -module-name "$MODULO" \
  -target "$TRIPLE" -sdk "$SDK" \
  -emit-library -static -o Intents/out/libGnothiIntents.a \
  -emit-module -emit-module-path "Intents/out/$MODULO.swiftmodule" \
  -emit-const-values-path "Intents/out/$MODULO.swiftconstvalues" \
  Intents/GnothiIntents.swift
ok "libGnothiIntents.a  $(stat -f%z Intents/out/libGnothiIntents.a) bytes"
[ -s "Intents/out/$MODULO.swiftconstvalues" ] \
  && ok "$MODULO.swiftconstvalues  $(stat -f%z "Intents/out/$MODULO.swiftconstvalues") bytes" \
  || { bad "no salio el .swiftconstvalues: sin el, el procesador no tiene de donde extraer"; exit 1; }

# ── 2 · la app, con la libreria enlazada ───────────────────────────────────────────────────
say "2 · dotnet build"
dotnet build -f net9.0-ios -r "$RID" -c Debug -v:quiet -clp:NoSummary
APP="bin/Debug/net9.0-ios/$RID/Gnothi.iOS.app"
BIN="$APP/Gnothi.iOS"
[ -d "$APP" ] || { bad "no aparecio $APP"; exit 1; }
ok "$APP"
nm "$BIN" 2>/dev/null | grep -q "$TIPO" \
  && ok "$TIPO esta en el ejecutable (ForceLoad funciono)" \
  || { bad "$TIPO NO esta en el ejecutable: el linker lo borro. Revisar ForceLoad en el csproj."; exit 1; }
otool -L "$BIN" | grep -q AppIntents.framework \
  && ok "el ejecutable carga AppIntents.framework" \
  || { bad "el ejecutable NO carga AppIntents.framework. Revisar <Frameworks> en el csproj."; exit 1; }

# ── 3 · ¿dotnet build dejo la metadata? (se espera NO) ─────────────────────────────────────
say "3 · ¿hay Metadata.appintents en el bundle tal como lo dejo dotnet build?"
if [ -d "$APP/Metadata.appintents" ]; then
  ok "SI esta. La hipotesis era FALSA: el build de .NET si la genera. #24863 es otra cosa."
else
  bad "NO esta. Hipotesis CONFIRMADA: el build de .NET no corre appintentsmetadataprocessor. Sigue el 4."
fi

# ── 4 · generarla e inyectarla ─────────────────────────────────────────────────────────────
# La invocacion es la de Bazel (rules_apple, app_intents.bzl). --binary-file lleva una ruta
# FALSA a proposito: con --compile-time-extraction el procesador no lee el binario, pero el
# flag es obligatorio igual.
say "4 · generar Metadata.appintents e inyectarla"
rm -rf "$APP/Metadata.appintents"
procesar() {
  xcrun appintentsmetadataprocessor \
    --compile-time-extraction \
    --binary-file /ruta/falsa/a/proposito \
    --module-name "$MODULO" \
    --output "$1" \
    --source-files Intents/GnothiIntents.swift \
    --swift-const-vals "Intents/out/$MODULO.swiftconstvalues" \
    --sdk-root "$SDK" \
    --platform-family iOS \
    --deployment-target 16.0 \
    --target-triple "$TRIPLE" \
    --xcode-version "$XCV" \
    --toolchain-dir "$TC"
}
procesar "$APP" || info "el procesador devolvio error con --output=$APP; se revisa igual"
if [ ! -d "$APP/Metadata.appintents" ]; then
  info "no aparecio; reintento con --output apuntando al directorio final"
  procesar "$APP/Metadata.appintents" || true
fi
if [ -f "$APP/Metadata.appintents/extract.actionsdata" ]; then
  ok "extract.actionsdata generado e inyectado"
  info "lo que Atajos va a leer:"
  plutil -p "$APP/Metadata.appintents/extract.actionsdata" 2>/dev/null \
    || python3 -m json.tool "$APP/Metadata.appintents/extract.actionsdata" 2>/dev/null \
    || cat "$APP/Metadata.appintents/extract.actionsdata"
else
  bad "el procesador no produjo extract.actionsdata. Pegar la salida de arriba tal cual."
  exit 1
fi
codesign --force --sign - --deep "$APP" && ok "bundle re-firmado (ad-hoc, simulador)"

# ── 5 · instalar y lanzar ──────────────────────────────────────────────────────────────────
say "5 · instalar en el simulador abierto y abrir una vez (para que escriba la foto)"
xcrun simctl uninstall booted "$BUNDLE_ID" >/dev/null 2>&1 || true
xcrun simctl install booted "$APP" && ok "instalada"
info "lanzando; buscar en el log: 'instantanea: N modulos' y 'Metadata.appintents'"
xcrun simctl launch --console-pty booted "$BUNDLE_ID" 2>&1 | grep GNOTHI | head -12 || true

# ── 6 · a mano ─────────────────────────────────────────────────────────────────────────────
say "6 · AHORA SE MIDE A MANO"
cat <<'EOF'
  a) Atajos → + → buscar «Gnothi Host». ¿Aparece «Lo que Gnothi tiene»?

        SI → la metadata inyectada FUNCIONA dentro de Gnothi.iOS.
        NO → esta pero no la lee. Reportar extract.actionsdata de arriba.
             Y correr Spikes/Lab.IntentsDesdeNet para separar "el mecanismo
             no anda" de "algo de Gnothi interfiere".

  b) Agregarla a un atajo y CORRERLA (o decirle a Siri «Qué tiene Gnothi Host»).
     ¿Dice el encuadre Y el json con las facturas/examenes REALES y la fecha?

        SI → UNA SOLA APP. El Host de .NET bajo Mono, con carga dinamica, y
             los App Intents en Swift adentro leyendo lo que el Host escribe.
             Lab.Siri y el App Group dejan de hacer falta.
        «Gnothi todavia no dejo ninguna foto» → la intent corre pero no ve
             la carpeta del C#. Reportar la ruta que dice y la linea
             'instantanea: ... -> ...' del log: una es donde busco Swift, la
             otra donde escribio C#.
        NO → Atajos la ve pero no la instancia. Reportar el error exacto, y
             correr Spikes/Lab.IntentsDesdeNet.

  Reportar: veredicto del 3, del 4, y para 6b el TEXTO COMPLETO.
EOF
