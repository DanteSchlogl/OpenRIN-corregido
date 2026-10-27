using Services.DomainModel.Validacion;

namespace Negocio.DomainModel
{
    /// <summary>
    /// Entidad aseguradora que cubre al paciente (obra social / prepaga).
    /// </summary>
    public class ObraSocial
    {
        public int Id { get; set; }

        /// <summary>Nombre de la obra social (único).</summary>
        [Requerido]
        [Unico]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Indica si la obra social está disponible para asignación.</summary>
        public bool Activo { get; set; } = true;
    }
}
