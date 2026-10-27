namespace Negocio.DomainModel.Enums
{
    /// <summary>
    /// Nivel de criticidad de una medición de RIN (REQ-FUNC-005):
    /// Baja = dentro del rango terapéutico; Media = fuera por menos de ±0.5;
    /// Alta = fuera por ±0.5 o más (o por tendencia peligrosa, REQ-FUNC-008).
    /// </summary>
    public enum NivelCriticidad
    {
        Baja,
        Media,
        Alta
    }
}
