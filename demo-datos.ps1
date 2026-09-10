# El operador de datos de la demo: junta las piezas QUE YA EXISTEN para mover los journals.
#
# No reescribe nada: cada verbo delega en lo que hicieron los duenos de cada negocio --
# el mock del proveedor (docker compose), el InyectorDeEventosDemo (mete eventos nuevos EN
# EL MOCK), y la Consola con su verbo 'refrescar', que ES la app de la persona sin
# navegador: consigue su credencial sola y ESCRIBE el journal en C:\Gnothi.
#
#   .\demo-datos.ps1 arriba                        levanta los tres mocks
#   .\demo-datos.ps1 estado facturas               total y ultimo evento, leyendo el journal
#   .\demo-datos.ps1 inyectar facturas             una factura nueva EN EL MOCK
#   .\demo-datos.ps1 refrescar facturas            la "app de la persona" la trae al journal
#   .\demo-datos.ps1 movil [serial]                sincroniza los journals al telefono/emulador
#   .\demo-datos.ps1 ciclo facturas [serial]       todo lo anterior, de una
#   .\demo-datos.ps1 abajo                         apaga los mocks
#
# OJO, UN SOLO ESCRITOR A LA VEZ: 'refrescar' escribe el mismo journal que la app del
# navegador. Si la caratula wasm esta abierta sobre C:\Gnothi, cerrala primero -- dos
# escritores sobre un journal es exactamente lo que el modelo prohibe.

param(
    [Parameter(Position = 0)] [string] $Verbo = 'ayuda',
    [Parameter(Position = 1)] [string] $Negocio = '',
    [Parameter(Position = 2)] [string] $Dispositivo = '',
    [string] $Journals = 'C:\Gnothi'
)

$ErrorActionPreference = 'Continue'
$repo = $PSScriptRoot

# La tabla que une lo de todos. Puerto = el del mock de eventos (URL2); 'recurso' e 'id'
# son los argumentos que el refrescar de cada Consola espera; 'inyectar' es el comando del
# inyector de ese negocio ('aleatoria' en facturas genera una factura distinta cada vez).
$negocios = @{
    facturas = @{ compose = 'HolaMundo\Facturas'; consola = 'HolaMundo\Facturas\App\Facturas.Consola'
                  inyector = 'HolaMundo\Facturas\InyectorDeEventosDemo'
                  url = 'http://localhost:8081/'; recurso = 'clientes';   id = '1'; inyectar = 'aleatoria' }
    compras  = @{ compose = 'Compras'; consola = 'Compras\App\Compras.Consola'
                  inyector = 'Compras\InyectorDeEventosDemo'
                  url = 'http://localhost:8101/'; recurso = 'cliente';    id = '1'; inyectar = 'inyectar' }
    examenes = @{ compose = 'Examenes'; consola = 'Examenes\App\Examenes.Consola'
                  inyector = 'Examenes\InyectorDeEventosDemo'
                  url = 'http://localhost:8091/'; recurso = 'expediente'; id = '1'; inyectar = 'inyectar' }
}

# El stderr de docker y dotnet llega como ErrorRecord y PowerShell 5.1 lo pinta rojo sin
# serlo. Se desenvuelve, igual que en los otros scripts de este repo.
function Exe {
    & $args[0] $args[1..($args.Count - 1)] 2>&1 | ForEach-Object {
        if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.Exception.Message } else { "$_" }
    }
}

function Elegidos {
    if ($Negocio -and $negocios.ContainsKey($Negocio)) { return @($Negocio) }
    if ($Negocio) { throw "no conozco el negocio '$Negocio' (hay: $($negocios.Keys -join ', '))" }
    return @($negocios.Keys)
}

switch ($Verbo) {

    'arriba' {
        foreach ($n in Elegidos) {
            Write-Host "── $n"
            Exe docker compose -f (Join-Path $repo "$($negocios[$n].compose)\docker-compose.yml") up -d | Select-Object -Last 2
        }
    }

    'abajo' {
        foreach ($n in Elegidos) {
            Write-Host "── $n"
            Exe docker compose -f (Join-Path $repo "$($negocios[$n].compose)\docker-compose.yml") down | Select-Object -Last 1
        }
    }

    'estado' {
        foreach ($n in Elegidos) {
            Write-Host "── $n"
            Exe dotnet run --project (Join-Path $repo $negocios[$n].consola) -c Debug -v q -- estado (Join-Path $Journals $n) |
                Where-Object { $_ -match 'total|cantidad|evento|analitos|compras' } | Select-Object -First 5
        }
    }

    'inyectar' {
        foreach ($n in Elegidos) {
            Write-Host "── $n → el mock recibe un evento nuevo"
            $c = $negocios[$n]
            # --recurso e --id EXPLICITOS: el default del inyector es OTRO stream (loan/78341
            # en facturas) y el refresh lee el de la persona. Inyectar sin esto mete el evento
            # en una historia que nadie relee — costó dos inyecciones descubrirlo.
            Exe dotnet run --project (Join-Path $repo $c.inyector) -c Debug -v q -- $c.inyectar --url $c.url --recurso $c.recurso --id $c.id |
                Select-Object -Last 4
        }
    }

    'refrescar' {
        foreach ($n in Elegidos) {
            Write-Host "── $n → la app de la persona escribe el journal"
            $c = $negocios[$n]
            Exe dotnet run --project (Join-Path $repo $c.consola) -c Debug -v q -- refrescar (Join-Path $Journals $n) $c.url $c.recurso $c.id |
                Where-Object { $_ -match 'refresh|total|cantidad|evento|credencial' } | Select-Object -First 6
        }
    }

    'movil' {
        # $Negocio ocupa la posicion 1, asi que aca puede venir el serial ahi o en el 3ro.
        $serial = if ($Dispositivo) { $Dispositivo } elseif ($Negocio -and -not $negocios.ContainsKey($Negocio)) { $Negocio } else { '' }
        & (Join-Path $repo 'Gnothi.Mobile\actualizar-journals.ps1') -Journals $Journals -Dispositivo $serial
    }

    'ciclo' {
        if (-not $Negocio) { throw "ciclo pide un negocio: .\demo-datos.ps1 ciclo facturas [serial]" }
        & $PSCommandPath inyectar  $Negocio
        & $PSCommandPath refrescar $Negocio
        & $PSCommandPath movil     $Dispositivo
        Write-Host ""
        Write-Host "Ciclo completo: mock → journal de la compu → telefono. Pregunta de nuevo y el numero es otro."
    }

    default {
        Get-Content $PSCommandPath | Select-Object -Skip 2 -First 17 | ForEach-Object { $_.TrimStart('# ') }
    }
}
