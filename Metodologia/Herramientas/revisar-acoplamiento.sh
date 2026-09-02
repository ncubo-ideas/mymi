#!/bin/bash
#
# La otra mitad del observable 14, y cubre el agujero que los marcadores dejan:
# el marcador lo pone la misma persona que se olvida de sincronizar. Si alguien
# agrega una regla a la hoja SIN marcador, verificar-hoja.sh pasa igual.
#
# Esto no depende de que nadie declare nada. Mira los ultimos commits y avisa
# cuando uno toco la hoja o el capitulo pero no el otro.
#
#   bash revisar-acoplamiento.sh [cuantos commits]      # por defecto 20
#
# NO ES UN OBSERVABLE, ES UNA HEURISTICA. Tiene falsos positivos legitimos: un
# typo en el capitulo no necesita tocar la hoja. Por eso dice "revisar" y no
# "falla", y por eso SIEMPRE sale con codigo 0. Sirve para mirar, no para
# bloquear.

set -u
cuantos="${1:-20}"

HOJA=Metodologia/Guias/hoja-de-la-entrevista.md
CAP=Metodologia/Guias/punto-de-partida.md

sospechosos=0
while read -r sha resto; do
	[ -z "$sha" ] && continue
	tocados=$(git show --name-only --format= "$sha")
	h=0; c=0
	echo "$tocados" | grep -qxF "$HOJA" && h=1
	echo "$tocados" | grep -qxF "$CAP"  && c=1

	[ $((h + c)) -eq 1 ] || continue
	if [ "$h" -eq 1 ]; then cual="solo la hoja      "; else cual="solo el capitulo  "; fi
	echo "REVISAR  $cual ${sha:0:7}  $resto"
	sospechosos=$((sospechosos + 1))
done < <(git log --format='%h %s' -n "$cuantos" -- "$HOJA" "$CAP")

echo "-----"
echo "commits mirados: hasta $cuantos que tocaron alguno de los dos   para revisar: $sospechosos"
echo "(heuristica: un typo en uno no obliga a tocar el otro. Nunca falla el build.)"
