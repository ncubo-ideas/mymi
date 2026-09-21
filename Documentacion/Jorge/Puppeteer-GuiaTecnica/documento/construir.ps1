# Ensambla index.html a partir de plantilla.html + capitulos/*.html.
# Ejecutar tras editar cualquier capitulo:  ./construir.ps1
$ErrorActionPreference = 'Stop'
$enc  = New-Object System.Text.UTF8Encoding($false)
$root = $PSScriptRoot

$plantilla = [System.IO.File]::ReadAllText((Join-Path $root 'plantilla.html'))

$dir = Join-Path $root 'capitulos'
$archivos = Get-ChildItem -Path $dir -Filter '*.html' | Sort-Object Name

$piezas = foreach ($f in $archivos) {
    '  ' + ([System.IO.File]::ReadAllText($f.FullName)).TrimEnd()
}
$contenido = ($piezas -join "`r`n`r`n")

$salida = $plantilla.Replace('<!--CONTENIDO-->', $contenido)
[System.IO.File]::WriteAllText((Join-Path $root 'index.html'), $salida, $enc)
Write-Host ("index.html generado desde {0} capitulos." -f $archivos.Count)
