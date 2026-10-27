namespace Negocio.DomainModel.Enums
{
    /// <summary>
    /// Estado de un turno presencial en la agenda médica (REQ-FUNC-009).
    /// </summary>
    public enum EstadoTurno
    {
        /// <summary>Turno agendado a la espera de confirmación.</summary>
        Pendiente,

        /// <summary>Turno confirmado con el paciente.</summary>
        Confirmado,

        /// <summary>Turno cancelado.</summary>
        Cancelado,

        /// <summary>Turno efectivamente atendido.</summary>
        Atendido,

        /// <summary>Turno marcado como candidato dentro de una sugerencia de reasignación.</summary>
        EnReasignacion
    }
}
