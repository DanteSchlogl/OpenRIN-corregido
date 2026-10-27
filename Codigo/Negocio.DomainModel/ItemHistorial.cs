namespace Negocio.DomainModel
{
    /// <summary>
    /// Registro del historial clínico unificado de un paciente (REQ-FUNC-013):
    /// una medición de RIN, una alerta o un evento adverso, presentados cronológicamente.
    /// </summary>
    public class ItemHistorial
    {
        public DateTime Fecha { get; set; }

        /// <summary>Tipo de registro: "RIN", "Alerta" o "Evento adverso".</summary>
        public string Tipo { get; set; } = string.Empty;

        /// <summary>Descripción legible del registro.</summary>
        public string Descripcion { get; set; } = string.Empty;
    }
}
