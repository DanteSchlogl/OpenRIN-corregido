using Negocio.DomainModel.Enums;
using Services.DomainModel.Validacion;

namespace Negocio.DomainModel
{
    /// <summary>
    /// Registro individual de un valor de RIN reportado (REQ-FUNC-004).
    /// Almacena la fecha/hora, el canal y el usuario que realizó la carga,
    /// y queda disponible para el módulo de cálculo de criticidad.
    /// </summary>
    public class MedicionRIN
    {
        /// <summary>Valor inferior biológicamente posible de un RIN.</summary>
        public const decimal RangoBiologicoMinimo = 0.1m;

        /// <summary>Valor superior biológicamente posible de un RIN.</summary>
        public const decimal RangoBiologicoMaximo = 20.0m;

        public int Id { get; set; }

        public int IdPaciente { get; set; }

        public Paciente? Paciente { get; set; }

        /// <summary>Valor de RIN reportado (dentro del rango biológicamente posible).</summary>
        [Requerido]
        [Rango(0.1, 20.0)]
        public decimal ValorRIN { get; set; }

        /// <summary>Fecha de la medición (no puede ser futura).</summary>
        public DateTime FechaMedicion { get; set; }

        /// <summary>Canal por el que se recibió el reporte (digital o telefónico).</summary>
        public CanalReporteRIN Canal { get; set; }

        /// <summary>Usuario que registró el reporte (opcional: carga del propio paciente).</summary>
        public int? IdUsuarioRegistro { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        /// <summary>Nivel de criticidad calculado (REQ-FUNC-005); null = pendiente de evaluación.</summary>
        public NivelCriticidad? NivelCriticidad { get; set; }

        /// <summary>Indica si el reporte fue reclasificado por tendencia peligrosa (REQ-FUNC-008).</summary>
        public bool PorTendenciaPeligrosa { get; set; }
    }
}
