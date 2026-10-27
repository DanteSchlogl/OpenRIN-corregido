param(
    [string]$NombreTarea = 'OpenRIN - Respaldo Nocturno',
    [string]$Hora = '03:00'
)

# Registra (o re-registra: es idempotente) la tarea diaria del respaldo nocturno en el
# Programador de tareas de Windows. Corre como el usuario actual con token interactivo
# (sin contrasenas): se ejecuta cuando el usuario tiene sesion iniciada, aunque este
# bloqueada. En produccion, evaluar una cuenta de servicio dedicada (ver LEEME.md).

$ErrorActionPreference = 'Stop'

$wrapper = Join-Path $PSScriptRoot 'ejecutar_respaldo.ps1'
if (-not (Test-Path -LiteralPath $wrapper)) {
    throw ('No se encontro el wrapper del respaldo: ' + $wrapper)
}

$accion = 'powershell.exe -NoProfile -ExecutionPolicy Bypass -File "' + $wrapper + '"'
schtasks.exe /Create /TN $NombreTarea /TR $accion /SC DAILY /ST $Hora /RU $env:USERNAME /F
if ($LASTEXITCODE -ne 0) {
    throw ('schtasks /Create fallo con codigo ' + $LASTEXITCODE)
}

Write-Output ('Tarea registrada: "' + $NombreTarea + '" - diaria a las ' + $Hora + ' como ' + $env:USERNAME + '.')
schtasks.exe /Query /TN $NombreTarea
