using System.Collections.Generic;
using Services.BLL.Backups;
using Services.DomainModel;

namespace Services.Facade
{
    /// <summary>
    /// Fachada pública del módulo de respaldo (REQ-ARQ-005): crear, restaurar y verificar
    /// copias de seguridad de las bases de la plataforma, más la programación automática diaria.
    /// </summary>
    public static class BackupService
    {
        /// <summary>Realiza un respaldo (Completo o Diferencial) y devuelve la ruta del archivo .bak generado.</summary>
        public static string RealizarBackup(TipoBackup tipo, string? nombreBaseDatos = null)
            => BackupLogic.RealizarBackup(tipo, nombreBaseDatos);

        /// <summary>Realiza un respaldo de TODAS las bases de la plataforma y devuelve las rutas generadas.</summary>
        public static List<string> RealizarBackupDeLaPlataforma(TipoBackup tipo)
            => BackupLogic.RealizarBackupDeLaPlataforma(tipo);

        /// <summary>Restaura una base desde un archivo .bak (verificando antes que la base no esté en uso).</summary>
        public static void RestaurarBackup(string rutaArchivo) => BackupLogic.RestaurarBackup(rutaArchivo);

        /// <summary>Verifica la integridad de un archivo de respaldo (RESTORE VERIFYONLY). Devuelve false si es inválido.</summary>
        public static bool VerificarIntegridadBackup(string rutaArchivo) => BackupLogic.VerificarIntegridadBackup(rutaArchivo);

        /// <summary>Programa un respaldo completo automático diario (por defecto a las 3:00 AM).</summary>
        public static void ProgramarRespaldoDiario(int horaDelDia = 3) => BackupLogic.ProgramarRespaldoDiario(horaDelDia);

        /// <summary>Detiene la programación automática diaria.</summary>
        public static void DetenerProgramacion() => BackupLogic.DetenerProgramacion();

        /// <summary>Indica si hay una programación automática activa.</summary>
        public static bool ProgramacionActiva => BackupLogic.ProgramacionActiva;
    }
}
