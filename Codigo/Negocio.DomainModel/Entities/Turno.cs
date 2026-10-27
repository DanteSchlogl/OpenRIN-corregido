using Negocio.DomainModel.Enums;

namespace Negocio.DomainModel
{
    /// <summary>
    /// Cita presencial programada en la agenda médica (REQ-FUNC-009). Un turno puede
    /// entrar en estado EnReasignacion como candidato dentro de una sugerencia de
    /// reasignación inteligente entre pacientes estables y críticos.
    /// </summary>
    public class Turno
    {
        public int Id { get; set; }

        public int IdPaciente { get; set; }

        public Paciente? Paciente { get; set; }

        /// <summary>Médico asignado (usuario con perfil médico).</summary>
        public int IdUsuarioMedico { get; set; }

        /// <summary>Fecha y hora de la cita.</summary>
        public DateTime FechaHora { get; set; }

        public EstadoTurno Estado { get; set; } = EstadoTurno.Pendiente;

        public string? Observaciones { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
