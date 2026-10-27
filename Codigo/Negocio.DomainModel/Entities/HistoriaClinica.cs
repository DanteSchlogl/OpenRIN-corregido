using Services.DomainModel.Validacion;

namespace Negocio.DomainModel
{
    /// <summary>
    /// Configuración hematológica del paciente (REQ-FUNC-002): rango terapéutico objetivo
    /// de RIN, medicación y periodicidad esperada de reporte. Es el insumo indispensable
    /// para el cálculo de criticidad de los reportes de RIN.
    /// </summary>
    public class HistoriaClinica
    {
        public int Id { get; set; }

        /// <summary>Paciente al que pertenece la historia clínica (una HC por paciente).</summary>
        public int IdPaciente { get; set; }

        public Paciente? Paciente { get; set; }

        /// <summary>Patología o indicación de anticoagulación (catálogo).</summary>
        public int? IdDiagnostico { get; set; }

        public Diagnostico? Diagnostico { get; set; }

        /// <summary>Límite inferior del rango terapéutico objetivo de RIN.</summary>
        [Requerido]
        [Rango(0.1, 20.0)]
        public decimal LimiteInferiorRIN { get; set; }

        /// <summary>Límite superior del rango terapéutico objetivo de RIN (debe superar al inferior).</summary>
        [Requerido]
        [Rango(0.1, 20.0)]
        public decimal LimiteSuperiorRIN { get; set; }

        /// <summary>Medicamento anticoagulante actual (fármaco).</summary>
        [Requerido]
        public string Medicamento { get; set; } = string.Empty;

        /// <summary>Dosis indicada del medicamento.</summary>
        [Requerido]
        public string Dosis { get; set; } = string.Empty;

        /// <summary>Periodicidad esperada de reporte, en días.</summary>
        [Requerido]
        [Rango(1, 365)]
        public int PeriodicidadDias { get; set; } = 30;

        /// <summary>Fecha del próximo control indicado (si corresponde).</summary>
        public DateTime? ProximaFechaControl { get; set; }

        public DateTime FechaConfiguracion { get; set; } = DateTime.Now;

        public DateTime UltimaActualizacion { get; set; } = DateTime.Now;
    }
}
