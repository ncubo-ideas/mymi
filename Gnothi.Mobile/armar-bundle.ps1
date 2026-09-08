# ARMA EL BUNDLE DE UNA INSTITUCION: el zip que su pagina publica y que Gnothi baja al dar
# de alta el modulo (especificacion 11 §3, 13 §7).
#
# El bundle son DOS cosas y nada mas:
#
#   manifest.json   quien es la institucion, que ofrece, y donde vive su narrativa
#   web/            su app publicada, tal cual sale de `dotnet publish`
#
# NO trae ejecutable ni instalacion. La instalacion la escribe Gnothi cuando lo adopta,
# porque es su archivo y su autoridad: la institucion declara donde esta su publicador, y
# Gnothi lo fija. De ahi en adelante el modulo no lo puede cambiar.
#
# POR QUE EL PUERTO ES UN PARAMETRO. El manifiesto lleva la URL del publicador ADENTRO, y en
# una prueba local esa URL es un tunel de `adb reverse` cuyo puerto lo elige quien corre la
# prueba. Un puerto quemado aca daria un modulo que instala bien y despues no trae nada, sin
# que nada diga por que.
#
# LA INSTITUCION SE NOMBRA COMO SE NOMBRA EN SU MARCA -la misma que su manifest.webmanifest-,
# porque desde el 2026-09-08 ese nombre es lo GRANDE de la tarjeta en Gnothi, y su icono y su
# color salen del webmanifest del mismo bundle (Gnothi.Mobile/Marca.cs). Nada de sufijos
# «(mock del POC)»: eso es prosa de repo, no de tarjeta. Este archivo va con BOM a proposito:
# PowerShell 5.1 lee un .ps1 sin BOM como ANSI y «Clínico» llegaria roto al manifiesto.
#
# EL ESLOGAN es una linea corta de la institucion, en su voz, que Gnothi pone debajo de la
# marca en la tarjeta en vez del titulo del modulo. Los de aca son PROPUESTA (2026-09-08),
# sacados de la pagina de cada una; la especificacion de marca de Geaninna manda.
#
# LAS TOOLS DECLARADAS TIENEN QUE COINCIDIR con lo que el modulo contesta de verdad: Gnothi
# compara las dos listas al dar de alta (`comprobacion de <modulo>: matches`). Por eso salen
# de esta tabla y no se escriben a mano en cada corrida.

param(
    [string] $Modulo     = 'examenes',
    [string] $Proyecto   = '',        # se deduce del modulo si no se pasa
    [string] $Publicador = '',        # por omision, el puerto de su propio mock
    [string] $Recurso    = '',
    [string] $Id         = '',
    [string] $Salida     = ''         # por omision: <Gnothi.Mobile>/bundles
)

$ErrorActionPreference = 'Stop'

$raiz = Split-Path -Parent $PSScriptRoot

# LOS CUATRO MODULOS DEL POC. Cada institucion declara lo suyo; lo unico que comparten es la
# forma. DealershipCar va en ingles porque el modulo entero esta en ingles.
$catalogo = @{
    'examenes' = @{
        Proyecto    = "$raiz\Examenes\App\Examenes.Wasm\Examenes.Wasm.csproj"
        Titulo      = 'Mis examenes'
        Institucion = 'Laboratorio Clínico Delfos'
        Slogan      = 'Tu salud, en tus manos'
        Color       = '#0e2f55'
        Recurso     = 'expediente'
        Id          = '1'
        Puerto      = 8095
        Tools       = @(
            @{ name = 'resumen'; title = 'Como vengo'
               description = 'Cuantos examenes tiene registrados la persona en este dispositivo, cuantos analitos estan fuera de rango, y hasta que evento del laboratorio leyo su app.' },
            @{ name = 'lista_de_examenes'; title = 'Mis examenes'
               description = 'La lista de examenes que la app del laboratorio tiene registrados en este dispositivo, con su fecha y su resultado.' }
        )
    }
    'compras' = @{
        Proyecto    = "$raiz\Compras\App\Compras.Wasm\Compras.Wasm.csproj"
        Titulo      = 'Mis compras'
        Institucion = 'Supermercado La Guaria'
        Slogan      = 'Tu canasta, más cerca de vos'
        Color       = '#2f6b3f'
        Recurso     = 'cliente'
        Id          = '1'
        Puerto      = 8101
        Tools       = @(
            @{ name = 'resumen'; title = 'Como vengo'
               description = 'Cuanto lleva gastado la persona segun lo registrado en este dispositivo, cuantas compras hay, y hasta que evento del supermercado leyo su app.' },
            @{ name = 'lista_de_compras'; title = 'Mis compras'
               description = 'La lista de compras que la app del supermercado tiene registradas en este dispositivo, con su fecha y su total.' }
        )
    }
    'cargas' = @{
        Proyecto    = "$raiz\Cargas\App\Cargas.Wasm\Cargas.Wasm.csproj"
        Titulo      = 'Mis cargas'
        Institucion = 'Estaciones Faro'
        Slogan      = 'Rendimiento real, tramo a tramo'
        Color       = '#0B3C49'
        Recurso     = 'cargas'
        Id          = '1'
        Puerto      = 8111
        Tools       = @(
            @{ name = 'resumen'; title = 'Como vengo'
               description = 'Cuantas cargas de combustible tiene registradas la persona en este dispositivo, cuantos vehiculos, y hasta que evento de la estacion leyo su app.' },
            @{ name = 'lista_de_vehiculos'; title = 'Mis vehiculos'
               description = 'Los vehiculos que la bitacora conoce, con su placa y cuantas cargas tiene cada uno. Es la llave para preguntar por uno en particular.' }
        )
    }
    'dealership' = @{
        Proyecto    = "$raiz\DealershipCar\App\DealershipCar.Wasm\DealershipCar.Wasm.csproj"
        Titulo      = 'My maintenance'
        Institucion = 'Sotavento Motors'
        Slogan      = 'Care that keeps you moving'
        Color       = '#1f3a5f'
        Recurso     = 'vehicle'
        Id          = '1HGCM82633A004352'
        Puerto      = 8092
        Tools       = @(
            @{ name = 'resumen'; title = 'How my car is doing'
               description = 'How the vehicle stands right now: how many maintenance operations are due, overdue and missed, and up to which dealership event the app has read.' },
            @{ name = 'hasta_donde_leyo'; title = 'How up to date this is'
               description = 'How far the app has read the dealership record and up to what instant the schedule was evaluated. If it goes up between two calls, new facts came in.' }
        )
    }
}

if (-not $catalogo.ContainsKey($Modulo)) {
    throw "no conozco el modulo '$Modulo'. Son: $($catalogo.Keys -join ', ')"
}
$m = $catalogo[$Modulo]

if (-not $Proyecto)   { $Proyecto = $m.Proyecto }
if (-not $Recurso)    { $Recurso = $m.Recurso }
if (-not $Id)         { $Id = $m.Id }
if (-not $Publicador) { $Publicador = "http://127.0.0.1:$($m.Puerto)/" }

if (-not (Test-Path $Proyecto)) { throw "no existe el proyecto: $Proyecto" }

if (-not $Salida) { $Salida = "$PSScriptRoot\bundles" }
$armado = "$Salida\$Modulo"
$zip    = "$Salida\$Modulo.zip"

# --- publicar la app -------------------------------------------------------------------
Write-Host "publicando $Modulo..."
$publicado = "$Salida\.publish-$Modulo"
if (Test-Path $publicado) { Remove-Item -Recurse -Force $publicado }

dotnet publish $Proyecto -c Release -o $publicado -v q --nologo | Out-Null
if ($LASTEXITCODE -ne 0) { throw "fallo el publish de $Proyecto" }

# `dotnet publish` de un Blazor Wasm deja la app en wwwroot/ y al lado deja los dll del
# publish, que NO van al bundle: lo que la institucion sirve es exactamente lo que el
# navegador pide.
$web = "$publicado\wwwroot"
if (-not (Test-Path $web)) { throw "el publish no dejo wwwroot en $publicado" }

# --- armar el bundle -------------------------------------------------------------------
if (Test-Path $armado) { Remove-Item -Recurse -Force $armado }
New-Item -ItemType Directory -Path $armado -Force | Out-Null
Copy-Item -Recurse $web "$armado\web"

$manifiesto = [ordered]@{
    '//'          = 'ESTE ARCHIVO ES DE LA INSTITUCION. Declara quien es, que ofrece y donde vive su narrativa.'
    manifest      = '1.1'
    module        = $Modulo
    title         = $m.Titulo
    institution   = $m.Institucion
    '//brand'     = 'LA MARCA (1.1). El logo es una ruta relativa al bundle; el color, el theme_color de su PWA. Sin brand, Gnothi lee el manifest.webmanifest.'
    brand         = [ordered]@{ logo = 'web/icon-192.png'; color = $m.Color; slogan = $m.Slogan }
    substrate     = @{ range = '>=3.0.0 <4.0.0' }
    '//publicador'= 'DONDE ESTA SU NARRATIVA. Gnothi lo registra al dar de alta el modulo y a partir de ahi el endpoint es fijo: el modulo no lo elige ni lo puede cambiar.'
    publicador    = [ordered]@{ eventos = $Publicador; recurso = $Recurso; id = $Id }
    tools         = @($m.Tools | ForEach-Object {
        [ordered]@{ name = $_.name; title = $_.title; description = $_.description; readOnly = $true; input = @() }
    })
}

# SIN BOM, a proposito. `Set-Content -Encoding utf8` de PowerShell 5.1 escribe BOM, y aunque
# `File.ReadAllText` lo tolera -- que es como lo lee AltaDeModulo -- un manifiesto con BOM
# revienta en cuanto alguien lo lea con `JsonDocument.Parse(string)`. No vale la pena la trampa.
$json = $manifiesto | ConvertTo-Json -Depth 8
[System.IO.File]::WriteAllText("$armado\manifest.json", $json, (New-Object System.Text.UTF8Encoding($false)))

# --- el zip ----------------------------------------------------------------------------
if (Test-Path $zip) { Remove-Item -Force $zip }
Compress-Archive -Path "$armado\*" -DestinationPath $zip -CompressionLevel Optimal

Remove-Item -Recurse -Force $publicado

$mb = [math]::Round((Get-Item $zip).Length / 1MB, 1)
$n  = (Get-ChildItem -Recurse $armado -File).Count

Write-Host ""
Write-Host "bundle listo : $zip"
Write-Host "  archivos   : $n"
Write-Host "  tamano     : $mb MB"
Write-Host "  publicador : $Publicador$Recurso/$Id/events"
Write-Host "  tools      : $(($m.Tools | ForEach-Object { $_.name }) -join ', ')"
