using System;
using System.Collections.Generic;
using Services.BLL.Backups;
using Services.DomainModel;

namespace Services.Facade
{
    /// <summary>
    /// Fachada pública del módulo de respaldo (REQ-ARQ-005): crear, restaurar y verificar copias de seguridad de
    /// las bases de la plataforma, el registro de respaldos y el respaldo automático diario.
    /// </summary>
    public static class BackupService
    {
        /// <summary>Realiza un respaldo (Completo o Diferencial) y devuelve la ruta del archivo .bak generado.</summary>
        /// <param name="carpetaDestino">Carpeta elegida; si es nula se usa la configurada (BackupFolder).</param>
        public static string RealizarBackup(TipoBackup tipo, string? nombreBaseDatos = null, string? carpetaDestino = null, string usuario = "")
            => BackupLogic.RealizarBackup(tipo, nombreBaseDatos, carpetaDestino, "Manual", usuario);

        /// <summary>Realiza un respaldo de TODAS las bases de la plataforma y devuelve las rutas generadas.</summary>
        public static List<string> RealizarBackupDeLaPlataforma(TipoBackup tipo, string? carpetaDestino = null, string usuario = "")
            => BackupLogic.RealizarBackupDeLaPlataforma(tipo, carpetaDestino, "Manual", usuario);

        /// <summary>
        /// Respaldo automático de una vez por día: solo si los datos están sanos y hoy no se hizo uno; conserva los
        /// últimos 10. Devuelve los archivos generados (vacío si no correspondía).
        /// </summary>
        public static List<string> RespaldoDiarioSiCorresponde(Func<bool>? datosSanos = null, string usuario = "sistema", string? carpetaDestino = null)
            => BackupLogic.RespaldoDiarioSiCorresponde(datosSanos, usuario, carpetaDestino);

        /// <summary>Define cómo saber si los datos están sanos, para el respaldo automático programado.</summary>
        public static void EstablecerVerificadorDeSalud(Func<bool>? verificador) => BackupLogic.EstablecerVerificadorDeSalud(verificador);

        /// <summary>Lista los últimos respaldos registrados (tabla Respaldos).</summary>
        public static List<RespaldoRegistro> ListarRespaldos(int cantidad = 200) => BackupLogic.ListarRespaldos(cantidad);

        /// <summary>
        /// Restaura una base desde un archivo .bak: verifica el archivo, hace una copia previa de seguridad,
        /// pasa la base a SINGLE_USER, restaura WITH REPLACE y la devuelve a MULTI_USER.
        /// </summary>
        public static void RestaurarBackup(string rutaArchivo, string usuario = "") => BackupLogic.RestaurarBackup(rutaArchivo, usuario);

        /// <summary>Registra en la tabla Respaldos un archivo .bak externo elegido por el usuario.</summary>
        public static void RegistrarImportado(string rutaArchivo, string usuario) => BackupLogic.RegistrarImportado(rutaArchivo, usuario);

        /// <summary>Nombre de la base de datos a la que pertenece un archivo .bak.</summary>
        public static string ObtenerBaseDelArchivo(string rutaArchivo) => BackupLogic.ObtenerBaseDelArchivo(rutaArchivo);

        /// <summary>Verifica la integridad de un archivo de respaldo (RESTORE VERIFYONLY con CHECKSUM). Devuelve false si es inválido.</summary>
        public static bool VerificarIntegridadBackup(string rutaArchivo) => BackupLogic.VerificarIntegridadBackup(rutaArchivo);

        /// <summary>Programa un respaldo completo automático diario (por defecto a las 3:00 AM).</summary>
        public static void ProgramarRespaldoDiario(int horaDelDia = 3) => BackupLogic.ProgramarRespaldoDiario(horaDelDia);

        /// <summary>Detiene la programación automática diaria.</summary>
        public static void DetenerProgramacion() => BackupLogic.DetenerProgramacion();

        /// <summary>Indica si hay una programación automática activa.</summary>
        public static bool ProgramacionActiva => BackupLogic.ProgramacionActiva;
    }
}
