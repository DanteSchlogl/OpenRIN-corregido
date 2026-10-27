using Services.DomainModel;

namespace Services.DAL.Interfaces
{
    /// <summary>
    /// Contrato de operaciones de respaldo/restauración sobre SQL Server (REQ-ARQ-005).
    /// La implementación encapsula los comandos SQL nativos (BACKUP/RESTORE...) y se ejecuta
    /// con credenciales de administrador (cadena de conexión "BackupString").
    /// </summary>
    public interface IBackupRepository
    {
        /// <summary>Devuelve el tamaño total de la base indicada, en MB.</summary>
        double ObtenerTamanoBaseMb(string nombreBase);

        /// <summary>Ejecuta BACKUP DATABASE (FULL o DIFFERENTIAL) generando el archivo .bak indicado.</summary>
        void EjecutarBackup(string nombreBase, string rutaArchivo, TipoBackup tipo);

        /// <summary>Lee el nombre de la base de datos original desde el encabezado de un archivo .bak.</summary>
        string LeerNombreBaseDesdeArchivo(string rutaArchivo);

        /// <summary>Cuenta las sesiones activas sobre la base indicada (excluyendo la sesión actual).</summary>
        int CantidadSesionesEnBase(string nombreBase);

        /// <summary>Ejecuta RESTORE DATABASE (pasando la base a SINGLE_USER y de vuelta a MULTI_USER).</summary>
        void EjecutarRestore(string nombreBase, string rutaArchivo);

        /// <summary>Ejecuta RESTORE VERIFYONLY sobre el archivo indicado (lanza si es inválido).</summary>
        void EjecutarVerifyOnly(string rutaArchivo);
    }
}
