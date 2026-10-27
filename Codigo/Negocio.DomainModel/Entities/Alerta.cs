using Negocio.DomainModel.Enums;
using Services.DomainModel.Validacion;

namespace Negocio.DomainModel
{
    /// <summary>
    /// Notificación generada ante valores críticos o ausencias de reporte (REQ-FUNC-007/008/010).
    /// Permanece Activa hasta que el médico registra una acción de resolución; si el protocolo
    /// de contacto se agota sin éxito, la alerta se escala al médico.
    /// </summary>
    public class Alerta
    {
        public int Id { get; set; }

        public int IdPaciente { get; set; }

        public Paciente? Paciente { get; set; }

        /// <summary>Medición que originó la alerta (null si es por ausencia de reporte).</summary>
        public int? IdMedicion { get; set; }

        public MedicionRIN? Medicion { get; set; }

        public TipoAlerta Tipo { get; set; }

        /// <summary>Nivel de criticidad asociado (habitualmente Alta).</summary>
        public NivelCriticidad? NivelCriticidad { get; set; }

        [Requerido]
        public string Descripcion { get; set; } = string.Empty;

        public DateTime FechaGeneracion { get; set; } = DateTime.Now;

        public EstadoAlerta Estado { get; set; } = EstadoAlerta.Activa;

        /// <summary>Notificación interna enviada al médico responsable.</summary>
        public bool NotificadaMedico { get; set; }

        /// <summary>Notificación interna enviada al administrativo.</summary>
        public bool NotificadaAdministrativo { get; set; }

        /// <summary>Indica si la alerta fue escalada al médico (protocolo de contacto agotado).</summary>
        public bool EscaladaAMedico { get; set; }

        public DateTime? FechaEscalada { get; set; }

        public DateTime? FechaResolucion { get; set; }

        public int? IdUsuarioResolucion { get; set; }

        /// <summary>Acción de resolución registrada por el médico.</summary>
        public string? AccionResolucion { get; set; }
    }
}
