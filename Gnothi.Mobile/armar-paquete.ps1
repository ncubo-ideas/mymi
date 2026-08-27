# Arma el paquete de modulos que se le empuja al telefono.
#
# El APK viaja VACIO: no trae ningun modulo adentro, y eso es a proposito -- un modulo lo
# entrega una institucion, no nosotros. Este script prepara lo que se copia despues.
#
# Que produce, en .\paquete\ :
#
#   Modulos\<negocio>\bin\             el modulo publicado, con TODAS sus dependencias al lado
#   Modulos\<negocio>\manifest.json    el del repo, tal cual
#   Modulos\<negocio>\installation.json  reescrito con RUTAS DEL TELEFONO
#   journals\<negocio>\                el registro de la persona
#
# Las dependencias tienen que viajar AL LADO del dll porque en Android no existe
# AssemblyDependencyResolver -- depende de hostpolicy, que en movil no esta -- asi que
# InProcessShell las resuelve por vecindad. Por eso 'publish' y no 'build'.

param(
    # De donde salen los journals. Es lo unico que cambia de maquina en maquina.
    [string] $Journals = 'C:\Gnothi',
    [string] $Destino  = "$PSScriptRoot\paquete"
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent

# El id de la app. Si cambia en el csproj, cambia aca: las rutas del telefono lo llevan.
$app = 'com.ncubo.gnothi.host'
$enElTelefono = "/data/user/0/$app/files"

$negocios = @(
    @{ nombre = 'examenes'; csproj = 'Examenes\App\Examenes.Mcp\Examenes.Mcp.csproj';          dll = 'Examenes.Mcp.dll' }
    @{ nombre = 'facturas'; csproj = 'HolaMundo\Facturas\App\Facturas.Mcp\Facturas.Mcp.csproj'; dll = 'Facturas.Mcp.dll' }
    @{ nombre = 'compras';  csproj = 'Compras\App\Compras.Mcp\Compras.Mcp.csproj';              dll = 'Compras.Mcp.dll'  }
)

if (Test-Path $Destino) { Remove-Item -Recurse -Force $Destino }
New-Item -ItemType Directory -Force $Destino | Out-Null

foreach ($n in $negocios) {
    Write-Host "--- $($n.nombre)"

    $csproj = Join-Path $repo $n.csproj
    if (-not (Test-Path $csproj)) { throw "no encuentro $csproj" }

    $carpeta = Join-Path $Destino "Modulos\$($n.nombre)"
    New-Item -ItemType Directory -Force "$carpeta\bin" | Out-Null

    $salida = dotnet publish $csproj -c Debug -o "$carpeta\bin" -v q --nologo 2>&1
    if ($LASTEXITCODE -ne 0) {
        $salida | Write-Host

        # LA TRAMPA MAS COMUN, y no se ve en el mensaje de MSBuild: si tenes Claude Code o
        # Claude Desktop abiertos, ellos ya levantaron los servidores MCP declarados en
        # .mcp.json, y esos procesos TIENEN TOMADOS los dll que este publish quiere escribir.
        if ($salida -match 'MSB302[17]|being used by another process') {
            Write-Host ""
            Write-Host "El dll esta tomado por otro proceso. Casi siempre es un servidor MCP"
            Write-Host "que levanto Claude Code o Claude Desktop desde .mcp.json. Cerralos, o:"
            Write-Host ""
            Write-Host "  Get-CimInstance Win32_Process -Filter \"Name='dotnet.exe'\" |"
            Write-Host "    Where-Object { `$_.CommandLine -match 'Mcp.dll' } |"
            Write-Host "    ForEach-Object { Stop-Process -Id `$_.ProcessId -Force }"
        }

        throw "fallo el publish de $($n.nombre)"
    }

    # El manifiesto es de la institucion y NO se toca: es lo que ella declara.
    Copy-Item (Join-Path $repo "Modulos\$($n.nombre)\manifest.json") "$carpeta\manifest.json"

    # El installation.json SI se reescribe: es el archivo de la persona, y sus rutas son las
    # del aparato donde vive. La cascara es 'in-process' por obligacion -- en Android una app
    # con targetSdk >= 29 no puede exec() desde su directorio de datos.
    @"
{
  "//": "EL ARCHIVO DEL HOST. Rutas del telefono: el modulo vive en el area privada de la app.",
  "shell": {
    "type": "in-process",
    "command": "dotnet",
    "arguments": [ "$enElTelefono/Modulos/$($n.nombre)/bin/$($n.dll)" ]
  },
  "grantedFolder": "$enElTelefono/journals/$($n.nombre)",
  "enabled": true,
  "disabledTools": []
}
"@ | Set-Content "$carpeta\installation.json" -Encoding utf8

    # El journal, entero. Adentro trae una subcarpeta con el nombre del actor: es el layout
    # de Puppeteer y hay que respetarlo, porque grantedFolder apunta al nivel de afuera.
    $origen = Join-Path $Journals $n.nombre
    if (Test-Path $origen) {
        Copy-Item -Recurse $origen (Join-Path $Destino "journals\$($n.nombre)")
    } else {
        Write-Host "    OJO: no hay journal en $origen -- el modulo va a responder 'no hay datos'"
    }
}

$archivos = (Get-ChildItem -Recurse -File $Destino).Count
$megas    = [Math]::Round((Get-ChildItem -Recurse -File $Destino | Measure-Object Length -Sum).Sum / 1MB, 1)
Write-Host ""
Write-Host "Listo: $archivos archivos, $megas MB en $Destino"
Write-Host "Ahora empujalo con los dos 'adb push' del README."
