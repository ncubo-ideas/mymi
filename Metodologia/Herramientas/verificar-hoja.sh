#!/bin/bash
#
# La hoja de la entrevista es un resumen a mano del punto de partida, y NADA la
# mantiene sincronizada. Derivo cuatro veces en una semana, en las dos
# direcciones. Esto compara los marcadores de regla de los dos archivos:
#
#     <!-- regla: filtros-no-a-la-capacidad sesion -->
#
# Son comentarios HTML: invisibles al leer el markdown renderizado.
#
#   1. Todo marcador de la hoja existe en el capitulo
#      -> la hoja no puede tener doctrina propia; es un subconjunto
#   2. Todo marcador del capitulo etiquetado 'sesion' existe en la hoja
#      -> lo que hace falta con el entrevistado enfrente llega a la hoja
#
# La etiqueta 'sesion' es lo que permite que el capitulo tenga cosas propias
# -argumentacion, el porque de cada regla- sin falsos positivos.
#
#   bash verificar-hoja.sh [hoja.md] [capitulo.md]
#
# Sale 1 si hay divergencias.
#
# POR QUE NO COMPARA EL TEXTO. Los dos dicen lo mismo con palabras distintas a
# proposito -"Pero no se pregunta en frio: se propone y se deja corregir" contra
# "No preguntes en frio: proponé"-, y los titulos de los recuadros tambien
# difieren. Un diff daria cientos de diferencias legitimas y ninguna real.
# Comparar significados necesita que un humano juzgue, o sea que NO es
# observable. La salida es volverlo una declaracion, que es el mismo argumento
# del manifiesto: porque es una declaracion, se puede verificar.

set -u
aqui=$(dirname "$0")
hoja="${1:-$aqui/../Guias/hoja-de-la-entrevista.md}"
cap="${2:-$aqui/../Guias/punto-de-partida.md}"

# EL PISO. Sin esto, un par de archivos SIN marcadores pasa perfecto: cero
# marcadores, cero divergencias, codigo 0. Es la misma trampa que el verificador
# de enlaces cuando dio cero por estar roto -un chequeo que no encuentra nada
# porque no tiene que mirar-. Con el piso, borrar los marcadores falla en vez de
# aprobar.
PISO=20

marcadores() {
	grep -o '<!-- regla: [a-z0-9-]* *\(sesion\)\? *-->' "$1" \
	  | sed 's/<!-- regla: //; s/ *-->//'
}

for f in "$hoja" "$cap"; do
	[ -f "$f" ] || { echo "no existe: $f" >&2; exit 2; }
done

tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT

marcadores "$hoja" | awk '{print $1}'                 | sort -u > "$tmp/hoja"
marcadores "$cap"  | awk '{print $1}'                 | sort -u > "$tmp/cap"
marcadores "$cap"  | awk '$2=="sesion" {print $1}'    | sort -u > "$tmp/cap-sesion"

en_hoja=$(wc -l < "$tmp/hoja")
de_sesion=$(wc -l < "$tmp/cap-sesion")
rotos=0

if [ "$en_hoja" -lt "$PISO" ] || [ "$de_sesion" -lt "$PISO" ]; then
	echo "PISO           la hoja tiene $en_hoja marcadores y el capitulo $de_sesion de sesion;"
	echo "               el minimo es $PISO. O se borraron, o este chequeo no esta mirando nada."
	rotos=$((rotos + 1))
fi

while read -r regla; do
	[ -z "$regla" ] && continue
	echo "SOBRA EN LA HOJA   $regla   (no esta en el capitulo)"
	rotos=$((rotos + 1))
done < <(comm -23 "$tmp/hoja" "$tmp/cap")

while read -r regla; do
	[ -z "$regla" ] && continue
	echo "FALTA EN LA HOJA   $regla   (el capitulo la marca 'sesion')"
	rotos=$((rotos + 1))
done < <(comm -23 "$tmp/cap-sesion" "$tmp/hoja")

echo "-----"
echo "reglas en la hoja: $en_hoja   de sesion en el capitulo: $de_sesion   divergencias: $rotos"
[ "$rotos" -eq 0 ]
