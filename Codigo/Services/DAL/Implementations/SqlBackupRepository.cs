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
    public class SqlBackupRepository : IBackupRepository
    {
        private const string Conexion = "BackupString";

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
        public void EjecutarBackup(string nombreBase, string rutaArchivo, TipoBackup tipo)
        {
            string tipoSql = tipo == TipoBackup.Completo ? "FULL" : "DIFFERENTIAL";
            // Nota: el backup completo NO lleva palabra clave propia (es el modo por defecto);
            // el diferencial se indica con WITH DIFFERENTIAL.
            string opcionTipo = tipo == TipoBackup.Diferencial ? "DIFFERENTIAL, " : "";
            string sql =
                $"BACKUP DATABASE [{nombreBase}] TO DISK = N'{Escapar(rutaArchivo)}' " +
                $"WITH {opcionTipo}INIT, NAME = N'{Escapar(nombreBase)} {tipoSql} ({DateTime.Now:yyyy-MM-dd HH:mm})';";

            SqlHelper.EjecutarComando(sql, CommandType.Text, Conexion);
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

            throw new DataAccessException("No se pudo leer el encabezado del archivo de respaldo.");
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
            }
        }

        /// <inheritdoc />
        public void EjecutarVerifyOnly(string rutaArchivo)
        {
            SqlHelper.EjecutarComando(
                $"RESTORE VERIFYONLY FROM DISK = N'{Escapar(rutaArchivo)}';",
                CommandType.Text, Conexion);
        }

        private static string Escapar(string valor) => valor.Replace("'", "''");
    }
}
