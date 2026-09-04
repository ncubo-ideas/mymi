#!/usr/bin/env bash
# Arma la sonda SIN Gradle, igual que Lab.Caratula: aapt2 -> javac -> d8 -> zipalign -> apksigner.
# Las herramientas son binarios de Windows y no entienden /c/Users/...: se les pasa C:/Users/...
set -e
export MSYS_NO_PATHCONV=1

SDK="/c/Program Files (x86)/Android/android-sdk"
BT="$SDK/build-tools/36.0.0"
WSDK="C:/Program Files (x86)/Android/android-sdk"
JAR="$WSDK/platforms/android-36/android.jar"

HERE="$(cd "$(dirname "$0")" && pwd)"
W="$(cygpath -m "$HERE")"
OUT="$HERE/out"; WO="$W/out"

rm -rf "$OUT"; mkdir -p "$OUT/classes"

echo "1 · aapt2 link"
"$BT/aapt2.exe" link --manifest "$W/AndroidManifest.xml" -I "$JAR" \
  --min-sdk-version 30 --target-sdk-version 36 -o "$WO/base.apk"

echo "2 · javac"
javac -source 17 -target 17 -nowarn -classpath "$JAR" -d "$WO/classes" \
  "$W/java/com/ncubo/gnothi/sonda/"*.java

echo "3 · d8"
java -cp "$WSDK/build-tools/36.0.0/lib/d8.jar" com.android.tools.r8.D8 \
  --lib "$JAR" --min-api 30 --output "$WO" \
  $(find "$OUT/classes" -name '*.class' | while read -r f; do cygpath -m "$f"; done)

echo "4 · meter el dex en el apk"
cd "$OUT" && "$BT/aapt2.exe" version >/dev/null && cp base.apk sonda-unaligned.apk
cd "$OUT" && jar uf sonda-unaligned.apk classes.dex

echo "5 · zipalign + firmar (clave de debug, la MISMA que la caratula)"
"$BT/zipalign.exe" -f 4 "$WO/sonda-unaligned.apk" "$WO/sonda.apk"
java -jar "$WSDK/build-tools/36.0.0/lib/apksigner.jar" sign \
  --ks "$(cygpath -m "$HOME")/.android/debug.keystore" \
  --ks-pass pass:android --ks-key-alias androiddebugkey --key-pass pass:android \
  "$WO/sonda.apk"

echo "listo: $OUT/sonda.apk"
