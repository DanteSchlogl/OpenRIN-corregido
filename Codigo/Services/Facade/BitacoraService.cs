using System.Collections.Generic;
using Services.BLL;
using Services.DomainModel;

namespace Services.Facade
{
    /// <summary>
    /// Fachada pública de la bitácora (REQ-ARQ-003): registro de eventos de todas las capas
    /// y consulta de las últimas entradas para la pantalla de administración del sistema.
    /// </summary>
    public static class BitacoraService
    {
        /// <summary>Registra un evento con el nivel indicado. Nunca lanza excepciones.</summary>
        public static void Registrar(LogLevel nivel, string mensaje, Exception? ex = null, string usuario = "", string capa = "Services")
            => BitacoraLogic.Registrar(nivel, mensaje, ex, usuario, capa);

        /// <summary>Registra un evento de depuración.</summary>
        public static void Debug(string mensaje, string usuario = "") => Registrar(LogLevel.Debug, mensaje, null, usuario);

        /// <summary>Registra un evento informativo.</summary>
        public static void Info(string mensaje, string usuario = "") => Registrar(LogLevel.Info, mensaje, null, usuario);

        /// <summary>Registra una advertencia.</summary>
        public static void Warning(string mensaje, string usuario = "") => Registrar(LogLevel.Warning, mensaje, null, usuario);

        /// <summary>Registra un error (con su excepción, si aplica).</summary>
        public static void Error(string mensaje, Exception? ex = null, string usuario = "") => Registrar(LogLevel.Error, mensaje, ex, usuario);

        /// <summary>Registra un error fatal.</summary>
        public static void Fatal(string mensaje, Exception? ex = null, string usuario = "") => Registrar(LogLevel.Fatal, mensaje, ex, usuario);

        /// <summary>Devuelve las últimas entradas de la bitácora, opcionalmente filtradas por nivel mínimo.</summary>
        public static List<LogEntry> ObtenerUltimos(int cantidad = 200, LogLevel? nivelMinimo = null)
            => BitacoraLogic.ObtenerUltimos(cantidad, nivelMinimo);
    }
}
