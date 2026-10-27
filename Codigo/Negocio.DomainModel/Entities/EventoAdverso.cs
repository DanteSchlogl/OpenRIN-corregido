using Negocio.DomainModel.Enums;
using Services.DomainModel.Validacion;

namespace Negocio.DomainModel
{
    /// <summary>
    /// Incidente clínico relevante registrado en la historia clínica del paciente
    /// (REQ-FUNC-003): hemorragias, trombosis, reacciones. La fecha no puede ser futura.
    /// </summary>
    public class EventoAdverso
    {
        public int Id { get; set; }

        public int IdPaciente { get; set; }

        public Paciente? Paciente { get; set; }

        /// <summary>Fecha en que ocurrió el evento (no puede ser futura).</summary>
        public DateTime Fecha { get; set; }

        public TipoEventoAdverso Tipo { get; set; }

        [Requerido]
        public string Descripcion { get; set; } = string.Empty;

        public GravedadEvento Gravedad { get; set; }

        /// <summary>Acción tomada por el equipo médico.</summary>
        [Requerido]
        public string AccionTomada { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        /// <summary>Usuario que registró el evento (opcional).</summary>
        public int? IdUsuarioRegistro { get; set; }
    }
}
