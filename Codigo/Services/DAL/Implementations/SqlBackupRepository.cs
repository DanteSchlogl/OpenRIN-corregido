using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Services.DAL.Interfaces;
using Services.DAL.Tools;
using Services.DomainModel;
using Services.DomainModel.Exceptions;

namespace Services.DAL.Implementations
{
    /// <summary>
    /// Operaciones de respaldo y restauración sobre SQL Server (REQ-ARQ-005).
    /// Usa la conexión "BackupString" (credenciales con permisos de administrador, exigidas por el ERS)
    /// y encapsula los comandos SQL nativos de BACKUP/RESTORE.
    /// </summary>
    internal class SqlBackupRepository : IBackupRepository
    {
        private const string Conexion = "BackupString";

        /// <summary>Error 3201: «No se puede abrir el dispositivo de copia de seguridad» (carpeta sin permiso o inexistente para el servicio).</summary>
        private const int ErrorDispositivoDeRespaldo = 3201;

        /// <inheritdoc />
        public double ObtenerTamanoBaseMb(string nombreBase)
        {
            const string sql = @"
                SELECT ISNULL(SUM(size), 0) * 8.0 / 1024
                FROM sys.master_files
                WHERE database_id = DB_ID(@NombreBase);";

            object? valor = SqlHelper.EjecutarEscalar(
                sql, CommandType.Text, Conexion, new SqlParameter("@NombreBase", nombreBase));
            return valor == null || valor == DBNull.Value ? 0 : Convert.ToDouble(valor);
        }

        /// <inheritdoc />
        public bool ExisteBase(string nombreBase)
        {
            object? valor = SqlHelper.EjecutarEscalar(
                "SELECT CASE WHEN DB_ID(@NombreBase) IS NULL THEN 0 ELSE 1 END;", CommandType.Text, Conexion,
                new SqlParameter("@NombreBase", nombreBase));
            return valor != null && valor != DBNull.Value && Convert.ToInt32(valor) == 1;
        }

        /// <inheritdoc />
        public void EjecutarBackup(string nombreBase, string rutaArchivo, TipoBackup tipo)
        {
            string tipoSql = tipo == TipoBackup.Completo ? "FULL" : "DIFFERENTIAL";
            // El completo se hace COPY_ONLY: no altera la base de los diferenciales ni la cadena de respaldos del
            // servidor. El diferencial no admite COPY_ONLY. En ambos casos CHECKSUM valida cada página.
            string opcionTipo = tipo == TipoBackup.Completo ? "COPY_ONLY, " : "DIFFERENTIAL, ";
            string sql =
                $"BACKUP DATABASE [{nombreBase}] TO DISK = N'{Escapar(rutaArchivo)}' " +
                $"WITH {opcionTipo}CHECKSUM, INIT, NAME = N'{Escapar(nombreBase)} {tipoSql} ({DateTime.Now:yyyy-MM-dd HH:mm})';";

            SqlHelper.EjecutarComando(sql, CommandType.Text, Conexion);
        }

        /// <inheritdoc />
        public string ObtenerCarpetaDelServidor()
        {
            object? valor = SqlHelper.EjecutarEscalar(
                "SELECT CONVERT(NVARCHAR(500), SERVERPROPERTY('InstanceDefaultBackupPath'));", CommandType.Text, Conexion);
            string? carpeta = valor == null || valor == DBNull.Value ? null : Convert.ToString(valor);
            if (string.IsNullOrWhiteSpace(carpeta))
            {
                throw new RespaldoException("No se pudo determinar la carpeta de respaldos del servidor SQL.");
            }
            return carpeta;
        }

        /// <inheritdoc />
        public bool EsErrorDeCarpetaSinPermiso(Exception error)
        {
            for (Exception? actual = error; actual != null; actual = actual.InnerException)
            {
                if (actual is SqlException sql)
                {
                    foreach (SqlError e in sql.Errors)
                    {
                        if (e.Number == ErrorDispositivoDeRespaldo)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <inheritdoc />
        public string LeerNombreBaseDesdeArchivo(string rutaArchivo)
        {
            string sql = $"RESTORE HEADERONLY FROM DISK = N'{Escapar(rutaArchivo)}';";

            using SqlDataReader lector = SqlHelper.EjecutarLector(sql, CommandType.Text, Conexion);
            if (lector.Read())
            {
                return lector.GetString(lector.GetOrdinal("DatabaseName"));
            }

            throw new AccesoADatosException("No se pudo leer el encabezado del archivo de respaldo.");
        }

        /// <inheritdoc />
        public int CantidadSesionesEnBase(string nombreBase)
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM sys.dm_exec_sessions
                WHERE database_id = DB_ID(@NombreBase)
                  AND session_id <> @@SPID;";

            object? valor = SqlHelper.EjecutarEscalar(
                sql, CommandType.Text, Conexion, new SqlParameter("@NombreBase", nombreBase));
            return valor == null || valor == DBNull.Value ? 0 : Convert.ToInt32(valor);
        }

        /// <inheritdoc />
        public void EjecutarRestore(string nombreBase, string rutaArchivo)
        {
            SqlHelper.EjecutarComando(
                $"ALTER DATABASE [{nombreBase}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;",
                CommandType.Text, Conexion);
            try
            {
                SqlHelper.EjecutarComando(
                    $"RESTORE DATABASE [{nombreBase}] FROM DISK = N'{Escapar(rutaArchivo)}' WITH REPLACE;",
                    CommandType.Text, Conexion);
            }
            finally
            {
                SqlHelper.EjecutarComando(
                    $"ALTER DATABASE [{nombreBase}] SET MULTI_USER;",
                    CommandType.Text, Conexion);
                // Las conexiones agrupadas quedaron cortadas por ROLLBACK IMMEDIATE: se descartan para que la
                // aplicación siga usando la base restaurada sin errores de conexión.
                SqlConnection.ClearAllPools();
            }
        }

        /// <inheritdoc />
        public void EjecutarVerifyOnly(string rutaArchivo)
        {
            SqlHelper.EjecutarComando(
                $"RESTORE VERIFYONLY FROM DISK = N'{Escapar(rutaArchivo)}' WITH CHECKSUM;",
                CommandType.Text, Conexion);
        }

        private static string Escapar(string valor) => valor.Replace("'", "''");
    }
}
