using Services.DomainModel;

namespace Services.DAL.Interfaces
{
    /// <summary>Contrato de persistencia de bitácora (tabla Logs o archivo de respaldo).</summary>
    public interface ILoggerRepository
    {
        /// <summary>Persiste una entrada de bitácora.</summary>
        void Registrar(LogEntry entrada);
    }
}
