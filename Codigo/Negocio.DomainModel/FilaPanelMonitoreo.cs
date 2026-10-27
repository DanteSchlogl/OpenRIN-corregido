namespace Negocio.DomainModel
{
    /// <summary>
    /// Fila del panel de monitoreo (REQ-FUNC-006): datos de un paciente activo con su
    /// último valor de RIN, la criticidad vigente y el estado de alerta para el ranking visual.
    /// </summary>
    public class FilaPanelMonitoreo
    {
        public int IdPaciente { get; set; }

        public string NombreCompleto { get; set; } = string.Empty;

        public string DNI { get; set; } = string.Empty;

        /// <summary>Último valor de RIN reportado (null si el paciente no tiene reportes).</summary>
        public decimal? UltimoValorRIN { get; set; }

        /// <summary>Fecha del último reporte.</summary>
        public DateTime? FechaUltimoReporte { get; set; }

        /// <summary>Criticidad vigente del último reporte (null si no hay reportes).</summary>
        public Enums.NivelCriticidad? NivelCriticidad { get; set; }

        /// <summary>Indica si el último reporte fue reclasificado por tendencia peligrosa (REQ-FUNC-008).</summary>
        public bool PorTendenciaPeligrosa { get; set; }

        /// <summary>Indica si el paciente tiene alguna alerta activa pendiente de resolución.</summary>
        public bool TieneAlertaActiva { get; set; }

        /// <summary>Días transcurridos desde el último reporte (null si no hay reportes).</summary>
        public int? DiasDesdeUltimoReporte { get; set; }
    }
}
