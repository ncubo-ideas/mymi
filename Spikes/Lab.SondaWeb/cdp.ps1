# Cliente CDP minimo, para hablarle a la pagina que corre DENTRO de la WebAPK.
#
# POR QUE HACE FALTA, y no es comodidad:
#
#   1. Desde JavaScript, un bloqueo por contenido mixto y un "connection refused" entregan
#      EXACTAMENTE el mismo TypeError. El motivo solo existe en la consola del navegador, y
#      Chrome de canal estable NO manda la consola de la pagina a logcat -- medido: cero
#      lineas con `adb logcat -s chromium`. CDP es el unico lugar donde ese texto aparece.
#
#   2. El service worker de la PWA contesta TODA navegacion de su scope con su index.html
#      cacheado, salvo que la URL este en el manifiesto de assets. Abrir sonda.html adentro
#      de la WebAPK devuelve la app, no la sonda -- medido: el titulo de la pagina era
#      "Mis examenes", con url sonda.html. Con CDP se evalua en la pagina que YA esta abierta
#      y el service worker no interviene.
#
# Uso:
#   adb forward tcp:9222 localabstract:chrome_devtools_remote
#   powershell -File cdp.ps1 -Ws "ws://127.0.0.1:9222/devtools/page/0" -Js "1+1"
#   powershell -File cdp.ps1 -Ws "..." -JsFile sonda-inyectada.js -EsperaMs 12000

param(
  [Parameter(Mandatory = $true)][string] $Ws,
  [string] $Js = "",
  [string] $JsFile = "",
  [int] $EsperaMs = 8000
)

$ErrorActionPreference = 'Stop'
if ($JsFile) { $Js = Get-Content -Raw -Encoding UTF8 $JsFile }
if (-not $Js) { throw "hace falta -Js o -JsFile" }

$sock = New-Object System.Net.WebSockets.ClientWebSocket
[void]$sock.ConnectAsync([Uri]$Ws, [Threading.CancellationToken]::None).GetAwaiter().GetResult()

function Enviar($id, $metodo, $params) {
  $msg = @{ id = $id; method = $metodo }
  if ($params) { $msg.params = $params }
  $bytes = [Text.Encoding]::UTF8.GetBytes((ConvertTo-Json $msg -Depth 8 -Compress))
  $seg = New-Object 'System.ArraySegment[byte]' -ArgumentList @(,$bytes)
  [void]$sock.SendAsync($seg, 'Text', $true, [Threading.CancellationToken]::None).GetAwaiter().GetResult()
}

function Recibir($timeoutMs) {
  $buf = New-Object byte[] 262144
  $seg = New-Object 'System.ArraySegment[byte]' -ArgumentList @(,$buf)
  $sb = New-Object Text.StringBuilder
  $cts = New-Object Threading.CancellationTokenSource
  $cts.CancelAfter($timeoutMs)
  try {
    do {
      $r = $sock.ReceiveAsync($seg, $cts.Token).GetAwaiter().GetResult()
      [void]$sb.Append([Text.Encoding]::UTF8.GetString($buf, 0, $r.Count))
    } while (-not $r.EndOfMessage)
  } catch { return $null }
  return $sb.ToString()
}

# Runtime para evaluar; Log y Console porque ahi viven los motivos de los bloqueos.
Enviar 1 'Runtime.enable' $null
Enviar 2 'Log.enable' $null
Enviar 3 'Console.enable' $null
Enviar 10 'Runtime.evaluate' @{ expression = $Js; awaitPromise = $true; returnByValue = $true; userGesture = $true }

$hasta = [DateTime]::UtcNow.AddMilliseconds($EsperaMs)
$eventos = @()
$resultado = $null

while ([DateTime]::UtcNow -lt $hasta) {
  $crudo = Recibir 1500
  if (-not $crudo) { continue }
  try { $m = $crudo | ConvertFrom-Json } catch { continue }

  if ($m.id -eq 10) {
    if ($m.result.exceptionDetails) { $resultado = "EXCEPCION: " + $m.result.exceptionDetails.text + " " + $m.result.exceptionDetails.exception.description }
    else { $resultado = $m.result.result.value }
    continue
  }
  if ($m.method -eq 'Log.entryAdded') {
    $e = $m.params.entry
    $eventos += "[$($e.level)/$($e.source)] $($e.text)" + $(if ($e.url) { "  <$($e.url)>" })
  }
  if ($m.method -eq 'Runtime.consoleAPICalled') {
    $eventos += "[console.$($m.params.type)] " + (($m.params.args | ForEach-Object { $_.value }) -join ' ')
  }
}

Write-Output "===== RESULTADO ====="
Write-Output $resultado
Write-Output ""
Write-Output "===== CONSOLA Y SEGURIDAD (lo que JS no puede ver) ====="
if ($eventos.Count -eq 0) { Write-Output "(ningun evento)" } else { $eventos | ForEach-Object { Write-Output $_ } }

try { $sock.CloseAsync('NormalClosure', 'fin', [Threading.CancellationToken]::None).GetAwaiter().GetResult() } catch {}
