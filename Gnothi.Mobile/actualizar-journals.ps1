# Sincroniza los journals de la compu hacia el telefono (o el emulador) y reinicia la app.
#
# POR QUE EXISTE: el journal tiene UN escritor, y es la app de la persona -- que corre en la
# compu. El telefono es LECTOR: sus modulos releen una copia. Entonces "inyectarle datos al
# movil" es el ciclo de escritorio de siempre...
#
#   1. docker compose up -d          (en la carpeta del negocio: el mock del proveedor)
#   2. dotnet run InyectorDeEventosDemo   (mete el evento nuevo en el mock)
#   3. la app de la persona -> Refresh    (ELLA escribe C:\Gnothi\<negocio>)
#
# ...mas ESTE paso: copiar esos journals al area privada de la app y reiniciarla, para que
# los modulos rehidraten y el numero nuevo aparezca en los botones, el chat y el Journal.
#
# El copiado va por /data/local/tmp + run-as porque adb no puede escribir el area privada
# (y esta bien que no pueda: nadie de afuera le escribe los datos a la persona).

param(
    [string] $Journals    = 'C:\Gnothi',
    [string] $Dispositivo = '',            # serial ('RFCW40SMN7R', 'emulator-5554'); vacio = el primero
    [string] $Sdk         = 'C:\Program Files (x86)\Android\android-sdk'
)

$ErrorActionPreference = 'Continue'
$paquete  = 'com.ncubo.gnothi.host'
$adb      = "$Sdk\platform-tools\adb.exe"
$negocios = @('examenes', 'facturas', 'compras')   # los mismos que arma armar-paquete.ps1

function Adb {
    $lineas = @(& $adb @args 2>&1 | ForEach-Object {
        if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.Exception.Message } else { "$_" }
    } | Where-Object { "$_".Trim() })
    if ($lineas.Count) { "$($lineas[-1])".Trim() } else { '' }
}

if (-not $Dispositivo) {
    $Dispositivo = (& $adb devices | Select-String '^(\S+)\s+device$').Matches.Groups[1].Value
    if (-not $Dispositivo) { throw "no hay ningun dispositivo conectado" }
}
Write-Host "dispositivo: $Dispositivo"

& $adb -s $Dispositivo shell rm -rf /data/local/tmp/gj 2>&1 | Out-Null

# El rm de incoming se intenta por DOS caminos porque cada hospedaje bloquea uno: en el
# telefono fisico el shell de adb puede escribir /sdcard/Android/data (por ahi se empujo
# el paquete), pero run-as no siempre alcanza /storage; en el emulador es al reves. Uno
# de los dos entra; el que no, falla en silencio y no importa.
& $adb -s $Dispositivo shell rm -rf "/sdcard/Android/data/$paquete/files/incoming/journals" 2>&1 | Out-Null

foreach ($n in $negocios) {
    $origen = Join-Path $Journals $n
    if (-not (Test-Path $origen)) { Write-Host "  OJO: no hay $origen, lo salteo"; continue }
    Write-Host "  $(Adb -s $Dispositivo push $origen /data/local/tmp/gj/$n)"
}

# El reemplazo es COMPLETO, no un merge: un journal es una historia, y una historia con
# pedazos de dos versiones no replaya. Todo o nada.
#
# Y TAMBIEN SE BORRA incoming/journals: el Host re-instala lo que haya ahi EN CADA
# arranque, asi que una copia vieja en el area de descarga reviviria y pisaria esta
# sincronizacion al reiniciar. Costo una prueba descubrirlo.
$externo = "/storage/emulated/0/Android/data/$paquete/files/incoming"
$null = Adb -s $Dispositivo shell "run-as $paquete sh -c 'rm -rf files/journals && mkdir files/journals && cp -r /data/local/tmp/gj/. files/journals/ && rm -rf $externo/journals'"
& $adb -s $Dispositivo shell rm -rf /data/local/tmp/gj 2>&1 | Out-Null

# Reinicio: los modulos rehidratan del journal al ser invocados, pero la app puede tener
# procesos a medio desalojar; arrancar limpio es lo que no deja dudas en una demo.
$activity = (& $adb -s $Dispositivo shell cmd package resolve-activity --brief $paquete | Select-Object -Last 1).Trim()
& $adb -s $Dispositivo shell am force-stop $paquete
& $adb -s $Dispositivo shell am start -n $activity | Out-Null

Write-Host ""
Write-Host "Journals actualizados y app reiniciada. Preguntale de nuevo y el numero es el nuevo."
