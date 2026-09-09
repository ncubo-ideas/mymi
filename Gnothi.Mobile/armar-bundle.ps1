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
    [string] $Origen     = '',        # the origin that serves the bundle (scheme://host:port); goes into the manifest as "origin"
    [string] $Llave      = '',        # the institution's signing key file; created on first use. Default: <Salida>/.keys/<Modulo>.key.json
    [string] $Salida     = '',        # por omision: <Gnothi.Mobile>/bundles
    [string] $Version    = '',        # overrides the catalog's publisher.version, to rehearse an UPDATE of an installed module

    # REESCRIBIR SOLO EL MANIFIESTO Y EL LOGO, sobre un bundle YA armado, y rezipear.
    #
    # Existe porque iterar la marca no tiene por que costar cuatro `dotnet publish` de Blazor
    # Wasm: la app no cambio, cambio lo que la institucion declara de si misma. Con esto el
    # ciclo de prueba en el emulador baja de minutos a segundos. The bundle is signed again
    # afterwards, like any other: a rewritten manifest under the old signature would be refused.
    [switch] $SoloManifiesto
)

$ErrorActionPreference = 'Stop'

$raiz = Split-Path -Parent $PSScriptRoot

# LOS CINCO MODULOS DEL POC. Cada institucion declara lo suyo; lo unico que comparten es la
# forma. El concesionario tambien va en espanol: Kenneth paso su PWA a espanol el 2026-09-08 y la
# tarjeta lo sigue (2026-09-09).
#
# LA MUNICIPALIDAD NO SE NOMBRA, y no es una casilla pendiente: es la respuesta. El concesionario
# inventa una marca y no pasa nada; toda municipalidad plausible es una que existe, con vecinos que
# le pagan, asi que su modulo usa el sustantivo comun y su dominio es el TLD reservado para
# ejemplos. El nombre real llega el dia que una municipalidad de verdad adopte el modulo.
$catalogo = @{
    'examenes' = @{
        Proyecto    = "$raiz/Examenes/App/Examenes.Wasm/Examenes.Wasm.csproj"
        Titulo      = 'Tus examenes, al dia'
        Institucion = 'Laboratorio Clínico Delfos'
        Marca       = 'Delfos'
        Slogan      = 'Tu salud, en tus manos'
        Color       = '#0e2f55'
        Rubro       = 'laboratorio'
        Descripcion = 'Tus resultados de laboratorio, con su historia y que te toca repetir.'
        NoHace      = 'No interpreta resultados ni reemplaza a tu medico.'
        Dominio     = 'delfos.cr'
        RazonSocial = 'Laboratorio Clínico Delfos S.A.'
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
        Marca       = 'La Guaria'
        Slogan      = 'Tu canasta, más cerca de vos'
        Color       = '#2f6b3f'
        Fuente      = @{ family = 'Nunito Sans'; file = 'web/brand/NunitoSans-800.woff2' }
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
        Institucion = 'Estaciones Faro'
        Marca       = 'Faro'
        Slogan      = 'Rendimiento real, tramo a tramo'
        Color       = '#0B3C49'
        Fuente      = @{ family = 'Barlow'; file = 'web/brand/Barlow-600.woff2' }
        Rubro       = 'combustible'
        Descripcion = 'Tus cargas de combustible y el rendimiento de cada vehiculo.'
        NoHace      = ''
        Dominio     = 'faro.cr'
        RazonSocial = 'Estaciones Faro S.A.'
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
        Proyecto    = "$raiz/Concesionario/App/Concesionario.Wasm/Concesionario.Wasm.csproj"
        Titulo      = 'Mi plan de mantenimiento'
        Institucion = 'Sotavento Motors'
        Marca       = 'Sotavento Motors'
        Slogan      = 'Tu carro, siempre listo'
        Color       = '#1f3a5f'
        Fuente      = @{ family = 'Sora'; file = 'web/brand/Sora-600.woff2' }
        Rubro       = 'taller'
        Descripcion = 'El plan de mantenimiento de tu vehiculo y lo que le toca.'
        NoHace      = ''
        Dominio     = 'sotavento.com'
        RazonSocial = 'Sotavento Motors Inc.'
        Cedula      = ''
        Version     = '3.1.0'
        Retencion   = '10 anos'
        Tamano      = '~2 MB al ano'
        Refresco    = 'al abrir la app'
        Recurso     = 'vehicle'
        Id          = '1HGCM82633A004352'
        Puerto      = 8092
        Tools       = @(
            @{ name = 'resumen'; title = 'Como va mi carro'
               description = 'Como esta el vehiculo ahora mismo: cuantas operaciones de mantenimiento estan por vencer, vencidas y no hechas, y hasta que evento del concesionario leyo la app.' },
            @{ name = 'hasta_donde_leyo'; title = 'Que tan al dia esta esto'
               description = 'Hasta donde leyo la app el registro del concesionario y hasta que instante se evaluo el plan. Si sube entre dos llamadas, entraron hechos nuevos.' }
        )
    }
    'municipalidad' = @{
        Proyecto    = "$raiz/Municipalidad/App/Municipalidad.Wasm/Municipalidad.Wasm.csproj"
        Titulo      = 'Mi cuenta municipal'
        Institucion = 'Tu municipalidad'
        Marca       = 'Tu municipalidad'
        Slogan      = 'Lo que el portal no te dice'
        Color       = '#2C306C'
        Rubro       = 'gobierno'
        Descripcion = 'Tus cobros municipales: por qué te subieron, qué se te vence y cuánto apartar.'
        NoHace      = 'No dice cuánto debés hoy: eso lo dice el portal de tu municipalidad.'
        Dominio     = 'municipalidad.example'
        RazonSocial = 'Municipalidad del POC'
        Cedula      = ''
        Version     = '1.0.0'
        Retencion   = '5 anos'
        Tamano      = '~1 MB al ano'
        Refresco    = 'al abrir la app'
        Recurso     = 'contribuyente'
        Id          = 'CEDULA-DEMO-1'
        Puerto      = 8121
        Tools       = @(
            @{ name = 'resumen'; title = 'Como vengo'
               description = 'Cuántas fincas y cuántos rubros con historia tiene registrados la persona en este dispositivo, cuántos períodos están por vencer y cuántos vencidos al día de hoy, y hasta qué evento de la municipalidad leyó su app.' },
            @{ name = 'lista_de_vencimientos'; title = 'Que se me vence'
               description = 'Los períodos que se le vencen a la persona y los que ya se le vencieron, con su rubro, su monto, cuántos días faltan o cuántos lleva de atraso, y la frase con que el registro dice cada fila. Son DOS listas y no se suman. Es la pregunta que el portal de la municipalidad no contesta.' }
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
# ONE NAME FOR THE PACKAGE, whatever its form: the desktop's cargo is <modulo>.gmod, and so is the
# phone's hosted bundle since 2026-09-09. The two are told apart from INSIDE, by the manifest
# (load.entry vs web/), and a person who sees the file sees one kind of thing. Still a zip.
$zip    = "$Salida/$Modulo.gmod"

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
    # THE ORIGIN THE INSTITUTION CLAIMS. Gnothi compares it with the origin that actually served
    # the bundle and with what that origin publishes at /.well-known/gnothi/modules.json; when the
    # three agree the sheet says the module is backed by its origin. Left out when not given, and
    # then the sheet says the package declares no origin. It sits next to publisher.domain below,
    # which the brand line compares with the serving host on its own: two declarations of one
    # fact, and which one stays is a decision for the team (NotasGnothiHost/43).
    origin        = $Origen
    '//brand'     = 'LA MARCA. La institucion declara y el Host compone: ella pone el material, el decide el lugar, el tamano y el contraste.'
    brand         = [ordered]@{
        shortName = $m.Marca
        # SI EL MODULO NO TRAE LOGO PROPIO, NO SE DECLARA -- y no es una omision: sin
        # `brand.logo` el Host lo saca del `manifest.webmanifest` que la PWA ya trae en su
        # `web/`, con su `theme_color` y sus `icons`. Los dos caminos estan en uso a proposito
        # en los cuatro mocks: tres declaran el suyo en logos/, y dealership no, asi que sale
        # del webmanifest de su propia app. Un bundle 1.0 se pinta igual sin tocarlo.
        logo      = if ($traeLogo) { 'brand/logo.png' } else { '' }
        color     = $m.Color
        slogan    = $m.Slogan
        # THE BANNER AND THE TYPEFACE (2026-09-09) live in the institution's own wwwroot/brand/ and
        # get published with its app, so they land in web/brand/ of the bundle. The Host reads them
        # through the same customs as the logo: banner up to 200 KB and 1600 px; font woff2/woff/ttf
        # up to 160 KB, typed by its bytes. A file that does not pass leaves the card without it.
        # The typeface dresses ONLY the brand name; the rest of the card is Gnothi's Manrope. An
        # institution without one (Delfos) gets its name in Manrope ExtraBold, and declares nothing.
        banner    = 'web/brand/banner.jpg'
        font      = if ($m.Fuente) { [ordered]@{ family = $m.Fuente.family; file = $m.Fuente.file } } else { $null }
    }
    '//publisher' = 'DE DONDE VIENE Y QUE VERSION ES. El dominio es el unico campo CONTRASTABLE: Gnothi ya sabe de que host bajo el bundle y compara.'
    publisher     = [ordered]@{
        domain    = $m.Dominio
        legal     = $m.RazonSocial
        taxId     = $m.Cedula
        version   = if ($Version) { $Version } else { $m.Version }
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
if (-not $Origen) { $manifiesto.Remove('origin') }
$json = $manifiesto | ConvertTo-Json -Depth 8
[System.IO.File]::WriteAllText("$armado/manifest.json", $json, (New-Object System.Text.UTF8Encoding($false)))

# --- el zip ----------------------------------------------------------------------------
if (Test-Path $zip) { Remove-Item -Force $zip }
# Compress-Archive (PowerShell 5.1) only writes a destination that ends in .zip: it is written
# there and renamed. The container IS a zip; only the name says what is inside.
$zipTemporal = "$zip.zip"
if (Test-Path $zipTemporal) { Remove-Item -Force $zipTemporal }
Compress-Archive -Path "$armado/*" -DestinationPath $zipTemporal -CompressionLevel Optimal
Move-Item -Force $zipTemporal $zip

# --- la firma ----------------------------------------------------------------------------
# The Host signs, as the publisher-side helper it already is for --pack: the same code that
# verifies on the phone signs here, so the two cannot drift. The key pair lives next to the
# bundles (git-ignored) and is created the first time; the page publishes its public half in the
# well-known. Built into its own bin because the MCP servers of a Claude session hold the default
# one (see NotasGnothiHost); forward slashes because a trailing backslash inside quotes escapes
# the quote on Windows.
if (-not $Llave) { $Llave = "$Salida/.keys/$Modulo.key.json" }
$raiz = (Split-Path -Parent $PSScriptRoot) -replace '\\', '/'
# No build flags before the `--`: `dotnet run` forwards what it does not recognise to the app, and
# the Host took a stray --nologo for its modules directory and never signed (measured 2026-09-09).
dotnet run --project "$raiz/Gnothi.Host/Gnothi.Host.csproj" -p:BaseOutputPath="$raiz/bin/firmar/" -- --sign $zip $Llave
if ($LASTEXITCODE -ne 0) { throw "fallo la firma del bundle de $Modulo" }

if ($publicado -and (Test-Path $publicado)) { Remove-Item -Recurse -Force $publicado }

$mb = [math]::Round((Get-Item $zip).Length / 1MB, 1)
$n  = (Get-ChildItem -Recurse $armado -File).Count

Write-Host ""
Write-Host "bundle listo : $zip"
Write-Host "  archivos   : $n"
Write-Host "  tamano     : $mb MB"
Write-Host "  publicador : $Publicador$Recurso/$Id/events"
if ($Origen) { Write-Host "  origen     : $Origen" } else { Write-Host "  origen     : (no declarado: la hoja de Gnothi lo va a decir)" }
Write-Host "  llave      : $Llave"
Write-Host "  tools      : $(($m.Tools | ForEach-Object { $_.name }) -join ', ')"
Write-Host "  marca      : $($m.Marca) $(if ($traeLogo) { "con logo ($([math]::Round((Get-Item $logo).Length / 1KB, 1)) KB)" } else { 'SIN logo -- entra con el dibujo de su rubro' })"
