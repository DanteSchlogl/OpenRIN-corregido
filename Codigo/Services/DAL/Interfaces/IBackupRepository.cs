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

        /// <summary>
        /// Ejecuta BACKUP DATABASE con CHECKSUM (el completo además con COPY_ONLY, para no alterar la cadena de
        /// respaldos) generando el archivo .bak indicado.
        /// </summary>
        void EjecutarBackup(string nombreBase, string rutaArchivo, TipoBackup tipo);

        /// <summary>Carpeta donde el servicio de SQL Server tiene permiso de escritura por defecto.</summary>
        string ObtenerCarpetaDelServidor();

        /// <summary>Indica si el error de SQL Server corresponde a una carpeta sin permiso de escritura para el servicio.</summary>
        bool EsErrorDeCarpetaSinPermiso(System.Exception error);

        /// <summary>Lee el nombre de la base de datos original desde el encabezado de un archivo .bak.</summary>
        string LeerNombreBaseDesdeArchivo(string rutaArchivo);

        /// <summary>Cuenta las sesiones activas sobre la base indicada (excluyendo la sesión actual).</summary>
        int CantidadSesionesEnBase(string nombreBase);

        /// <summary>Ejecuta RESTORE DATABASE (SINGLE_USER, WITH REPLACE y de vuelta a MULTI_USER) y limpia los pools de conexiones.</summary>
        void EjecutarRestore(string nombreBase, string rutaArchivo);

        /// <summary>Ejecuta RESTORE VERIFYONLY WITH CHECKSUM sobre el archivo indicado (lanza si es inválido).</summary>
        void EjecutarVerifyOnly(string rutaArchivo);

        /// <summary>Indica si la base existe en el servidor.</summary>
        bool ExisteBase(string nombreBase);
    }
}
