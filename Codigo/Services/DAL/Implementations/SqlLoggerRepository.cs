using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Services.DAL.Interfaces;
using Services.DAL.Tools;
using Services.DomainModel;

namespace Services.DAL.Implementations
{
    /// <summary>
    /// Persiste entradas de bitácora en la tabla Logs de la base Services (ADO.NET, REQ-ARQ-003).
    /// En caso de error técnico lanza <see cref="Services.DomainModel.Exceptions.DataAccessException"/>.
    /// Expone también la consulta de las últimas entradas (pantalla de administración).
    /// </summary>
    public class SqlLoggerRepository : ILoggerRepository
    {
        private const string InsertarSql = @"
            INSERT INTO dbo.Logs (Fecha, Nivel, Mensaje, Excepcion, Usuario, Capa)
            VALUES (@Fecha, @Nivel, @Mensaje, @Excepcion, @Usuario, @Capa);";

        /// <inheritdoc />
        public void Registrar(LogEntry entrada)
        {
            SqlHelper.EjecutarComando(
                InsertarSql,
                CommandType.Text,
                new SqlParameter("@Fecha", entrada.Fecha),
                new SqlParameter("@Nivel", entrada.Nivel.ToString()),
                new SqlParameter("@Mensaje", entrada.Mensaje),
                new SqlParameter("@Excepcion", (object?)entrada.Excepcion ?? DBNull.Value),
                new SqlParameter("@Usuario", (object?)entrada.Usuario ?? DBNull.Value),
                new SqlParameter("@Capa", (object?)entrada.Capa ?? DBNull.Value));
        }

        /// <summary>
        /// Devuelve las últimas entradas de la bitácora (más recientes primero), opcionalmente
        /// filtradas por nivel mínimo. Si hay filtro se leen más filas y se recorta en memoria,
        /// porque el nivel se persiste como texto legible.
        /// </summary>
        public List<LogEntry> ObtenerUltimos(int cantidad, LogLevel? nivelMinimo = null)
        {
            const string consulta = @"
                SELECT TOP (@Cantidad) Id, Fecha, Nivel, Mensaje, Excepcion, Usuario, Capa
                FROM dbo.Logs
                ORDER BY Fecha DESC, Id DESC;";

            int tope = nivelMinimo == null ? cantidad : cantidad * 4;
            using SqlDataReader lector = SqlHelper.EjecutarLector(
                consulta, CommandType.Text, new SqlParameter("@Cantidad", tope));

            var lista = new List<LogEntry>();
            while (lector.Read())
            {
                LogEntry entrada = new LogEntry
                {
                    Id = lector.GetInt32(0),
                    Fecha = lector.GetDateTime(1),
                    Nivel = Enum.TryParse(lector.GetString(2), out LogLevel nivel) ? nivel : LogLevel.Info,
                    Mensaje = lector.GetString(3),
                    Excepcion = lector.IsDBNull(4) ? null : lector.GetString(4),
                    Usuario = lector.IsDBNull(5) ? null : lector.GetString(5),
                    Capa = lector.IsDBNull(6) ? null : lector.GetString(6)
                };

                if (nivelMinimo != null && entrada.Nivel < nivelMinimo.Value)
                {
                    continue;
                }

                lista.Add(entrada);
                if (lista.Count >= cantidad)
                {
                    break;
                }
            }
            return lista;
        }
    }
}
