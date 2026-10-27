namespace Negocio.DomainModel
{
    /// <summary>
    /// Fila del reporte de eficacia por diagnóstico (REQ-FUNC-012): agrupa a los pacientes
    /// según la patología registrada en su HC y resume su desempeño en el período.
    /// </summary>
    public class FilaReporteDiagnostico
    {
        /// <summary>Id del diagnóstico agrupador (null = pacientes sin diagnóstico asignado).</summary>
        public int? IdDiagnostico { get; set; }

        public string NombreDiagnostico { get; set; } = string.Empty;

        /// <summary>Cantidad de pacientes del grupo con mediciones en el período.</summary>
        public int CantidadPacientes { get; set; }

        /// <summary>Indicador promedio de eficacia del grupo (% de mediciones dentro del rango).</summary>
        public decimal IndicadorPromedio { get; set; }

        public int CantidadOptimos { get; set; }

        public int CantidadSuboptimos { get; set; }

        public int CantidadDeficientes { get; set; }

        /// <summary>Destaca los diagnósticos con bajo desempeño (indicador por debajo del umbral).</summary>
        public bool BajoDesempeno { get; set; }
    }
}
