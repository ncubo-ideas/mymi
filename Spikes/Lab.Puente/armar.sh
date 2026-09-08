#!/usr/bin/env bash
# EL CRUCE ENTRE MODULOS CONTRA EL MODELO ON-DEVICE. Compila Puente.swift como app de simulador,
# la instala en el simulador de iOS 26.5 booteado y la corre una vez.
#
#     ./armar.sh ["la pregunta"]
#
# OJO CON EL RUNTIME: en iOS 26.2 la generacion NO corre -ModelManagerError 1026- aunque
# `availability` diga available. Hace falta 26.5. Ver Spikes/Lab.Siri/HALLAZGOS.md seccion 8.
set -euo pipefail
cd "$(dirname "$0")"

say() { printf '\n\033[1m%s\033[0m\n' "$*"; }
ok()  { printf '  \033[32m✔ %s\033[0m\n' "$*"; }
bad() { printf '  \033[31m✘ %s\033[0m\n' "$*"; }

PREGUNTA="${1:-}"
BUNDLE="com.ncubo.puente"
APP="out/Puente.app"

say "0 · simulador"
# El udid explicito y no `booted`: con dos simuladores arriba `booted` es ambiguo, y si el que
# agarra es el de 26.2 la generacion falla y parece que el experimento no funciona.
UDID="$(xcrun simctl list devices 2>/dev/null \
        | awk '/-- iOS 26.5 --/{f=1;next} /^-- /{f=0} f && /Booted/' \
        | sed -n 's/.*(\([0-9A-Fa-f-]\{36\}\)).*/\1/p' | sed -n '1p')"
[ -n "$UDID" ] || { bad "no hay simulador de iOS 26.5 booteado: abrir uno y repetir"; exit 1; }
ok "iOS 26.5 · $UDID"

say "1 · compilar"
rm -rf out && mkdir -p "$APP"
cat > "$APP/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>CFBundleIdentifier</key><string>com.ncubo.puente</string>
  <key>CFBundleExecutable</key><string>Puente</string>
  <key>CFBundleName</key><string>Puente</string>
  <key>CFBundleVersion</key><string>1</string>
  <key>MinimumOSVersion</key><string>26.0</string>
  <key>UIDeviceFamily</key><array><integer>1</integer></array>
</dict></plist>
PLIST
xcrun swiftc -parse-as-library -target arm64-apple-ios26.0-simulator \
  -sdk "$(xcrun --sdk iphonesimulator --show-sdk-path)" -o "$APP/Puente" Puente.swift
codesign -f -s - "$APP" >/dev/null
ok "$APP"

say "2 · instalar y correr"
xcrun simctl install "$UDID" "$APP"
LOG="$(mktemp)"
# --console-pty no termina solo: se lanza al fondo, se espera el texto y se mata. La primera
# generacion del dia puede pasar los 90 s, asi que el techo es holgado.
nohup xcrun simctl launch --console-pty \
  --terminate-running-process "$UDID" "$BUNDLE" > "$LOG" 2>&1 &
for _ in $(seq 150); do
  grep -qE "PUENTE-RESPUESTA|PUENTE: 2 fallo" "$LOG" 2>/dev/null && break
  sleep 2
done
pkill -f "simctl launch --console-pty" 2>/dev/null || true

say "3 · que llamo y que contesto"
grep -E "PUENTE" "$LOG" || bad "no devolvio nada en 300 s"
rm -f "$LOG"
