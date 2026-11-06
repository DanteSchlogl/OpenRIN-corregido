using System.Collections.Generic;

namespace Services.DomainModel.Exceptions
{
    /// <summary>Los dígitos verificadores indican que la base fue alterada fuera del sistema.</summary>
    public class IntegridadVulneradaException : ReglaNegocioException
    {
        /// <summary>Registros o tablas alterados.</summary>
        public IReadOnlyList<string> Problemas { get; }

        public IntegridadVulneradaException(IReadOnlyList<string> problemas)
            : base("Se detectaron alteraciones en la base de datos realizadas fuera del sistema.")
            => Problemas = problemas;
    }
}
