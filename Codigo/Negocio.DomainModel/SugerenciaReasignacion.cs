namespace Negocio.DomainModel
{
    /// <summary>
    /// Sugerencia de reasignación inteligente de turnos (REQ-FUNC-009): propone mover el
    /// turno de un paciente estable a un paciente crítico que no posee turno, para maximizar
    /// la eficiencia clínica de la agenda.
    /// </summary>
    public class SugerenciaReasignacion
    {
        /// <summary>Turno del paciente estable que se propone liberar.</summary>
        public int IdTurno { get; set; }

        public int IdPacienteEstable { get; set; }

        public string NombrePacienteEstable { get; set; } = string.Empty;

        public int IdPacienteCritico { get; set; }

        public string NombrePacienteCritico { get; set; } = string.Empty;

        /// <summary>Fecha y hora del turno que se propone reasignar.</summary>
        public DateTime FechaHoraTurno { get; set; }

        /// <summary>Médico asignado al turno.</summary>
        public int IdUsuarioMedico { get; set; }

        /// <summary>Descripción para la interfaz y la auditoría (por qué se sugiere).</summary>
        public string Descripcion { get; set; } = string.Empty;
    }
}
