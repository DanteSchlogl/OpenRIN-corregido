namespace Services.DomainModel
{
    /// <summary>
    /// Texto de localización (T05): una leyenda concreta en un idioma concreto, almacenada en
    /// la base de datos. La clave identifica la leyenda en el código y el valor es lo que ve
    /// el usuario.
    /// </summary>
    public class TextoLocalizacion
    {
        /// <summary>Identificador interno del registro.</summary>
        public int Id { get; set; }

        /// <summary>Código del idioma al que pertenece el texto.</summary>
        public string CodigoIdioma { get; set; } = string.Empty;

        /// <summary>Clave de la leyenda (por ejemplo, "login.ingresar").</summary>
        public string Clave { get; set; } = string.Empty;

        /// <summary>Valor visible de la leyenda en ese idioma.</summary>
        public string Valor { get; set; } = string.Empty;
    }
}
