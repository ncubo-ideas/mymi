#!/usr/bin/env bash
# Pone la sonda donde la PWA instalada la pueda abrir.
#
# LA TRAMPA QUE ESTO EXISTE PARA EVITAR: el servidor de la institucion NO sirve desde el repo.
# Sirve desde la carpeta que se le pasa en --AppSuelta, que en las corridas es
# %TEMP%\examenes-publicada\wwwroot. Copiar la sonda al wwwroot del repo y abrirla en el telefono
# da 404, o peor, sirve una version vieja -- eso ya paso con el .gmod y costo una tarde.
#
# La sonda tiene que correr DENTRO de la PWA instalada, no en una pestaña: el veredicto de varias
# filas depende del origen y de display-mode=standalone. La propia sonda lo mide y lo dice arriba.
set -e

HERE="$(cd "$(dirname "$0")" && pwd)"
DESTINOS=(
  "$LOCALAPPDATA/Temp/examenes-publicada/wwwroot"
  "$HERE/../../Examenes/PaginaDeDescarga/publicado/wwwroot"
  "$HERE/../../Examenes/App/Examenes.Wasm/wwwroot"
)

copiadas=0
for d in "${DESTINOS[@]}"; do
  if [ -d "$d" ]; then
    cp "$HERE/sonda.html" "$d/sonda.html"
    echo "copiada -> $d/sonda.html"
    copiadas=$((copiadas + 1))
  else
    echo "(no existe, se saltea) $d"
  fi
done

[ "$copiadas" -eq 0 ] && { echo "ERROR: ningun destino existe. Publica la PWA primero."; exit 1; }

cat <<'FIN'

Ahora, en el telefono:

  1. abrir la PWA instalada (el icono), no una pestaña de Chrome
  2. navegar a  <origen-de-la-pwa>/app/sonda.html   (el servidor monta la PWA en RequestPath=/app)
     -- la sonda confirma arriba si display-mode es standalone; si dice PESTAÑA, no cuenta
  3. del lado de la maquina, con el aparato conectado:

       adb logcat -c
       adb logcat -s chromium

     Eso es EL INSTRUMENTO de dos filas: desde JS, un bloqueo por contenido mixto y un
     "connection refused" se ven IDENTICOS. Solo el log de chromium los distingue.

  4. "Volcar como markdown" y pegar el resultado en F1

Y para la fila de AIDL/Binder, el instrumento es el otro lado:

       adb logcat -s GNOTHI

  Si la PWA intenta cualquier escapada hacia IpcService y ahi no aparece NI UN bind,
  eso es la medicion: la llamada no llega, Authorize nunca corre.
FIN
