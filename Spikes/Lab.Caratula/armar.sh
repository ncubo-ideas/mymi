#!/usr/bin/env bash
# Arma el APK-caratula SIN Gradle ni Kotlin: aapt2 -> javac -> d8 -> zipalign -> apksigner.
# Todo viene con el Android SDK y el JDK del workload de .NET Android.
#
# Se hace a mano por dos razones: no hay Gradle en esta maquina, y el shim es tan chico que
# la cadena entera cabe en una pantalla -- lo que ademas deja ver exactamente que tendria
# que producir el "target de compilacion" que Alvaro describe en su R13.
#
# RUTAS: las herramientas son binarios de Windows y NO entienden /c/Users/... Se les pasa
# la forma C:/Users/... (con barra normal, que si aceptan). Costo dos intentos.
set -e
export MSYS_NO_PATHCONV=1

SDK="/c/Program Files (x86)/Android/android-sdk"
BT="$SDK/build-tools/36.0.0"
WSDK="C:/Program Files (x86)/Android/android-sdk"
JAR="$WSDK/platforms/android-36/android.jar"

HERE="$(cd "$(dirname "$0")" && pwd)"
W="$(cygpath -m "$HERE")"     # -m = C:/Users/... con barra normal
OUT="$HERE/out"
WO="$W/out"

rm -rf "$OUT"; mkdir -p "$OUT/classes"

echo "1a · aapt2 compile: res/ -> res.zip   (app_metadata y su string)"
# El v1 no necesitaba recursos: el XML iba en assets y listo. El v2 pide un
# @xml/app_metadata, que es un RECURSO, y por eso ahora hay un paso de compile.
"$BT/aapt2.exe" compile --dir "$W/res" -o "$WO/res.zip"

echo "1b · aapt2 link: manifiesto + assets + recursos -> apk base"
"$BT/aapt2.exe" link \
  --manifest "$W/AndroidManifest.xml" \
  -I "$JAR" \
  -A "$W/assets" \
  -R "$WO/res.zip" \
  --auto-add-overlay \
  --min-sdk-version 36 --target-sdk-version 36 \
  -o "$WO/base.apk"

echo "2 · javac: el service contra android.jar"
javac -source 17 -target 17 -nowarn \
  -classpath "$JAR" \
  -d "$WO/classes" \
  "$W/java/com/ncubo/lab/caratula/"*.java

echo "3 · d8: bytecode -> dex"
# d8.bat y apksigner.bat son batch de Windows y revientan con rutas que llevan espacios
# ("'C:\Program' is not recognized"). Se invocan sus JARs directamente, que es lo que el
# .bat hace por dentro y no tiene ese problema.
java -cp "$WSDK/build-tools/36.0.0/lib/d8.jar" com.android.tools.r8.D8 \
  --min-api 36 --output "$WO" --lib "$JAR" \
  "$WO/classes/com/ncubo/lab/caratula/"*.class

echo "4 · el dex adentro del apk"
powershell.exe -NoProfile -Command "
  Add-Type -A System.IO.Compression.FileSystem
  \$z = [IO.Compression.ZipFile]::Open('$WO/base.apk', 'Update')
  \$e = \$z.CreateEntry('classes.dex')
  \$s = \$e.Open()
  \$b = [IO.File]::ReadAllBytes('$WO/classes.dex')
  \$s.Write(\$b, 0, \$b.Length); \$s.Close(); \$z.Dispose()" >/dev/null

echo "5 · zipalign + firma con el keystore de debug"
KS="$(cygpath -m "$HOME")/.android/debug.keystore"
if [ ! -f "$HOME/.android/debug.keystore" ]; then
  keytool -genkeypair -keystore "$KS" -storepass android -keypass android \
    -alias androiddebugkey -dname "CN=Android Debug,O=Android,C=US" -keyalg RSA -validity 10000
fi
"$BT/zipalign.exe" -f 4 "$WO/base.apk" "$WO/aligned.apk"

java -jar "$WSDK/build-tools/36.0.0/lib/apksigner.jar" sign \
  --ks "$KS" --ks-pass pass:android --key-pass pass:android \
  --out "$WO/caratula.apk" "$WO/aligned.apk"

echo
echo "listo: $OUT/caratula.apk ($(stat -c%s "$OUT/caratula.apk") bytes)"
