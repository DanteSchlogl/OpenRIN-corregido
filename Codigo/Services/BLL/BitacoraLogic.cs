using System;
using System.Collections.Generic;
using System.Configuration;
using Services.DAL.Implementations;
using Services.DAL.Interfaces;
using Services.DomainModel;

namespace Services.BLL
{
    /// <summary>
    /// Lógica de bitácora (REQ-ARQ-003): filtra por el nivel mínimo configurado (app.config),
    /// persiste en la tabla Logs y cae al archivo de respaldo si la base de datos falla.
    /// Nunca propaga excepciones hacia el invocante. Expone además la consulta de las últimas
    /// entradas para la pantalla de administración (Sistema).
    /// </summary>
    internal static class BitacoraLogic
    {
        private static readonly SqlLoggerRepository RepositorioSql = new SqlLoggerRepository();
        private static readonly ILoggerRepository RepositorioArchivo = new FileLoggerRepository();
        private static readonly object Candado = new object();

        public static void Registrar(LogLevel nivel, string mensaje, Exception? ex = null, string usuario = "", string capa = "Services")
        {
            try
            {
                if (nivel < NivelMinimo())
                {
                    return;
                }

                LogEntry entrada = new LogEntry
                {
                    Fecha = DateTime.Now,
                    Nivel = nivel,
                    Mensaje = mensaje,
                    Excepcion = ex?.ToString(),
                    Usuario = usuario,
                    Capa = capa
                };

                lock (Candado)
                {
                    try
                    {
                        RepositorioSql.Registrar(entrada);
                    }
                    catch (Exception)
                    {
                        // Si la base falla, se escribe al archivo de respaldo con rotación (REQ-ARQ-003).
                        RepositorioArchivo.Registrar(entrada);
                    }
                }
            }
            catch
            {
                // La bitácora nunca debe romper la aplicación (REQ-ARQ-003).
            }
        }

        /// <summary>
        /// Devuelve las últimas entradas de la bitácora persistida, opcionalmente filtradas por
        /// nivel mínimo. Ante cualquier error devuelve una lista vacía (nunca propaga).
        /// </summary>
        public static List<LogEntry> ObtenerUltimos(int cantidad, LogLevel? nivelMinimo = null)
        {
            try
            {
                return RepositorioSql.ObtenerUltimos(cantidad, nivelMinimo);
            }
            catch
            {
                return new List<LogEntry>();
            }
        }

        private static LogLevel NivelMinimo()
        {
            string? valor = ConfigurationManager.AppSettings["MinimalLogLevel"];
            return Enum.TryParse(valor, ignoreCase: true, out LogLevel nivel) ? nivel : LogLevel.Info;
        }
    }
}
