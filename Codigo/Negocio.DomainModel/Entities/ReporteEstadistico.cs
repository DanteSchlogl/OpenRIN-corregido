using Services.DomainModel.Validacion;

namespace Negocio.DomainModel
{
    /// <summary>
    /// Informe estadístico de eficacia del tratamiento (REQ-FUNC-011): consolidado mensual
    /// con indicadores por paciente, clasificación de control y detalle serializado para
    /// trazabilidad. Alimenta también el reporte por diagnóstico (REQ-FUNC-012).
    /// </summary>
    public class ReporteEstadistico
    {
        public int Id { get; set; }

        /// <summary>Año del período reportado.</summary>
        [Rango(2020, 2100)]
        public int Anio { get; set; }

        /// <summary>Mes del período reportado (1-12).</summary>
        [Rango(1, 12)]
        public int Mes { get; set; }

        public DateTime FechaGeneracion { get; set; } = DateTime.Now;

        /// <summary>Indicador global de eficacia: porcentaje promedio de mediciones dentro del rango.</summary>
        [Rango(0, 100)]
        public decimal IndicadorGlobalEficacia { get; set; }

        /// <summary>Cantidad de pacientes con control óptimo en el período.</summary>
        public int CantidadOptimos { get; set; }

        /// <summary>Cantidad de pacientes con control subóptimo en el período.</summary>
        public int CantidadSuboptimos { get; set; }

        /// <summary>Cantidad de pacientes con control deficiente en el período.</summary>
        public int CantidadDeficientes { get; set; }

        /// <summary>Detalle del reporte (resumen ejecutivo, detalle por paciente y tendencias) en JSON.</summary>
        public string? DetalleJson { get; set; }
    }
}
