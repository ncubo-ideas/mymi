# LEVANTA EL ESCENARIO COMPLETO de la prueba del modulo web hospedado, y lo deja listo para
# que alguien haga la prueba a mano. No prueba nada por si mismo: monta el mundo.
#
# Por cada institucion del POC son DOS servidores:
#
#   publicador     su narrativa (<Modulo>/EndPointDeEventos)
#   institucion    su pagina con el boton, su app suelta en /app/, y el relevo de eventos
#
#   examenes      :8095 publicador   :8096 institucion
#   compras       :8101              :8102
#   cargas        :8111              :8112
#   dealership    :8092              :8093
#   municipalidad :8121              :8122
#
# Mas un tunel `adb reverse` por puerto, que es lo que hace que el aparato los alcance.
#
# POR QUE UN SOLO SERVIDOR PARA LA PAGINA Y LA APP DE CADA UNA. La app suelta pide sus eventos
# en una URL RELATIVA a su propio origen y se publica con `<base href="/app/">`. Servida sola
# en la raiz de otro puerto no arranca: es el mismo binario que va adentro del bundle y espera
# vivir bajo el origen de su institucion. Medido el 2026-09-04 -- daba "Ocurrio un error
# inesperado", sin mencionar archivos estaticos por ningun lado.
#
# POR QUE `adb reverse` Y NO LA IP DE LA MAQUINA. Gnothi solo admite trafico en claro contra
# 127.0.0.1 y localhost (Resources/xml/network_security_config.xml). Contra una IP de LAN o
# contra 10.0.2.2 el cartero muere con "Connection failure" y el modulo se abre vacio, sin que
# nada diga por que. `adb reverse` hace que 127.0.0.1 DEL APARATO sea esta maquina, y con eso
# el emulador y el telefono fisico se prueban con las MISMAS urls.
#
# Verificado el 2026-09-04 sobre un emulador sdk_gphone64_x86_64 (Android 16 / SDK 36).

param(
    [string]   $Sdk      = 'C:\Program Files (x86)\Android\android-sdk',
    [string[]] $Modulos  = @('examenes', 'compras', 'cargas', 'dealership', 'municipalidad'),
    [switch]   $SinBundle,        # no rearma los bundles (mas rapido si no cambio nada)
    [switch]   $Detener           # baja todo y limpia los tuneles
)

$ErrorActionPreference = 'Continue'

$raiz    = Split-Path -Parent $PSScriptRoot
$paquete = 'com.ncubo.gnothi.host'
$adb     = "$Sdk\platform-tools\adb.exe"
$logs    = "$env:TEMP\gnothi-escenario"

# LAS CINCO INSTITUCIONES. Los puertos del publicador tienen que coincidir con los de
# armar-bundle.ps1: el manifiesto los lleva ADENTRO y el modulo no los puede cambiar despues.
#
# Y el directorio del concesionario ahora es Concesionario, no DealershipCar: master lo renombro
# en 0110017. Cambio la RUTA, no la marca -- su Titulo sigue en ingles porque el modulo entero
# esta en ingles, y sus tipos tambien (DealershipCarAPI). Si esto se vuelve a romper, es esto.
#
# El publicador de municipalidad es el 8121 y NO es una eleccion libre: es el mismo puerto que su
# docker-compose publica, asi que los contenedores del modulo hay que bajarlos antes de correr
# esto. Su Web quedo en 8122 -- pegado a su publicador, y libre.
$catalogo = [ordered]@{
    'examenes'      = @{ Mock = "$raiz\Examenes\EndPointDeEventos";      Pub = 8095; Web = 8096
                         Titulo = 'Laboratorio Clinico Delfos'; Subtitulo = 'Tus examenes, al dia' }
    'compras'       = @{ Mock = "$raiz\Compras\EndPointDeEventos";       Pub = 8101; Web = 8102
                         Titulo = 'Supermercado La Guaria';     Subtitulo = 'La Guaria y vos' }
    'cargas'        = @{ Mock = "$raiz\Cargas\EndPointDeEventos";        Pub = 8111; Web = 8112
                         Titulo = 'Estaciones Faro';            Subtitulo = 'Tus cargas' }
    'dealership'    = @{ Mock = "$raiz\Concesionario\EndPointDeEventos"; Pub = 8092; Web = 8093
                         Titulo = 'Sotavento Motors';           Subtitulo = 'Your maintenance' }
    'municipalidad' = @{ Mock = "$raiz\Municipalidad\EndPointDeEventos"; Pub = 8121; Web = 8122
                         Titulo = 'Tu municipalidad';           Subtitulo = 'Mi cuenta municipal' }
}

if (-not (Test-Path $adb)) { throw "no encuentro adb en $adb. Pasa -Sdk con la ruta de tu SDK de Android." }

function Puerto-Vivo([int] $p) {
    try { $null = Invoke-WebRequest "http://127.0.0.1:$p/salud" -UseBasicParsing -TimeoutSec 2; $true }
    catch { "$($_.Exception.Message)" -match '40[0-9]' }   # un 401/404 tambien prueba que hay alguien
}

# --- bajar todo -------------------------------------------------------------------------
if ($Detener) {
    Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" |
        Where-Object { $_.CommandLine -match 'EventosMock|PaginaDeLaInstitucion' } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
    Get-Process -ErrorAction SilentlyContinue |
        Where-Object { $_.ProcessName -match 'EventosMock|PaginaDeLaInstitucion' } |
        Stop-Process -Force -ErrorAction SilentlyContinue
    & $adb reverse --remove-all 2>&1 | Out-Null
    Write-Host "escenario detenido y tuneles limpios."
    return
}

# --- el aparato -------------------------------------------------------------------------
$dispositivos = @(& $adb devices | Select-String '\sdevice$')
if (-not $dispositivos) {
    throw "no hay ningun aparato. Arranca el emulador (demo-emulador.ps1) o conecta el telefono con depuracion USB, y acepta el dialogo de autorizacion en su pantalla."
}
Write-Host "aparato    : $((& $adb shell getprop ro.product.model).Trim())  (SDK $((& $adb shell getprop ro.build.version.sdk).Trim()), $((& $adb shell getprop ro.product.cpu.abi).Trim()))"
Write-Host "modulos    : $($Modulos -join ', ')"

New-Item -ItemType Directory -Path $logs -Force | Out-Null

# OJO con el nombre del segundo parametro: `$args` es variable AUTOMATICA de PowerShell -- los
# argumentos sin ligar de la propia funcion -- y pisa cualquier parametro que se llame igual.
# El sintoma es cruel: Start-Process falla por argumento nulo, el script sigue y anuncia que
# levanto el servidor. Medido el 2026-09-04.
function Levantar($nombre, $argumentos, $puerto) {
    if (Puerto-Vivo $puerto) { Write-Host "    :$puerto $nombre ya estaba arriba"; return }

    # QUOTE WHATEVER CONTAINS A SPACE, and this is the same family of trap as the note above:
    # it fails silently and the script still announces success.
    #
    # Start-Process joins -ArgumentList with spaces and adds NO quotes of its own. Measured:
    # with the array as it comes, dotnet receives `--Titulo Supermercado La Guaria`, binds
    # Titulo to "Supermercado", and `La` and `Guaria` arrive as two stray arguments. The page
    # then comes up titled "Supermercado" and nothing anywhere says why.
    #
    # It also protects the PATHS: --project, --Bundle and --AppSuelta carry $raiz, so a clone
    # in a directory whose name has a space breaks every server for the same reason.
    #
    # No element here ends in a backslash. If one ever does, it needs doubling first: on
    # Windows a trailing "\" inside quotes escapes the closing quote.
    $quoted = $argumentos | ForEach-Object {
        if ($_ -match '\s') { '"' + $_ + '"' } else { $_ }
    }

    Start-Process dotnet -ArgumentList $quoted -WindowStyle Hidden `
        -RedirectStandardOutput "$logs\$nombre.log" -RedirectStandardError "$logs\$nombre.err"
    Write-Host "    :$puerto $nombre levantado"
}

$puertos = @()

foreach ($nombre in $Modulos) {
    if (-not $catalogo.Contains($nombre)) { Write-Warning "no conozco el modulo '$nombre'; lo salto"; continue }
    $m = $catalogo[$nombre]

    Write-Host "  $nombre"

    # EL BUNDLE DEPENDE DEL PUERTO: el manifiesto lleva la URL del publicador adentro.
    $zip = "$PSScriptRoot\bundles\$nombre.gmod"
    if (-not $SinBundle -or -not (Test-Path $zip)) {
        Write-Host "    armando el bundle..."
        # -Origen: the page's own origin, so the manifest declares the origin that serves it and
        # Gnothi's origin check comes back "verificado" (over http: consistent, not authentic).
        & "$PSScriptRoot\armar-bundle.ps1" -Modulo $nombre -Publicador "http://127.0.0.1:$($m.Pub)/" -Origen "http://127.0.0.1:$($m.Web)" | Out-Null
    }
    if (-not (Test-Path $zip)) { Write-Warning "no se armo el bundle de $nombre"; continue }

    Levantar "pub-$nombre" @(
        "run", "--project", $m.Mock, "--urls", "http://0.0.0.0:$($m.Pub)") $m.Pub

    # La app suelta se sirve desde el MISMO publish que va adentro del bundle: asi lo que se
    # prueba suelto y lo que se prueba hospedado son el mismo binario (11 §2).
    Levantar "web-$nombre" @(
        "run", "--project", "$raiz\PaginaDeLaInstitucion",
        "--urls", "http://0.0.0.0:$($m.Web)",
        "--Bundle", $zip,
        "--AppSuelta", "$PSScriptRoot\bundles\$nombre\web",
        "--Publicador", "http://127.0.0.1:$($m.Pub)/",
        "--UrlPublica", "http://127.0.0.1:$($m.Web)",
        "--Titulo", $m.Titulo,
        "--Subtitulo", $m.Subtitulo,
        # The signing key armar-bundle.ps1 created: the page publishes its public half. And the
        # keys the institution retired (Gnothi.Host --revoke), if any.
        "--Llave", "$PSScriptRoot\bundles\.keys\$nombre.key.json",
        "--LlavesRetiradas", "$PSScriptRoot\bundles\.keys\$nombre.revoked.json") $m.Web

    $puertos += $m.Pub, $m.Web
}

Start-Sleep -Seconds 12

foreach ($p in $puertos) {
    if (-not (Puerto-Vivo $p)) { Write-Warning "el puerto $p no contesta. Mira $logs\*.err" }
}

# --- los tuneles ------------------------------------------------------------------------
# --remove-all primero: un reverse viejo apuntando a un puerto muerto no da error, simplemente
# no contesta, y eso se ve igual que "el servidor no arranco". Costo un rato el 2026-09-03.
& $adb reverse --remove-all 2>&1 | Out-Null
foreach ($p in $puertos) { & $adb reverse "tcp:$p" "tcp:$p" 2>&1 | Out-Null }
Write-Host "tuneles    : $((& $adb reverse --list).Count) puertos"

# --- la app -----------------------------------------------------------------------------
$apk = "$PSScriptRoot\bin\Debug\net9.0-android\$paquete-Signed.apk"
if (-not (Test-Path $apk)) {
    Write-Host "no hay apk, compilando..."
    dotnet build "$PSScriptRoot\Gnothi.Mobile.csproj" -c Debug -v q --nologo
    if ($LASTEXITCODE -ne 0) { throw "fallo el build de Gnothi.Mobile" }
}
# INSTALL, KEEPING THE DATA. On the G37 emulator `install -r` dies with
# INSTALL_FAILED_INSUFFICIENT_STORAGE ("Failed to override installation location") even with room
# to spare; `pm uninstall -k` -keep data and cache- followed by a plain install gets the new build in
# with the modules, journals and pins of the previous one untouched (measured 2026-09-09).
#
# Y SE MIRA EL CODIGO DE SALIDA, NO LA PALABRA «Success». Esto decia
# `if ("$instalado" -notmatch 'Success')`, y adb 36 NO IMPRIME ESA PALABRA: contesta
# «Install command complete in 3323 ms». Con eso, un install -r que FUNCIONO se leia como
# fallado y el guion caia SIEMPRE al desinstalar -- que es justo lo que el camino de abajo
# existe para evitar.
#
# Y el precio no eran unos segundos: desinstalar el paquete le BORRA AL LANZADOR SUS ACCESOS
# FIJADOS. `pm uninstall -k` conserva modulos y journals -- son datos de la app -- pero los
# iconos de la pantalla de inicio no son datos de la app, son del lanzador, y se van con el
# paquete. Medido el 2026-09-14: despues de una corrida, `dumpsys shortcut` ya no nombraba a
# com.ncubo.gnothi.host.
#
# El comentario de arriba decia «con los pins de la anterior intactos (medido 2026-09-09)».
# Era cierto cuando se midio, con un adb que decia Success.
& $adb install -r $apk | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "    install -r no pudo (codigo $LASTEXITCODE); reinstalando sin perder los datos..."
    Write-Host "    OJO: esto BORRA los accesos fijados en la pantalla de inicio; hay que volver a fijarlos."
    & $adb shell pm uninstall -k $paquete 2>&1 | Out-Null
    Start-Sleep -Seconds 3
    & $adb install $apk | Out-Null
    $instalado = if ($LASTEXITCODE -eq 0) { "reinstalado (se perdieron los accesos fijados)" } else { "FALLO (codigo $LASTEXITCODE)" }
}
else { $instalado = "actualizado en el lugar; los accesos fijados siguen" }
Write-Host "apk        : $instalado"

# EL DOMINIO, APROBADO A MANO. Chrome solo le entrega un enlace https a una app cuando esa app
# es un handler VERIFICADO del dominio, y verificar go.gnothi.app de verdad exige servir un
# assetlinks.json con la huella de firma desde ese dominio. Sin esto, tocar "Agregar a Gnothi"
# en la pagina no abre Gnothi: Chrome navega el mismo y muestra DNS_PROBE_FINISHED_NXDOMAIN.
# Requiere que el IntentFilter lleve AutoVerify.
& $adb shell pm set-app-links --package $paquete 2 go.gnothi.app 2>&1 | Out-Null
$aprobado = (& $adb shell pm get-app-links $paquete 2>&1) -join ' '
if ($aprobado -notmatch 'approved') {
    Write-Warning "no se pudo aprobar go.gnothi.app como app link. El boton de la pagina no va a abrir Gnothi."
}

# La pantalla apagada da capturas en negro y hace que el emulador no pinte.
& $adb shell svc power stayon true 2>&1 | Out-Null

Write-Host ""
Write-Host "ESCENARIO ARRIBA."
foreach ($nombre in $Modulos) {
    if (-not $catalogo.Contains($nombre)) { continue }
    $m = $catalogo[$nombre]
    Write-Host ("  {0,-11} pagina http://127.0.0.1:{1}/   app suelta http://127.0.0.1:{1}/app/" -f $nombre, $m.Web)
}
Write-Host ""
Write-Host "Para mirar por dentro, en otra terminal (SON TRES TAGS):"
# `modulo:I` y NO `modulo:examenes:I`: -s lee cada termino como TAG:PRIORIDAD, asi que un tag
# con dos puntos es un termino invalido, y uno invalido hace que logcat rechace TODA la lista.
# El nombre del modulo va al principio del mensaje. Medido el 2026-09-07.
Write-Host "  `"$adb`" logcat -s GNOTHI:I gnothi:I modulo:I"
Write-Host ""
Write-Host "Para bajar todo:  .\escenario-webview.ps1 -Detener"
