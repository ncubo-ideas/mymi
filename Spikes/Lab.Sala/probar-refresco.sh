#!/usr/bin/env bash
# PROBAR UN REFRESCO REAL POR LA SALA, de punta a punta.
#
# Lo que afirma: que lo que el modulo asienta adentro de la sala ATERRIZA en el journal de la
# persona y SIGUE AHI despues de desalojarlo. Sin esto el refresco se veia aplicado y volvia al
# estado anterior al cambiar de modulo, que es la peor clase de falla.
#
# La prueba tiene forma de antes-y-despues:
#
#   1 · se pregunta el total ANTES
#   2 · se abre la pantalla del modulo, que dispara el pull contra el publicador
#   3 · se cambia a OTRO modulo, lo que fuerza el desalojo y el guardado
#   4 · se vuelve y se pregunta el total OTRA VEZ
#
# Si el total cambio en el paso 1 contra el 4, el viaje de vuelta funciona.
#
# Uso:  ./probar-refresco.sh <serie-del-telefono>
set -u

ADB=${ADB:-/c/af/sdk/platform-tools/adb.exe}
D=${1:-}
REPO=$(cd "$(dirname "$0")/../.." && pwd)
TMP=$(mktemp -d)

if [ -z "$D" ]; then echo "falta la serie: ./probar-refresco.sh <serie>"; exit 2; fi
if ! "$ADB" devices | grep -q "^$D"; then echo "el telefono $D no esta conectado"; exit 2; fi

pedir() { # modulo tool
	curl -s -D "$TMP/h" -o "$TMP/b" "http://127.0.0.1:47623/gnothi/consulta?token=$TOKEN&modulo=$1&tool=$2"
	node -e "const j=JSON.parse(require('fs').readFileSync('$TMP/b','utf8'));console.log(j.authorized?JSON.stringify(j.contenido):('FALLO '+j.motivo))"
}

echo "1 · el publicador de compras, en esta maquina"
dotnet run --project "$REPO/Compras/EndPointDeEventos/Compras.EventosMock.csproj" \
	-c Debug -p:BaseOutputPath="$REPO/bin/mock-compras/" --no-build >"$TMP/mock.log" 2>&1 &
MOCK=$!
trap 'kill $MOCK 2>/dev/null' EXIT
sleep 6
"$ADB" -s "$D" reverse tcp:8101 tcp:8101 >/dev/null && echo "   alcanzable desde el telefono en 127.0.0.1:8101"

echo "2 · Gnothi, con la sala forzada para comprasw"
"$ADB" -s "$D" shell am force-stop com.ncubo.gnothi.host
"$ADB" -s "$D" shell input keyevent KEYCODE_WAKEUP >/dev/null
"$ADB" -s "$D" logcat -c
ACT=$("$ADB" -s "$D" shell cmd package resolve-activity --brief com.ncubo.gnothi.host | tail -1 | tr -d '\r')
"$ADB" -s "$D" shell am start -n "$ACT" --es sala "comprasw,examenesw" >/dev/null 2>&1
sleep 18
"$ADB" -s "$D" shell am start-foreground-service -n com.ncubo.gnothi.host/com.ncubo.gnothi.host.LoopbackSpikeService >/dev/null 2>&1
sleep 5
"$ADB" -s "$D" forward tcp:47623 tcp:47623 >/dev/null
TOKEN=$("$ADB" -s "$D" logcat -d | grep -o "token=[a-f0-9]*" | tail -1 | cut -d= -f2)

echo
echo "3 · el total ANTES del refresco"
ANTES=$(pedir comprasw resumen); echo "   $ANTES"

echo
echo "4 · abrir la pantalla, que dispara el pull contra el publicador"
"$ADB" -s "$D" shell am start -n "$ACT" --es sala "comprasw,examenesw" --es "gnothi.modulo" "comprasw" >/dev/null 2>&1
sleep 25

echo
echo "5 · cambiar de modulo, que fuerza el desalojo y el guardado"
pedir examenesw resumen >/dev/null

echo
echo "6 · el total DESPUES, con el modulo recargado desde el disco"
DESPUES=$(pedir comprasw resumen); echo "   $DESPUES"

echo
echo "=== lo que el puente guardo ==="
"$ADB" -s "$D" logcat -d | grep -oE "sala/(guardar|desalojar) .*" | tail -8

echo
if [ "$ANTES" = "$DESPUES" ]; then
	echo "SIN CAMBIO: o no habia eventos nuevos, o el viaje de vuelta no funciono. Mirar el log de arriba."
else
	echo "EL REFRESCO QUEDO: el total cambio y sobrevivio al desalojo."
fi

"$ADB" -s "$D" shell am stopservice -n com.ncubo.gnothi.host/com.ncubo.gnothi.host.LoopbackSpikeService >/dev/null 2>&1
