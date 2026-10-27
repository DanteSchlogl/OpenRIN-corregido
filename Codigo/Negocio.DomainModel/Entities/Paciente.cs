using Negocio.DomainModel.Enums;
using Services.DomainModel.Validacion;

namespace Negocio.DomainModel
{
    /// <summary>
    /// Persona anticoagulada registrada en el centro (REQ-FUNC-001). La baja es lógica:
    /// el estado pasa a Inactivo y las credenciales se deshabilitan, pero el historial
    /// clínico se conserva.
    /// </summary>
    public class Paciente
    {
        public int Id { get; set; }

        [Requerido]
        public string NombreCompleto { get; set; } = string.Empty;

        /// <summary>Documento de identidad (único entre pacientes activos).</summary>
        [Requerido]
        [Unico]
        public string DNI { get; set; } = string.Empty;

        [Requerido]
        public string Telefono { get; set; } = string.Empty;

        [Requerido]
        public string Email { get; set; } = string.Empty;

        /// <summary>Obra social que cubre al paciente (opcional).</summary>
        public int? IdObraSocial { get; set; }

        public ObraSocial? ObraSocial { get; set; }

        /// <summary>Número de afiliado de la obra social (opcional).</summary>
        public string? NumeroAfiliado { get; set; }

        /// <summary>Credenciales de acceso al portal de pacientes (se generan en el alta).</summary>
        public int? IdUsuarioPortal { get; set; }

        public EstadoPaciente Estado { get; set; } = EstadoPaciente.Activo;

        /// <summary>Motivo registrado al dar de baja lógica al paciente.</summary>
        public string? MotivoBaja { get; set; }

        public DateTime FechaAlta { get; set; } = DateTime.Now;

        public DateTime? FechaBaja { get; set; }
    }
}
