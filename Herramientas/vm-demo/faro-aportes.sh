#!/bin/bash
# LOS APORTES DE LA PERSONA SOBRE EL REGISTRO DE FARO, en cuanto el registro existe.
#
# QUE ANOTA Y POR QUE NO ES TRAMPA. Los kilometrajes y la confirmacion del tanque son APORTES
# DE LA PERSONA, no datos de la estacion: la estacion sabe cuantos litros vendio, y el odometro
# no lo sabe nadie mas que ella. Sin dos tanques llenos con kilometraje no hay tramo cerrado y
# no hay km/L, asi que la cara de Faro se queda diciendo que todavia no puede calcular. Esto le
# ahorra teclear 17 formularios; no inventa nada que una institucion debiera haber mandado.
#
# POR QUE NO SE PUEDE HACER EN LA CONSTRUCCION: la carpeta del registro la concede el ALTA, y
# el alta la hace la persona desde la ventana. Y tampoco alcanza con que la carpeta exista: los
# aportes refieren TRANSACCIONES CONCRETAS -- T-0201, T-0202... --, asi que hasta que el pull
# no metio las cargas, `AnotarOdometro` no tiene a que engancharse y no anota ninguna.
#
# POR QUE UN TIMER Y NO UN .path. Se intento primero con `PathChanged` y NO DISPARA NUNCA:
# systemd necesita que el directorio PADRE exista para poner el inotify, y
# `~/Documentos/Gnothi/` no existe hasta que la persona da el alta. La unidad arranca, no
# encuentra a quien vigilar, y se queda callada -- sin error, que es lo peor de todo. Un timer
# que pregunta cada minuto no tiene ese problema y se apaga solo con la marca.
set -u
CONSOLA=/opt/mocks/cargas/consola
MARCA=/opt/mocks/cargas/.aportes-hechos
LOG=/opt/mocks/cargas/aportes.log
BASE=/home/demo/Documentos/Gnothi/cargas
export DOTNET_ROLL_FORWARD=Major

exec >>"$LOG" 2>&1
[ -e "$MARCA" ] && exit 0
[ -d "$BASE" ] || exit 0
cd "$CONSOLA" || { echo "$(date -Is) falta la consola"; exit 1; }

# LA CARPETA DEL JOURNAL ESTA UN NIVEL MAS ADENTRO de la que concede el Host: el Host concede
# `Gnothi/cargas` y el modulo arma su journal en `cargas/` adentro. Se prueban las dos en vez
# de fijar una, que es lo que hace que esto no se rompa si esa forma cambia.
cargas=""
for c in "$BASE/cargas" "$BASE"; do
    [ -d "$c" ] || continue
    n=$(./Cargas.Consola estado "$c" 2>/dev/null \
        | sed -n 's/^cantidad de cargas *: *//p' | tr -d ' ')
    if [ -n "${n:-}" ] && [ "$n" -gt 0 ] 2>/dev/null; then cargas="$c"; break; fi
done
[ -n "$cargas" ] || exit 0        # todavia sin cargas: se reintenta en el proximo tick

echo "── $(date -Is) el registro tiene $n cargas en $cargas; anotando los aportes"
./Cargas.Consola sembrar-aportes "$cargas"
echo "   salida $?"
: > "$MARCA"
