@echo off
rem OpenRIN - lanzador del instalador (se ejecuta desde OpenRIN-Setup.exe)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Instalar.ps1" %*
