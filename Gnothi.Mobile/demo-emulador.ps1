# Levanta la demo entera en un emulador, de cero: crea el AVD si no esta, lo arranca,
# instala el apk, mete los modulos y abre la app.
#
# Verificado el 2026-08-26 sobre un Pixel 6 / Android 14 (SDK 34) x86_64.
#
# TRES TRAMPAS QUE ESTE SCRIPT ESQUIVA, y ninguna es obvia:
#
#   1. El emulador se cuelga sin '-gpu host'. Por omision intenta un fallback de software
#      que en esta maquina no existe: "Critical: Failed to load opengl32sw" y no arranca
#      nunca. adb no lo ve jamas y parece que no pasa nada.
#
#   2. 'adb push' a /sdcard/Android/data/<paquete>/ NO FUNCIONA en el emulador. La imagen
#      es google_apis_playstore y el scoped storage lo bloquea: corta con
#      "failed to read copy response: EOF" despues del primer archivo. En el telefono
#      fisico si anda. Por eso aca vamos por /data/local/tmp y de ahi con 'run-as'.
#
#   3. El nombre de la Activity lleva un hash que cambia en cada recompilacion. Nunca se
#      copia: se resuelve cada vez.

param(
    [string] $Avd     = 'Gnothi',
    [string] $Sdk     = 'C:\Program Files (x86)\Android\android-sdk',
    [string] $Imagen  = 'system-images;android-34;google_apis_playstore;x86_64',

    # EL MODO DE GPU, Y ES UNA TRAMPA MEDIDA. Con 'host' el emulador va rapido y `adb shell
    # screencap` FALLA -- tira "Assertion failed: !rcEnc->featureInfo()->hasReadColorBufferDma"
    # y deja un archivo de 72 bytes con ese texto adentro. O sea: un PNG que no es un PNG, y
    # el sintoma se parece a un problema de rutas de adb, que es donde se pierde el rato.
    #
    # 'swiftshader_indirect' renderiza por software: mas lento, y screencap anda. Por eso la
    # captura programatica -- la que hace que una prueba sea reproducible por otra persona --
    # pide este modo. Medido el 2026-09-08 sobre el AVD Gnothi37k4, SDK 37.
    [string] $Gpu     = 'host',

    [switch] $Recrear                     # borra los datos de la app antes de empezar
)

# NO 'Stop': en PowerShell 5.1 el stderr de un ejecutable NATIVO se convierte en ErrorRecord
# y con 'Stop' eso mata el script. adb escribe "device offline" por stderr mientras el
# emulador arranca, que es normal y esperado. Los errores se miran a mano con $LASTEXITCODE.
$ErrorActionPreference = 'Continue'

# adb escribe su PROGRESO por stderr -- cuantos archivos empujo, que instalo -- y PowerShell
# lo pinta en rojo como si fueran errores. No lo son. Capturarlo lo silencia y deja ver solo
# la ultima linea, que es la que dice como fue.
function Adb {
    # Cada linea de stderr llega como ErrorRecord, no como texto. Se la desenvuelve para
    # recuperar el mensaje crudo -- si no, lo que se imprime es el volcado del error de
    # PowerShell y no lo que adb quiso decir.
    # @(...) es obligatorio: con una sola linea de salida el resultado NO es un arreglo, es
    # un string, y $lineas[-1] devuelve un [char] en vez de la linea. Falla en el caso mas
    # comun, que es el que menos se prueba.
    $lineas = @(& $adb @args 2>&1 | ForEach-Object {
        if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.Exception.Message } else { "$_" }
    } | Where-Object { "$_".Trim() })

    if ($lineas.Count) { "$($lineas[-1])".Trim() } else { '' }
}
$paquete = 'com.ncubo.gnothi.host'
$adb     = "$Sdk\platform-tools\adb.exe"
$emu     = "$Sdk\emulator\emulator.exe"
$env:ANDROID_HOME = $Sdk

# --- el AVD ---------------------------------------------------------------------------
if ((& $emu -list-avds) -notcontains $Avd) {
    Write-Host "creando el AVD $Avd..."
    'no' | & "$Sdk\cmdline-tools\latest\bin\avdmanager.bat" create avd -n $Avd -k $Imagen -d pixel_6
}

# --- arrancarlo -----------------------------------------------------------------------
if (-not (& $adb devices | Select-String 'emulator-\d+\s+device')) {
    Write-Host "arrancando el emulador (la primera vez tarda ~75s)..."
    Start-Process $emu -ArgumentList "-avd $Avd -gpu $Gpu -no-snapshot-load -no-boot-anim" -WindowStyle Normal

    $limite = (Get-Date).AddMinutes(5)
    while ((Get-Date) -lt $limite) {
        Start-Sleep -Seconds 5
        if ((& $adb -e shell getprop sys.boot_completed 2>$null) -match '1') { break }
    }
    if ((& $adb -e shell getprop sys.boot_completed 2>$null) -notmatch '1') {
        throw "el emulador no arranco. Mira si dice 'Failed to load opengl32sw' -- probá otro -gpu."
    }
}
Write-Host "emulador listo: $(& $adb -e shell getprop ro.product.model)"

# --- el apk ---------------------------------------------------------------------------
$apk = "$PSScriptRoot\bin\Debug\net9.0-android\$paquete-Signed.apk"
if (-not (Test-Path $apk)) {
    Write-Host "no hay apk, compilando..."
    dotnet build "$PSScriptRoot\Gnothi.Mobile.csproj" -c Debug -v q --nologo
    if ($LASTEXITCODE -ne 0) { throw "fallo el build" }
}

if ($Recrear) { & $adb -e shell pm clear $paquete | Out-Null }

Write-Host "instalando..."
Write-Host "  $(Adb -e install -r $apk)"

# --- los modulos ----------------------------------------------------------------------
$pkgDir = "$PSScriptRoot\paquete"
if (-not (Test-Path "$pkgDir\Modulos")) {
    Write-Host "no hay paquete de modulos, armandolo..."
    & "$PSScriptRoot\armar-paquete.ps1"
}

Write-Host "metiendo los modulos..."
& $adb -e shell rm -rf /data/local/tmp/gn 2>&1 | Out-Null
Write-Host "  $(Adb -e push "$pkgDir\Modulos" /data/local/tmp/gn/Modulos)"
Write-Host "  $(Adb -e push "$pkgDir\journals" /data/local/tmp/gn/journals)"
$null = Adb -e shell "run-as $paquete sh -c 'rm -rf files/Modulos files/journals; cp -r /data/local/tmp/gn/Modulos files/ && cp -r /data/local/tmp/gn/journals files/'"

# --- abrirla --------------------------------------------------------------------------
$activity = (& $adb -e shell cmd package resolve-activity --brief $paquete | Select-Object -Last 1).Trim()
& $adb -e shell am force-stop $paquete
& $adb -e logcat -c
& $adb -e shell am start -n $activity | Out-Null

Write-Host ""
Write-Host "Abierta. Para ver que hace por dentro, en otra terminal:"
Write-Host "  `"$adb`" -e logcat -s GNOTHI:I"
