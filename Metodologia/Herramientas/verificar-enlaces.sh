#!/bin/bash
#
# Verifica que cada enlace de los .md de un arbol apunte a algo que existe:
# el archivo, y -si el enlace trae "#"- tambien el ancla.
#
#   bash verificar-enlaces.sh ../          # revisa toda la Metodologia
#   bash verificar-enlaces.sh ../Guias     # solo las guias
#
# Sale con codigo 1 si encontro algo roto, 0 si esta limpio.
#
# POR QUE EXISTE. Un ancla rota no da error en ningun lado: el markdown se ve
# bien, el enlace se ve bien, y al hacer click el navegador simplemente no
# salta. Las reglas del substrato se enlazan por ancla desde tres archivos
# distintos, asi que renombrar el titulo de una regla rompe enlaces en
# silencio.

set -u

destino="${1:-.}"
cd "$destino" || { echo "no existe el directorio: $destino" >&2; exit 2; }

lista=$(mktemp)
trap 'rm -f "$lista"' EXIT

# EL TITULO SE CONVIERTE EN ANCLA COMO LO HACE GITHUB, y las dos veces que
# esto estuvo mal el script mintio entero. Los dos rastrillos:
#
#   1. Los espacios NO se colapsan. "## 13 · El print" da "13--el-print", con
#      DOS guiones: el punto medio desaparece y sus dos espacios quedan.
#
#   2. Los multibyte se quitan UNO POR UNO y ANTES de cualquier clase [...].
#      Adentro de una clase, sed en locale de bytes parte el caracter multibyte
#      y se come bytes sueltos de otros -por eso el · quedaba a medias y NINGUNA
#      ancla resolvia-.
slugify() {
	sed -e 's/^#\{1,6\}[[:space:]]*//' \
	    -e 's/·//g' -e 's/—//g' -e 's/–//g' -e 's/…//g' \
	    -e 's/«//g' -e 's/»//g' \
	    -e 's/“//g' -e 's/”//g' -e 's/‘//g' -e 's/’//g' \
	    -e 's/⚠//g' -e 's/️//g' \
	    -e 's/`//g' \
	    -e 's/\*\*//g' -e 's/\*//g' \
	    -e 's/\[\([^]]*\)\](\([^)]*\))/\1/g' \
	    -e 's/[][(){}.,:;!?"'"'"'\/]//g' \
	    -e 's/ /-/g' \
	  | tr 'ABCDEFGHIJKLMNOPQRSTUVWXYZ' 'abcdefghijklmnopqrstuvwxyz'
}

# Primero se juntan todos los enlaces en una lista, y despues se verifican.
# En dos pasadas y no en una porque el conteo de rotos tiene que sobrevivir:
# un while alimentado por una tuberia corre en subshell y sus variables se
# pierden al terminar.
while IFS= read -r archivo; do
	dir=$(dirname "$archivo")
	# LOS BLOQUES DE CODIGO NO LLEVAN ENLACES. Un ``` abre y el siguiente
	# cierra; lo de adentro es texto de ejemplo. Sin esto, cualquier guia que
	# MUESTRE markdown se reporta rota -y el primero en caer fue el README de
	# esta misma carpeta, que trae enlaces rotos a proposito como control-.
	awk '/^```/ { dentro = !dentro; next } !dentro' "$archivo" \
	| grep -o '](\([^)]*\))' 2>/dev/null | sed 's/^](//; s/)$//' \
	| while IFS= read -r enlace; do
		case "$enlace" in
			http*|mailto:*|"") continue ;;
		esac
		ruta="${enlace%%#*}"
		ancla=""
		case "$enlace" in *#*) ancla="${enlace#*#}" ;; esac
		if [ -z "$ruta" ]; then apunta="$archivo"; else apunta="$dir/$ruta"; fi
		printf '%s\t%s\t%s\t%s\n' "$archivo" "$enlace" "$apunta" "$ancla"
	done
done < <(find . -name "*.md" -not -path "*/bin/*" -not -path "*/obj/*") > "$lista"

revisados=0
rotos=0
while IFS=$'\t' read -r origen enlace apunta ancla; do
	revisados=$((revisados + 1))

	if [ ! -e "$apunta" ]; then
		echo "NO EXISTE   $origen  ->  $enlace"
		rotos=$((rotos + 1))
		continue
	fi

	# Un enlace a un directorio es valido y no tiene anclas que revisar.
	[ -d "$apunta" ] && continue

	if [ -n "$ancla" ]; then
		if ! grep '^#\{1,6\}[[:space:]]' "$apunta" | slugify | grep -qxF "$ancla"; then
			echo "ANCLA ROTA  $origen  ->  $enlace"
			rotos=$((rotos + 1))
		fi
	fi
done < "$lista"

echo "-----"
echo "enlaces revisados: $revisados    rotos: $rotos"
[ "$rotos" -eq 0 ]
