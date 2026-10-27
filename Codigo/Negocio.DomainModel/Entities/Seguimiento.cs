using Negocio.DomainModel.Enums;

namespace Negocio.DomainModel
{
    /// <summary>
    /// Registro de intentos de contacto y decisiones clínicas del seguimiento proactivo
    /// (REQ-FUNC-010). Si un intento es exitoso, la alerta de ausencia asociada se cierra;
    /// al agotar el umbral de intentos fallidos, el caso se escala al médico, quien puede
    /// registrar su decisión clínica (incluyendo actualizar la periodicidad de la HC).
    /// </summary>
    public class Seguimiento
    {
        public int Id { get; set; }

        public int IdPaciente { get; set; }

        public Paciente? Paciente { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        /// <summary>Medio utilizado en el intento de contacto.</summary>
        public TipoContacto TipoContacto { get; set; }

        public ResultadoContacto Resultado { get; set; }

        public string? Observaciones { get; set; }

        /// <summary>Usuario (administrativo) que registró el intento.</summary>
        public int? IdUsuarioRegistro { get; set; }

        /// <summary>Decisión clínica del médico tras agotar el protocolo.</summary>
        public DecisionClinica DecisionClinica { get; set; } = DecisionClinica.Ninguna;

        public string? DetalleDecision { get; set; }

        public int? IdUsuarioDecision { get; set; }

        public DateTime? FechaDecision { get; set; }
    }
}
