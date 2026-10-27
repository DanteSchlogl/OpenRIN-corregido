param(
    [string]$Instancia = "localhost",
    [string]$SqlCmd = "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE",
    [string]$LogDir = "C:\OpenRIN\Backups\logs"
)

# Respaldo nocturno de OpenRIN: ejecuta 01_respaldo_nocturno.sql con autenticacion de
# Windows (-E, sin contrasenas) y registra la salida en un log con marca de tiempo.
# Devuelve el codigo de salida de sqlcmd (0 = exito) para el historial del Programador
# de tareas de Windows.

$ErrorActionPreference = 'Stop'

# Asegurar la carpeta raiz de respaldos (donde escribe SQL Server) y la de logs: el
# mecanismo queda autosuficiente incluso en un equipo donde C:\OpenRIN no exista aun.
$RaizBackups = Split-Path -Parent $LogDir
if (-not (Test-Path -LiteralPath $RaizBackups)) {
    New-Item -ItemType Directory -Path $RaizBackups -Force | Out-Null
}
if (-not (Test-Path -LiteralPath $LogDir)) {
    New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
}

$scriptSql = Join-Path $PSScriptRoot '01_respaldo_nocturno.sql'
$log = Join-Path $LogDir ('respaldo_' + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.log')

& $SqlCmd -S $Instancia -E -b -i $scriptSql -o $log
$codigo = $LASTEXITCODE
Add-Content -LiteralPath $log -Value ('[' + (Get-Date -Format 's') + '] sqlcmd finalizo con codigo ' + $codigo)
exit $codigo
