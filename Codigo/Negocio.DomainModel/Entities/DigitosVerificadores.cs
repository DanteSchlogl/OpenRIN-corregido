using Services.DomainModel.Validacion;

namespace Negocio.DomainModel
{
    /// <summary>
    /// Dígito verificador vertical (DVV) por tabla (integridad de datos): firma SHA-256 del
    /// conjunto de DVH de todas las filas de una tabla, recalculada en cada escritura.
    /// </summary>
    public class DigitosVerificadores
    {
        public int Id { get; set; }

        /// <summary>Nombre físico de la tabla cubierta (único).</summary>
        [Requerido]
        public string NombreTabla { get; set; } = string.Empty;

        /// <summary>Firma vertical vigente de la tabla.</summary>
        [Requerido]
        public string DVV { get; set; } = string.Empty;

        public DateTime FechaCalculo { get; set; } = DateTime.Now;
    }
}
