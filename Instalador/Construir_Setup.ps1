<#
 OpenRIN - Construcción del instalador (Setup.exe).
 Publica la aplicación y las herramientas con el runtime de .NET incluido, arma app.zip,
 copia los scripts SQL y empaqueta todo en un único Setup.exe con IExpress (incluido en Windows).
 Requiere el SDK de .NET 8 o superior.   Uso:  powershell -File Construir_Setup.ps1 [-Salida <carpeta>]
#>
param([string]$Salida = (Join-Path $PSScriptRoot "Salida"))
$ErrorActionPreference = "Stop"
$raiz = Split-Path -Parent $PSScriptRoot
$codigo = Join-Path $raiz "Codigo"
$trabajo = Join-Path $env:TEMP "OpenRIN_build"
if (Test-Path $trabajo) { Remove-Item -Recurse -Force $trabajo }
New-Item -ItemType Directory -Force -Path $trabajo, $Salida | Out-Null

Write-Host "1/4 Publicando la aplicación y las herramientas"
$pub = Join-Path $trabajo "publish"
dotnet publish (Join-Path $codigo "Negocio.UI\Negocio.UI.csproj") -c Release -r win-x64 --self-contained true -o $pub | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Falló la publicación de Negocio.UI" }
dotnet publish (Join-Path $codigo "Herramientas\Herramientas.csproj") -c Release -r win-x64 --self-contained true -o $pub | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Falló la publicación de Herramientas" }

# Ayuda en línea (D02): se copia junto a la aplicación para que el botón Ayuda y F1 la encuentren.
$ayuda = Join-Path $raiz "Documentacion\Manuales\OpenRIN-Ayuda.chm"
if (Test-Path $ayuda) { Copy-Item $ayuda $pub -Force } else { Write-Warning "No se encontró OpenRIN-Ayuda.chm: el botón Ayuda mostrará un aviso." }

Write-Host "2/4 Armando el paquete"
$paq = Join-Path $trabajo "paquete"
New-Item -ItemType Directory -Force -Path $paq | Out-Null
Compress-Archive -Path (Join-Path $pub "*") -DestinationPath (Join-Path $paq "app.zip") -CompressionLevel Optimal
$sql = Join-Path $codigo "Services\DAL\Tools"
Copy-Item (Join-Path $sql "script.sql")             (Join-Path $paq "services_01_bitacora.sql")
Copy-Item (Join-Path $sql "security.sql")           (Join-Path $paq "services_02_seguridad.sql")
Copy-Item (Join-Path $sql "permisos.sql")           (Join-Path $paq "services_03_permisos.sql")
Copy-Item (Join-Path $sql "localizacion.sql")       (Join-Path $paq "services_04_localizacion.sql")
Copy-Item (Join-Path $sql "localizacion_seed.sql")  (Join-Path $paq "services_05_localizacion_seed.sql")
Copy-Item (Join-Path $codigo "Negocio.DAL\Tools\script.sql") (Join-Path $paq "negocio_esquema.sql")
# Los scripts de PowerShell se guardan con BOM UTF-8 para que Windows PowerShell 5.1 lea bien los acentos.
foreach ($f in "Instalar.ps1", "Desinstalar.ps1") {
    $t = [IO.File]::ReadAllText((Join-Path $PSScriptRoot $f), [Text.Encoding]::UTF8)
    [IO.File]::WriteAllText((Join-Path $paq $f), $t, (New-Object Text.UTF8Encoding($true)))
}
Copy-Item (Join-Path $PSScriptRoot "install.cmd") (Join-Path $paq "install.cmd")

Write-Host "3/4 Generando Setup.exe con IExpress"
$destinoExe = Join-Path $Salida "OpenRIN-Setup.exe"
if (Test-Path $destinoExe) { Remove-Item $destinoExe -Force }
$archivos = Get-ChildItem $paq -File | Select-Object -ExpandProperty Name
$strings = ($archivos | ForEach-Object -Begin { $i = 0 } -Process { "FILE$i=`"$_`""; $i++ }) -join "`r`n"
$lista = ($archivos | ForEach-Object -Begin { $i = 0 } -Process { "%FILE$i%="; $i++ }) -join "`r`n"
$sed = @"
[Version]
Class=IEXPRESS
SEDVersion=3
[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=1
HideExtractAnimation=0
UseLongFileName=1
InsideCompressed=0
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=
DisplayLicense=
FinishMessage=
TargetName=$destinoExe
FriendlyName=OpenRIN 1.0 - Instalador
AppLaunched=cmd.exe /c install.cmd
PostInstallCmd=<None>
AdminQuietInstCmd=
UserQuietInstCmd=
SourceFiles=SourceFiles
[Strings]
$strings
[SourceFiles]
SourceFiles0=$paq\
[SourceFiles0]
$lista
"@
$sedPath = Join-Path $trabajo "openrin.sed"
[IO.File]::WriteAllText($sedPath, $sed, [Text.Encoding]::ASCII)
& "$env:SystemRoot\System32\iexpress.exe" /N /Q $sedPath | Out-Null
for ($i = 0; $i -lt 120 -and -not (Test-Path $destinoExe); $i++) { Start-Sleep -Seconds 2 }
if (-not (Test-Path $destinoExe)) { throw "IExpress no generó el instalador." }

Write-Host "4/4 Listo: $destinoExe ($([int]((Get-Item $destinoExe).Length / 1MB)) MB)"
