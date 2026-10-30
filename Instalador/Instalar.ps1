<#
 OpenRIN - Instalador (A01)
 Instala la aplicación, el motor de base de datos (SQL Server Express si no hay ninguna instancia),
 las bases OpenRIN_Services y OpenRIN_Negocio con su esquema y los datos de demostración.

 Uso:  Setup.exe              (asistente con mínima interacción)
       Setup.exe /S           (silencioso: sin preguntas)
 Parámetros del script: -Servidor ".\SQLEXPRESS" -Destino "C:\Program Files\OpenRIN" -ClaveDemo "..." -Silencioso
#>
param(
    [string]$Servidor = "",
    [string]$Destino = (Join-Path $env:ProgramFiles "OpenRIN"),
    [string]$ClaveDemo = "Demo2026!",
    [switch]$Silencioso
)
$ErrorActionPreference = "Stop"
$aqui = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not (Test-Path (Join-Path $aqui "app.zip"))) {
    Write-Host "Este script es el instalador sin empaquetar: faltan app.zip y los scripts SQL." -ForegroundColor Red
    Write-Host "Use OpenRIN-Setup.exe (carpeta 7 - Software) o genere el paquete con Construir_Setup.ps1." -ForegroundColor Yellow
    exit 1
}
$log = Join-Path $env:TEMP "OpenRIN_instalacion.log"

# ---- elevación -------------------------------------------------------------------------------
$esAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $esAdmin) {
    $argumentos = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "`"$($MyInvocation.MyCommand.Path)`"")
    if ($Servidor) { $argumentos += @("-Servidor", "`"$Servidor`"") }
    $argumentos += @("-Destino", "`"$Destino`"", "-ClaveDemo", "`"$ClaveDemo`"")
    if ($Silencioso) { $argumentos += "-Silencioso" }
    Start-Process powershell -Verb RunAs -ArgumentList $argumentos -Wait
    exit
}
Start-Transcript -Path $log -Force | Out-Null

function Paso($n, $texto) { Write-Host ""; Write-Host "[$n/8] $texto" -ForegroundColor Cyan }
function Fallar($mensaje) {
    Write-Host ""; Write-Host "ERROR: $mensaje" -ForegroundColor Red
    Write-Host "Detalle en: $log"
    if (-not $Silencioso) { [void](Read-Host "Presione Enter para salir") }
    Stop-Transcript | Out-Null
    exit 1
}

Write-Host "=============================================" -ForegroundColor Green
Write-Host " OpenRIN 1.0 - Instalación" -ForegroundColor Green
Write-Host " Plataforma de alertas y monitoreo ACO" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green

# ---- 1. requisitos -----------------------------------------------------------------------------
Paso 1 "Verificando requisitos"
if ([Environment]::OSVersion.Version.Major -lt 10) { Fallar "Se requiere Windows 10 u 11." }
$libre = (Get-PSDrive -Name ($env:SystemDrive.TrimEnd(':'))).Free / 1MB
if ($libre -lt 1500) { Fallar "Espacio libre insuficiente en $env:SystemDrive ($([int]$libre) MB). Se necesitan 1500 MB." }
Write-Host "  Windows y espacio en disco: correctos"

# ---- 2. motor de base de datos -----------------------------------------------------------------
Paso 2 "Buscando el motor de base de datos (SQL Server 2019 o superior)"
function Obtener-Instancias {
    $clave = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL"
    if (-not (Test-Path $clave)) { return @() }
    $props = Get-ItemProperty $clave
    $props.PSObject.Properties | Where-Object { $_.Name -notlike "PS*" } | ForEach-Object {
        if ($_.Name -eq "MSSQLSERVER") { "." } else { ".\" + $_.Name }
    }
}
function Probar-Conexion($srv) {
    try {
        $c = New-Object System.Data.SqlClient.SqlConnection "Server=$srv;Database=master;Integrated Security=True;Connect Timeout=8"
        $c.Open()
        $cmd = $c.CreateCommand(); $cmd.CommandText = "SELECT CAST(SERVERPROPERTY('ProductMajorVersion') AS int), IS_SRVROLEMEMBER('sysadmin')"
        $r = $cmd.ExecuteReader(); [void]$r.Read(); $version = $r.GetInt32(0); $esSysadmin = $r.GetInt32(1); $r.Close(); $c.Close()
        return [pscustomobject]@{ Ok = $true; Version = $version; Sysadmin = ($esSysadmin -eq 1) }
    } catch { return [pscustomobject]@{ Ok = $false; Version = 0; Sysadmin = $false; Error = $_.Exception.Message } }
}
if (-not $Servidor) {
    foreach ($i in Obtener-Instancias) {
        $p = Probar-Conexion $i
        if ($p.Ok -and $p.Version -ge 15 -and $p.Sysadmin) { $Servidor = $i; break }
    }
}
if (-not $Servidor) {
    Write-Host "  No hay una instancia de SQL Server utilizable. Se instalará SQL Server 2022 Express."
    $winget = Get-Command winget -ErrorAction SilentlyContinue
    if (-not $winget) { Fallar "No se encontró 'winget'. Instale SQL Server Express (https://aka.ms/sqlexpress) y vuelva a ejecutar este instalador." }
    & winget install -e --id Microsoft.SQLServer.2022.Express --silent --accept-package-agreements --accept-source-agreements `
        --override "/QS /IACCEPTSQLSERVERLICENSETERMS /ACTION=Install /FEATURES=SQLEngine /INSTANCENAME=SQLEXPRESS /SQLSYSADMINACCOUNTS=BUILTIN\Administrators /TCPENABLED=0"
    foreach ($i in Obtener-Instancias) {
        $p = Probar-Conexion $i
        if ($p.Ok -and $p.Sysadmin) { $Servidor = $i; break }
    }
    if (-not $Servidor) { Fallar "No se pudo instalar o conectar con SQL Server Express. Consulte el Manual de Instalación." }
}
$prueba = Probar-Conexion $Servidor
if (-not $prueba.Ok) { Fallar "No se puede conectar con '$Servidor': $($prueba.Error)" }
if (-not $prueba.Sysadmin) { Fallar "El usuario actual no es administrador (sysadmin) de '$Servidor'." }
Write-Host "  Instancia: $Servidor (SQL Server $($prueba.Version))"

# ---- 3. bases de datos -------------------------------------------------------------------------
Paso 3 "Creando las bases de datos OpenRIN_Services y OpenRIN_Negocio"
function Ejecutar-Script($baseDatos, $archivo) {
    $texto = [IO.File]::ReadAllText($archivo, [Text.Encoding]::UTF8)
    $lotes = [regex]::Split($texto, "(?im)^\s*GO\s*$") | Where-Object { $_.Trim() -ne "" }
    $c = New-Object System.Data.SqlClient.SqlConnection "Server=$Servidor;Database=$baseDatos;Integrated Security=True;Connect Timeout=15"
    $c.Open()
    foreach ($l in $lotes) { $cmd = $c.CreateCommand(); $cmd.CommandText = $l; $cmd.CommandTimeout = 300; [void]$cmd.ExecuteNonQuery() }
    $c.Close()
}
$cm = New-Object System.Data.SqlClient.SqlConnection "Server=$Servidor;Database=master;Integrated Security=True"
$cm.Open()
foreach ($bd in @("OpenRIN_Services", "OpenRIN_Negocio")) {
    $cmd = $cm.CreateCommand(); $cmd.CommandText = "IF DB_ID('$bd') IS NULL CREATE DATABASE [$bd]"; [void]$cmd.ExecuteNonQuery()
}
$cm.Close()
foreach ($s in @("services_01_bitacora.sql", "services_02_seguridad.sql", "services_03_permisos.sql", "services_04_localizacion.sql", "services_05_localizacion_seed.sql")) {
    Write-Host "  OpenRIN_Services <- $s"; Ejecutar-Script "OpenRIN_Services" (Join-Path $aqui $s)
}
Write-Host "  OpenRIN_Negocio  <- negocio_esquema.sql"; Ejecutar-Script "OpenRIN_Negocio" (Join-Path $aqui "negocio_esquema.sql")

# La tarea de respaldo corre como SYSTEM: se le da acceso a las bases de OpenRIN (db_owner) para poder respaldar y registrar la bitácora.
$cm = New-Object System.Data.SqlClient.SqlConnection "Server=$Servidor;Database=master;Integrated Security=True"
$cm.Open()
$cmd = $cm.CreateCommand()
$cmd.CommandText = "IF SUSER_ID(N'NT AUTHORITY\SYSTEM') IS NULL CREATE LOGIN [NT AUTHORITY\SYSTEM] FROM WINDOWS; ALTER SERVER ROLE dbcreator ADD MEMBER [NT AUTHORITY\SYSTEM];"
[void]$cmd.ExecuteNonQuery()
$cm.Close()
foreach ($bd in @("OpenRIN_Services", "OpenRIN_Negocio")) {
    $c = New-Object System.Data.SqlClient.SqlConnection "Server=$Servidor;Database=$bd;Integrated Security=True"
    $c.Open(); $cmd = $c.CreateCommand()
    $cmd.CommandText = "IF USER_ID(N'NT AUTHORITY\SYSTEM') IS NULL CREATE USER [NT AUTHORITY\SYSTEM] FOR LOGIN [NT AUTHORITY\SYSTEM]; ALTER ROLE db_owner ADD MEMBER [NT AUTHORITY\SYSTEM];"
    [void]$cmd.ExecuteNonQuery(); $c.Close()
}
Write-Host "  Permisos de la cuenta del respaldo automático: correctos"

# ---- 4. aplicación -----------------------------------------------------------------------------
Paso 4 "Instalando la aplicación en $Destino"
$app = Join-Path $aqui "app.zip"
if (-not (Test-Path $app)) { Fallar "Falta app.zip en el paquete de instalación." }
if (Test-Path $Destino) { Get-Process -Name "Negocio.UI" -ErrorAction SilentlyContinue | Stop-Process -Force; Remove-Item -Recurse -Force $Destino }
Expand-Archive -Path $app -DestinationPath $Destino -Force
$datos = Join-Path $env:ProgramData "OpenRIN"
$respaldos = Join-Path $env:SystemDrive "OpenRIN\Backups"
New-Item -ItemType Directory -Force -Path $datos, $respaldos | Out-Null
& icacls $datos /grant "*S-1-5-32-545:(OI)(CI)M" /T /Q | Out-Null
$nombreInstancia = if ($Servidor -eq ".") { "MSSQLSERVER" } else { "MSSQL`$" + $Servidor.Split("\")[1] }
& icacls $respaldos /grant "NT SERVICE\${nombreInstancia}:(OI)(CI)M" /Q | Out-Null

$config = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <connectionStrings>
    <add name="ServicesDB" connectionString="Server=$Servidor;Database=OpenRIN_Services;Integrated Security=True;TrustServerCertificate=True;" providerName="Microsoft.Data.SqlClient" />
    <add name="NegocioDB" connectionString="Server=$Servidor;Database=OpenRIN_Negocio;Integrated Security=True;TrustServerCertificate=True;" providerName="Microsoft.Data.SqlClient" />
    <add name="BackupString" connectionString="Server=$Servidor;Database=master;Integrated Security=True;TrustServerCertificate=True;" providerName="Microsoft.Data.SqlClient" />
  </connectionStrings>
  <appSettings>
    <add key="MinimalLogLevel" value="Info" />
    <add key="BackupFolder" value="$respaldos" />
  </appSettings>
</configuration>
"@
foreach ($exe in @("Negocio.UI", "OpenRIN.Herramientas")) {
    [IO.File]::WriteAllText((Join-Path $Destino "$exe.dll.config"), $config, (New-Object Text.UTF8Encoding($false)))
}
Write-Host "  Configuración escrita (autenticación integrada de Windows, sin contraseñas)"

# ---- 5. datos de demostración ------------------------------------------------------------------
Paso 5 "Cargando usuarios y datos de demostración"
$herr = Join-Path $Destino "OpenRIN.Herramientas.exe"
& $herr sembrar $ClaveDemo | Out-Host
if ($LASTEXITCODE -ne 0) { Fallar "La carga de datos de demostración terminó con error ($LASTEXITCODE)." }

# ---- 6. accesos directos -----------------------------------------------------------------------
Paso 6 "Creando accesos directos"
$sh = New-Object -ComObject WScript.Shell
foreach ($carpeta in @([Environment]::GetFolderPath("CommonDesktopDirectory"), (Join-Path ([Environment]::GetFolderPath("CommonPrograms")) "OpenRIN"))) {
    New-Item -ItemType Directory -Force -Path $carpeta | Out-Null
    $l = $sh.CreateShortcut((Join-Path $carpeta "OpenRIN.lnk"))
    $l.TargetPath = Join-Path $Destino "Negocio.UI.exe"; $l.WorkingDirectory = $datos
    $l.IconLocation = (Join-Path $Destino "Negocio.UI.exe") + ",0"; $l.Description = "OpenRIN - Plataforma de alertas y monitoreo ACO"; $l.Save()
}
$u = $sh.CreateShortcut((Join-Path (Join-Path ([Environment]::GetFolderPath("CommonPrograms")) "OpenRIN") "Desinstalar OpenRIN.lnk"))
$u.TargetPath = "powershell.exe"; $u.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$Destino\Desinstalar.ps1`""; $u.Save()

# ---- 7. respaldo automático --------------------------------------------------------------------
Paso 7 "Programando el respaldo diario (03:00)"
$accion = New-ScheduledTaskAction -Execute $herr -Argument "respaldo" -WorkingDirectory $datos
$disparador = New-ScheduledTaskTrigger -Daily -At 3:00
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
Register-ScheduledTask -TaskName "OpenRIN - Respaldo Nocturno" -Action $accion -Trigger $disparador -Principal $principal -Force | Out-Null
Write-Host "  Tarea 'OpenRIN - Respaldo Nocturno' registrada (carpeta de respaldos: $respaldos)"

# ---- 8. registro de desinstalación -------------------------------------------------------------
Paso 8 "Registrando la desinstalación"
Copy-Item (Join-Path $aqui "Desinstalar.ps1") (Join-Path $Destino "Desinstalar.ps1") -Force
$k = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\OpenRIN"
New-Item -Path $k -Force | Out-Null
Set-ItemProperty $k DisplayName "OpenRIN 1.0"
Set-ItemProperty $k DisplayVersion "1.0"
Set-ItemProperty $k Publisher "Muñiz Cortés, Carlos Santiago - UAI"
Set-ItemProperty $k InstallLocation $Destino
Set-ItemProperty $k UninstallString "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$Destino\Desinstalar.ps1`""

Write-Host ""
Write-Host "Instalación finalizada." -ForegroundColor Green
Write-Host "  Aplicación   : $Destino"
Write-Host "  Base de datos: $Servidor  (OpenRIN_Services, OpenRIN_Negocio)"
Write-Host "  Usuarios de demostración: sysadmin, medico, recepcion  (contraseña indicada en el Manual de Instalación)"
Write-Host "  Registro     : $log"
Stop-Transcript | Out-Null
if (-not $Silencioso) { [void](Read-Host "Presione Enter para cerrar") }
