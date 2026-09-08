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
# LAS TOOLS DECLARADAS TIENEN QUE COINCIDIR con lo que el modulo contesta de verdad: Gnothi
# compara las dos listas al dar de alta (`comprobacion de <modulo>: matches`). Por eso salen
# de esta tabla y no se escriben a mano en cada corrida.

param(
    [string] $Modulo     = 'examenes',
    [string] $Proyecto   = '',        # se deduce del modulo si no se pasa
    [string] $Publicador = '',        # por omision, el puerto de su propio mock
    [string] $Recurso    = '',
    [string] $Id         = '',
    [string] $Salida     = '',        # por omision: <Gnothi.Mobile>/bundles

    # REESCRIBIR SOLO EL MANIFIESTO Y EL LOGO, sobre un bundle YA armado, y rezipear.
    #
    # Existe porque iterar la marca no tiene por que costar cuatro `dotnet publish` de Blazor
    # Wasm: la app no cambio, cambio lo que la institucion declara de si misma. Con esto el
    # ciclo de prueba en el emulador baja de minutos a segundos.
    [switch] $SoloManifiesto
)

$ErrorActionPreference = 'Stop'

$raiz = Split-Path -Parent $PSScriptRoot

# LOS CUATRO MODULOS DEL POC. Cada institucion declara lo suyo; lo unico que comparten es la
# forma. DealershipCar va en ingles porque el modulo entero esta en ingles.
$catalogo = @{
    'examenes' = @{
        Proyecto    = "$raiz/Examenes/App/Examenes.Wasm/Examenes.Wasm.csproj"
        Titulo      = 'Tus examenes, al dia'
        Institucion = 'Laboratorio Clinico Delfos'
        Marca       = 'Laboratorio Delfos'
        Color       = '#0E7C86'
        Rubro       = 'laboratorio'
        Descripcion = 'Tus resultados de laboratorio, con su historia y que te toca repetir.'
        NoHace      = 'No interpreta resultados ni reemplaza a tu medico.'
        Dominio     = 'delfos.cr'
        RazonSocial = 'Laboratorio Clinico Delfos S.A.'
        Cedula      = '3-101-123456'
        Version     = '2.4.0'
        Retencion   = '5 anos'
        Tamano      = '~2 MB al ano'
        Refresco    = 'al abrir la app'
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
        Proyecto    = "$raiz/Compras/App/Compras.Wasm/Compras.Wasm.csproj"
        Titulo      = 'La Guaria y vos'
        Institucion = 'Supermercado La Guaria'
        Marca       = 'Supermercado La Guaria'
        Color       = '#7B3FA0'
        Rubro       = 'supermercado'
        Descripcion = 'Tu historial de compras, con tus garantias y lo que sueles llevar.'
        NoHace      = 'No te dice que comer ni juzga tu canasta.'
        Dominio     = 'laguaria.cr'
        RazonSocial = 'Supermercado La Guaria S.A.'
        Cedula      = '3-101-654321'
        Version     = '1.2.0'
        Retencion   = '5 anos'
        Tamano      = '~4 MB al ano'
        Refresco    = 'al abrir la app'
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
        Proyecto    = "$raiz/Cargas/App/Cargas.Wasm/Cargas.Wasm.csproj"
        Titulo      = 'Tus cargas'
        Institucion = 'Estacion Los Higuerones'
        Marca       = 'Estacion Los Higuerones'
        Color       = '#C2410C'
        Rubro       = 'combustible'
        Descripcion = 'Tus cargas de combustible y el rendimiento de cada vehiculo.'
        NoHace      = ''
        Dominio     = 'higuerones.cr'
        RazonSocial = 'Estacion Los Higuerones S.A.'
        Cedula      = '3-101-777888'
        Version     = '1.0.3'
        Retencion   = '2 anos'
        Tamano      = '~1 MB al ano'
        Refresco    = 'al abrir la app'
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
        Proyecto    = "$raiz/DealershipCar/App/DealershipCar.Wasm/DealershipCar.Wasm.csproj"
        Titulo      = 'Your maintenance'
        Institucion = 'DealershipCar'
        Marca       = 'DealershipCar'
        Color       = '#1B3A6B'
        Rubro       = 'taller'
        Descripcion = 'Your vehicle maintenance schedule and what is due.'
        NoHace      = ''
        Dominio     = 'dealershipcar.com'
        RazonSocial = 'DealershipCar Inc.'
        Cedula      = ''
        Version     = '3.1.0'
        Retencion   = '10 years'
        Tamano      = '~2 MB/year'
        Refresco    = 'when the app opens'
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

if (-not $SoloManifiesto -and -not (Test-Path $Proyecto)) { throw "no existe el proyecto: $Proyecto" }

if (-not $Salida) { $Salida = "$PSScriptRoot/bundles" }
$armado = "$Salida/$Modulo"
$zip    = "$Salida/$Modulo.zip"

# --- publicar la app -------------------------------------------------------------------
$publicado = $null
if ($SoloManifiesto) {
    if (-not (Test-Path "$armado/web")) {
        throw "-SoloManifiesto necesita un bundle ya armado, y no hay $armado/web. Corre el script sin el switch la primera vez."
    }
    Write-Host "solo el manifiesto de $Modulo (la app queda como esta)"
} else {
    Write-Host "publicando $Modulo..."
    $publicado = "$Salida/.publish-$Modulo"
    if (Test-Path $publicado) { Remove-Item -Recurse -Force $publicado }

    dotnet publish $Proyecto -c Release -o $publicado -v q --nologo | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "fallo el publish de $Proyecto" }

    # `dotnet publish` de un Blazor Wasm deja la app en wwwroot/ y al lado deja los dll del
    # publish, que NO van al bundle: lo que la institucion sirve es exactamente lo que el
    # navegador pide.
    $web = "$publicado/wwwroot"
    if (-not (Test-Path $web)) { throw "el publish no dejo wwwroot en $publicado" }

    # --- armar el bundle ---------------------------------------------------------------
    if (Test-Path $armado) { Remove-Item -Recurse -Force $armado }
    New-Item -ItemType Directory -Path $armado -Force | Out-Null
    Copy-Item -Recurse $web "$armado/web"
}

# EL LOGO DE LA INSTITUCION, si tiene uno en logos/. Va adentro del bundle porque es de ella:
# el Host no trae logos de nadie. Y va como ARCHIVO y no como url, porque un logo traido de la
# red le contaria a la institucion cada vez que la persona abre su lista de modulos.
#
# La aduana del Host -Module.LogoDataUri- lo revisa igual al instalarlo: png, jpeg o webp por
# sus BYTES y no por su extension, hasta 64 KB y 512 pixeles de lado. Un logo que no pase se
# rechaza y el modulo entra con el dibujo de su rubro, con un warning que dice por que.
$logo = "$PSScriptRoot/logos/$Modulo.png"
$traeLogo = Test-Path $logo
if ($traeLogo) {
    New-Item -ItemType Directory -Path "$armado/brand" -Force | Out-Null
    Copy-Item $logo "$armado/brand/logo.png"
}

# OJO CON LAS TILDES EN ESTE ARCHIVO: ningun .ps1 del repo tiene BOM, y sin BOM PowerShell 5.1
# lee el script como ANSI -- una tilde aca sale corrupta en el telefono. Por eso los textos de
# los cuatro mocks son ASCII. No es una limitacion del formato: el manifest.json se escribe en
# UTF-8 sin BOM unas lineas mas abajo, y una institucion real escribe el suyo a mano con las
# tildes que quiera. Ver Herramientas/manifiesto-de-ejemplo.json, que las tiene.
$manifiesto = [ordered]@{
    '//'          = 'ESTE ARCHIVO ES DE LA INSTITUCION. Declara quien es, que ofrece y donde vive su narrativa.'
    manifest      = '1.1'
    module        = $Modulo
    title         = $m.Titulo
    institution   = $m.Institucion
    '//brand'     = 'LA MARCA. La institucion declara y el Host compone: ella pone el material, el decide el lugar, el tamano y el contraste.'
    brand         = [ordered]@{
        shortName = $m.Marca
        logo      = if ($traeLogo) { 'brand/logo.png' } else { '' }
        color     = $m.Color
    }
    '//publisher' = 'DE DONDE VIENE Y QUE VERSION ES. El dominio es el unico campo CONTRASTABLE: Gnothi ya sabe de que host bajo el bundle y compara.'
    publisher     = [ordered]@{
        domain    = $m.Dominio
        legal     = $m.RazonSocial
        taxId     = $m.Cedula
        version   = $m.Version
        released  = (Get-Date -Format 'yyyy-MM-dd')
    }
    '//about'     = 'QUE ES, EN PALABRAS PARA LA PERSONA. Las descripciones de las tools estan escritas para el modelo; esto es lo primero del manifiesto escrito para quien lo lee.'
    about         = [ordered]@{
        category    = $m.Rubro
        description = $m.Descripcion
        doesNotDo   = $m.NoHace
        locale      = if ($Modulo -eq 'dealership') { 'en-US' } else { 'es-CR' }
    }
    '//journal'   = 'LO QUE LA INSTITUCION PROMETE SOBRE EL REGISTRO. La pantalla del alta ya decia DONDE va a vivir; esto es cuanto, cada cuanto y por cuanto tiempo.'
    journal       = [ordered]@{
        retention    = $m.Retencion
        expectedSize = $m.Tamano
        updates      = $m.Refresco
    }
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
[System.IO.File]::WriteAllText("$armado/manifest.json", $json, (New-Object System.Text.UTF8Encoding($false)))

# --- el zip ----------------------------------------------------------------------------
if (Test-Path $zip) { Remove-Item -Force $zip }
Compress-Archive -Path "$armado/*" -DestinationPath $zip -CompressionLevel Optimal

if ($publicado -and (Test-Path $publicado)) { Remove-Item -Recurse -Force $publicado }

$mb = [math]::Round((Get-Item $zip).Length / 1MB, 1)
$n  = (Get-ChildItem -Recurse $armado -File).Count

Write-Host ""
Write-Host "bundle listo : $zip"
Write-Host "  archivos   : $n"
Write-Host "  tamano     : $mb MB"
Write-Host "  publicador : $Publicador$Recurso/$Id/events"
Write-Host "  tools      : $(($m.Tools | ForEach-Object { $_.name }) -join ', ')"
Write-Host "  marca      : $($m.Marca) $(if ($traeLogo) { "con logo ($([math]::Round((Get-Item $logo).Length / 1KB, 1)) KB)" } else { 'SIN logo -- entra con el dibujo de su rubro' })"
