# Pruebas funcionales de OpenRIN

Programa de consola que recorre los casos de uso contra una base real (103 comprobaciones): idiomas con .resx (ensamblados satélite), excepciones de negocio, fábrica con reflexión, seguridad (login, bloqueo, recuperación, AES, PBKDF2), pacientes, historia clínica, RIN y criticidad, alertas, eventos adversos, agenda, seguimiento, reportes, exportación, control de cambios, integridad DVH/DVV con recuperación, y respaldos (COPY_ONLY, CHECKSUM, tabla Respaldos y respaldo automático diario).

## Preparación

1. Crear las bases OpenRIN_Services y OpenRIN_Negocio en SQL Server.
2. Cargar en OpenRIN_Services los scripts de Services/DAL/Tools, con sqlcmd -f 65001: script, security, permisos, localizacion, localizacion_limpiar y respaldos (los textos de los idiomas vienen de los .resx, no de un script).
3. Cargar Negocio.DAL/Tools/script.sql en OpenRIN_Negocio, con sqlcmd -I.
4. Copiar App.config.ejemplo como App.config y ajustar el servidor. El archivo trae las claves Repositorio.<Interfaz> que usa la fábrica.

## Ejecución

    dotnet run --project Pruebas

Las pruebas crean y borran sus propios datos de prueba (pacientes, usuarios de prueba y respaldos en una carpeta temporal). Conviene correrlas contra una base de desarrollo y, después, dejar de nuevo los datos de demostración con OpenRIN.Herramientas.exe sembrar.
