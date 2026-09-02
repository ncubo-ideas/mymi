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
aviso(){ printf '  \033[33m! %s\033[0m\n' "$*"; }
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
# -emit-const-values-path dice DONDE escribir, pero no alcanza para que Swift lo emita: hay que
# pedirle ademas QUE protocolos recoger, y eso es -const-gather-protocols-file, que es flag del
# FRONTEND y no del driver -por eso va con -Xfrontend-. Sin el, el .a sale igual y el
# .swiftconstvalues no, asi que el paso 4 se queda sin nada de donde extraer.
#
# La lista vive en Intents/protocolos-appintents.json, versionada, porque Xcode no trae ninguna:
# rules_apple la genera al vuelo y aca la escribimos nosotros.
say "1 · compilar Intents/GnothiIntents.swift a libreria estatica"
rm -rf Intents/out && mkdir -p Intents/out
xcrun swiftc \
  -parse-as-library -wmo \
  -module-name "$MODULO" \
  -target "$TRIPLE" -sdk "$SDK" \
  -emit-library -static -o Intents/out/libGnothiIntents.a \
  -emit-module -emit-module-path "Intents/out/$MODULO.swiftmodule" \
  -Xfrontend -const-gather-protocols-file -Xfrontend Intents/protocolos-appintents.json \
  -emit-const-values-path "Intents/out/$MODULO.swiftconstvalues" \
  Intents/GnothiIntents.swift
ok "libGnothiIntents.a  $(stat -f%z Intents/out/libGnothiIntents.a) bytes"
[ -s "Intents/out/$MODULO.swiftconstvalues" ] \
  && ok "$MODULO.swiftconstvalues  $(stat -f%z "Intents/out/$MODULO.swiftconstvalues") bytes" \
  || { bad "no salio el .swiftconstvalues: sin el, el procesador no tiene de donde extraer"; exit 1; }

# ── 2 · la app, con la libreria enlazada ───────────────────────────────────────────────────
say "2 · dotnet build"
# EL CHEQUEO DE XCODE, y por que se saltea SOLO cuando hace falta. El workload de iOS exige la
# version EXACTA de Xcode que trae su banda: 26.5.9004 pide Xcode 26.5, y con 26.6 instalado el
# build muere antes de compilar. No hay workload publicado para 26.6 todavia, asi que las
# opciones reales son bajar Xcode 26.5 o saltear el chequeo.
#
# Se saltea solo si de verdad no coinciden, y se avisa: bypasear siempre esconderia el dia que
# la diferencia sea grande y si importe.
# LIMPIAR LA METADATA ANTES DE COMPILAR, o el paso 3 miente. El paso 4 la inyecta dentro de
# bin/ y el build siguiente es incremental, asi que sobrevive; el paso 3 entonces lee la de la
# corrida ANTERIOR y concluye que dotnet build la genera -declarando falsa la hipotesis y
# dejando sin sentido al paso 4-. Medido: borrada y con dotnet build a secas, no reaparece.
rm -rf "bin/Debug/net9.0-ios/$RID/Gnothi.iOS.app/Metadata.appintents"

XC_PIDE="$(sed -n '/_RecommendedXcodeVersion>/{
                     s/.*_RecommendedXcodeVersion>\([0-9]*\.[0-9]*\).*/\1/p; q; }' \
                   /usr/local/share/dotnet/*/packs/Microsoft.iOS.Sdk.net9.0_*/*/targets/*.props \
                   "$HOME"/.dotnet/packs/Microsoft.iOS.Sdk.net9.0_*/*/targets/*.props 2>/dev/null || true)"
XC_HAY="$(xcodebuild -version | awk 'NR==1 { split($2, v, "."); print v[1] "." v[2] }')"
SALTAR=""
if [ -n "$XC_PIDE" ] && [ "$XC_PIDE" != "$XC_HAY" ]; then
  SALTAR="-p:ValidateXcodeVersion=false"
  aviso "el workload pide Xcode $XC_PIDE y hay $XC_HAY: se saltea el chequeo de version"
fi
dotnet build -f net9.0-ios -r "$RID" -c Debug -v:quiet -clp:NoSummary $SALTAR
APP="bin/Debug/net9.0-ios/$RID/Gnothi.iOS.app"
BIN="$APP/Gnothi.iOS"
[ -d "$APP" ] || { bad "no aparecio $APP"; exit 1; }
ok "$APP"
# HAY QUE DESMANGLAR ANTES DE BUSCAR. El mangler de Swift comprime subcadenas repetidas con
# back-references, y como "Gnothi" ya aparece en el nombre del modulo, en el del tipo se
# reemplaza por una letra:
#
#     _$s13GnothiIntents05LoQueA11TieneIntentV...
#                             ^ "Gnothi", sustituido
#
# O sea que la cadena "LoQueGnothiTieneIntent" NO existe en el binario aunque el tipo si este.
# Buscarla cruda daba un falso negativo y acusaba al linker de haberlo borrado. Desmanglado son
# 52 coincidencias. Le pasa a cualquier tipo cuyo nombre repita un trozo del nombre del modulo.
# grep -c y no grep -q: -q sale al primer match, le manda SIGPIPE a swift-demangle, y con
# pipefail el pipeline entero cuenta como fallido aunque el simbolo ESTUVIERA. -c lee todo.
CUENTA_TIPO="$(nm "$BIN" 2>/dev/null | xcrun swift-demangle 2>/dev/null | grep -c "$TIPO" || true)"
[ "${CUENTA_TIPO:-0}" -gt 0 ] \
  && ok "$TIPO esta en el ejecutable, $CUENTA_TIPO simbolos (ForceLoad funciono)" \
  || { bad "$TIPO NO esta en el ejecutable: el linker lo borro. Revisar ForceLoad en el csproj."; exit 1; }
[ "$(otool -L "$BIN" | grep -c AppIntents.framework || true)" -gt 0 ] \
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

# EXIGIR UN APARATO BOOTEADO, y abortar si no hay. `simctl install booted` falla con "No devices
# are booted", y como iba dentro de un `&&` el fallo no abortaba nada: el script seguia derecho
# a imprimir las instrucciones del paso 6 como si la app estuviera instalada. Lo que se medIa
# despues era la instalacion ANTERIOR, con el paquete anterior. Ya paso.
#
# El udid se resuelve una vez y se usa explicito en lugar de `booted`, asi las tres llamadas van
# al MISMO aparato aunque alguien bootee otro en el medio.
UDID="$(xcrun simctl list devices booted | sed -n 's/.*(\([0-9A-Fa-f-]\{36\}\)).*/\1/p' | sed -n '1p')"
[ -n "$UDID" ] || { bad "no hay ningun simulador booteado: abrir uno y repetir"; exit 1; }
info "aparato: $UDID"

xcrun simctl uninstall "$UDID" "$BUNDLE_ID" >/dev/null 2>&1 || true
xcrun simctl install "$UDID" "$APP" || { bad "simctl install fallo"; exit 1; }
ok "instalada"

# EL LANZADO, ACOTADO. --console-pty no termina solo porque la app no sale, y antes esto
# terminaba solo de rebote: `| head -12` cerraba el pipe al juntar 12 lineas GNOTHI. Si la app
# imprimia menos -o fallaba antes de loguear- colgaba para siempre. Ahora corre al fondo y se
# espera la linea que importa, con techo.
LOG="$(mktemp)"
info "lanzando; se espera la linea 'instantanea: N modulos'"
xcrun simctl launch --console-pty "$UDID" "$BUNDLE_ID" > "$LOG" 2>&1 &
PID_APP=$!
for _ in $(seq 90); do
  grep -q "instantanea:" "$LOG" 2>/dev/null && break
  sleep 1
done
kill "$PID_APP" 2>/dev/null || true
grep GNOTHI "$LOG" | sed -n '1,16p' || true
if grep -q "instantanea:" "$LOG"; then
  ok "la app arranco y escribio la foto"
else
  bad "no aparecio 'instantanea:' en 90 s: la app no llego a escribirla. El 6 no se puede medir."
  rm -f "$LOG"; exit 1
fi
rm -f "$LOG"

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
