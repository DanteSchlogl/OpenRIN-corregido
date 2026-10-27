namespace Services.DomainModel
{
    /// <summary>
    /// Idioma soportado por la aplicación (T05): vive en la base de datos y puede incorporarse
    /// o desactivarse desde la administración del sistema en cualquier momento.
    /// </summary>
    public class Idioma
    {
        /// <summary>Código del idioma (por ejemplo, "es", "en", "zh-CN").</summary>
        public string Codigo { get; set; } = string.Empty;

        /// <summary>Nombre para mostrar (por ejemplo, "Español (Latinoamérica)").</summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Indica si el idioma está disponible para su selección.</summary>
        public bool Activo { get; set; }

        /// <summary>Representación como texto del idioma (útil para registros y depuración).</summary>
        public override string ToString() => $"{Codigo} - {Nombre}";
    }
}
