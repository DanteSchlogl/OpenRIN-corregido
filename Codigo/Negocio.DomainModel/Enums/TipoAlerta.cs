namespace Negocio.DomainModel.Enums
{
    /// <summary>
    /// Motivo de generación de una alerta (REQ-FUNC-007/008/010).
    /// </summary>
    public enum TipoAlerta
    {
        /// <summary>Medición clasificada con criticidad Alta.</summary>
        CriticidadAlta,

        /// <summary>Dos reportes consecutivos en el borde del rango (±0.1) — tendencia peligrosa.</summary>
        TendenciaPeligrosa,

        /// <summary>Plazo de reporte vencido sin registro de RIN (protocolo de contacto).</summary>
        ReporteAusente,

        Otro
    }
}
