<# OpenRIN - Desinstalador. Quita la aplicación, los accesos directos y la tarea de respaldo.
   Las bases de datos y los respaldos NO se borran, salvo que se indique -QuitarBases. #>
param([switch]$QuitarBases, [switch]$Silencioso)
$ErrorActionPreference = "Stop"
$esAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $esAdmin) { Start-Process powershell -Verb RunAs -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$($MyInvocation.MyCommand.Path)`"" -Wait; exit }
$destino = Split-Path -Parent $MyInvocation.MyCommand.Path
Get-Process -Name "Negocio.UI" -ErrorAction SilentlyContinue | Stop-Process -Force
Unregister-ScheduledTask -TaskName "OpenRIN - Respaldo Nocturno" -Confirm:$false -ErrorAction SilentlyContinue
Remove-Item (Join-Path ([Environment]::GetFolderPath("CommonDesktopDirectory")) "OpenRIN.lnk") -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path ([Environment]::GetFolderPath("CommonPrograms")) "OpenRIN") -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\OpenRIN" -Recurse -Force -ErrorAction SilentlyContinue
if ($QuitarBases) {
    $cfg = [IO.File]::ReadAllText((Join-Path $destino "Negocio.UI.dll.config"))
    if ($cfg -match "Server=([^;]+);Database=OpenRIN_Negocio") {
        $c = New-Object System.Data.SqlClient.SqlConnection "Server=$($Matches[1]);Database=master;Integrated Security=True"; $c.Open()
        foreach ($bd in "OpenRIN_Negocio", "OpenRIN_Services") {
            $m = $c.CreateCommand()
            $m.CommandText = "IF DB_ID('$bd') IS NOT NULL BEGIN ALTER DATABASE [$bd] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$bd]; END"
            [void]$m.ExecuteNonQuery()
        }
        $c.Close()
    }
}
# El script se ejecuta desde la carpeta de instalación: se borra al finalizar.
Start-Process cmd -ArgumentList "/c timeout /t 2 >nul & rmdir /s /q `"$destino`"" -WindowStyle Hidden
$estado = if ($QuitarBases) { "fueron eliminadas" } else { "se conservan (use -QuitarBases para eliminarlas)" }
Write-Host "OpenRIN se desinstaló. Las bases de datos $estado."
if (-not $Silencioso) { [void](Read-Host "Presione Enter para cerrar") }
