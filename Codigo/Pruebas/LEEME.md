# Pruebas funcionales de OpenRIN

Programa de consola que recorre los casos de uso contra una base real (62 comprobaciones): idiomas, seguridad (login, bloqueo, recuperación, AES, PBKDF2), pacientes, historia clínica, RIN y criticidad, alertas, eventos adversos, agenda, seguimiento, reportes, exportación, control de cambios, integridad DVH/DVV y respaldo.

## Preparación

1. Crear las bases OpenRIN_Services y OpenRIN_Negocio en SQL Server.
2. Cargar los scripts de Services/DAL/Tools (script, security, permisos, localizacion, localizacion_seed) en OpenRIN_Services, con sqlcmd -f 65001.
3. Cargar Negocio.DAL/Tools/script.sql en OpenRIN_Negocio, con sqlcmd -I.
4. Copiar App.config.ejemplo como App.config y ajustar el servidor.

## Ejecución

    dotnet run --project Pruebas

Crea (o reinicia) el usuario sysadmin con una contraseña aleatoria que guarda en credenciales_prueba.txt. No lo versionar.
