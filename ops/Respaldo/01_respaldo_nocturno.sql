-- OpenRIN — Respaldo nocturno (FULL) de ambas bases + verificación de integridad del respaldo.
-- Ejecución:  sqlcmd -S localhost -E -b -i 01_respaldo_nocturno.sql
-- Requiere:   permiso de respaldo sobre ambas bases (db_backupoperator o superior) y que la
--             carpeta de destino exista (C:\OpenRIN\Backups). La verificación (RESTORE
--             VERIFYONLY ... WITH CHECKSUM) confirma que el archivo es restaurable.
SET NOCOUNT ON;

DECLARE @sello nvarchar(20) = CONVERT(nvarchar(8), GETDATE(), 112) + N'_' + REPLACE(CONVERT(nvarchar(8), GETDATE(), 108), N':', N'');
DECLARE @rutaServices nvarchar(400) = N'C:\OpenRIN\Backups\OpenRIN_Services_' + @sello + N'_FULL.bak';
DECLARE @rutaNegocio  nvarchar(400) = N'C:\OpenRIN\Backups\OpenRIN_Negocio_'  + @sello + N'_FULL.bak';

PRINT N'Respaldo nocturno OpenRIN — ' + CONVERT(nvarchar(19), GETDATE(), 120);

BACKUP DATABASE [OpenRIN_Services] TO DISK = @rutaServices WITH INIT, CHECKSUM, NAME = N'OpenRIN_Services - Respaldo nocturno';
BACKUP DATABASE [OpenRIN_Negocio]  TO DISK = @rutaNegocio  WITH INIT, CHECKSUM, NAME = N'OpenRIN_Negocio - Respaldo nocturno';

RESTORE VERIFYONLY FROM DISK = @rutaServices WITH CHECKSUM;
RESTORE VERIFYONLY FROM DISK = @rutaNegocio  WITH CHECKSUM;

PRINT N'Respaldo nocturno completado y verificado: ' + @sello;
