using System.Collections.Generic;
using Services.DomainModel;

namespace Services.DAL.Interfaces
{
    /// <summary>Repositorio de bitácora que además permite consultar las últimas entradas (tabla Logs).</summary>
    public interface ILoggerConsultaRepository : ILoggerRepository
    {
        /// <summary>Devuelve las últimas entradas, de la más reciente a la más antigua, desde un nivel mínimo opcional.</summary>
        List<LogEntry> ObtenerUltimos(int cantidad, LogLevel? nivelMinimo = null);
    }
}
