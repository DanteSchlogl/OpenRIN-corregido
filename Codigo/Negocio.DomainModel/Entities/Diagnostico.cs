using Services.DomainModel.Validacion;

namespace Negocio.DomainModel
{
    /// <summary>
    /// Catálogo de patologías o indicaciones de anticoagulación usadas en las
    /// historias clínicas (REQ-FUNC-002 / REQ-FUNC-012).
    /// </summary>
    public class Diagnostico
    {
        public int Id { get; set; }

        /// <summary>Nombre de la patología o indicación (único).</summary>
        [Requerido]
        [Unico]
        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        /// <summary>Indica si el diagnóstico está disponible para asignación.</summary>
        public bool Activo { get; set; } = true;
    }
}
