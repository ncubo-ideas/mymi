#!/usr/bin/env bash
# APPLE INTELLIGENCE PARA LA VENTANA DE ESCRITORIO. Compila el MISMO Puente.swift que la app de
# iOS enlaza estatico, pero como .dylib universal (arm64 + x86_64) para macOS 26, y la deja en
# native/ para que el csproj la copie al lado del ejecutable.
#
#     ./armar-modelo.sh          # desde Gnothi.Desktop/, en una Mac con Xcode 26
#
# POR QUE EL MISMO ARCHIVO. El puente no sabe de iOS: sus @available(iOS 26.0, *) en macOS
# significan "disponible", y FoundationModels es el mismo framework. Copiarlo seria repetir el
# antipatron del manifiesto: dos copias que se desincronizan en silencio.
#
# POR QUE UNIVERSAL. El dotnet de al menos una de nuestras Macs es x64 bajo Rosetta, y medido el
# 2026-09-08: FoundationModels genera igual desde un proceso traducido (0,7 s la respuesta).
# Una dylib solo-arm64 fallaria ahi con "mach-o, but wrong architecture" y nada mas.
#
# Sin Xcode (Windows, Linux, CI) este script no corre, la dylib no existe, y la ventana compila
# igual: el preset de Apple Intelligence simplemente no aparece en Ajustes.
set -euo pipefail
cd "$(dirname "$0")"

ok()  { printf '  \033[32m✔ %s\033[0m\n' "$*"; }
bad() { printf '  \033[31m✘ %s\033[0m\n' "$*"; exit 1; }

FUENTE="../Gnothi.iOS/Intents/Puente.swift"
SALIDA="native/libGnothiModelo.dylib"
TMP="native/out"
[ -f "$FUENTE" ] || bad "no encuentro $FUENTE"
command -v xcrun >/dev/null || bad "hace falta Xcode: FoundationModels es de macOS 26"

rm -rf "$TMP" && mkdir -p "$TMP"
for ARCH in arm64 x86_64; do
  xcrun swiftc -O -parse-as-library -wmo \
    -module-name GnothiModelo \
    -target "${ARCH}-apple-macos26.0" \
    -emit-library -o "$TMP/$ARCH.dylib" \
    -Xlinker -install_name -Xlinker "@rpath/libGnothiModelo.dylib" \
    "$FUENTE"
  ok "$ARCH: $(stat -f%z "$TMP/$ARCH.dylib") bytes"
done
lipo -create "$TMP/arm64.dylib" "$TMP/x86_64.dylib" -output "$SALIDA"
ok "$SALIDA universal: $(lipo -archs "$SALIDA")"

for S in gnothi_fm_estado gnothi_fm_paso gnothi_fm_liberar; do
  nm -gU "$SALIDA" | grep -q "_$S\$" && ok "exporta $S" || bad "falta el simbolo $S"
done
rm -rf "$TMP"
