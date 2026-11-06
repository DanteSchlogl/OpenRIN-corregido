using System;

namespace Services.DomainModel.Exceptions
{
    /// <summary>Falta o es incorrecta una clave de configuración (cadena de conexión, clase de repositorio...).</summary>
    public class ConfiguracionInvalidaException : ReglaNegocioException
    {
        public ConfiguracionInvalidaException(string mensaje, Exception? innerException = null) : base(mensaje, innerException)
        {
        }
    }
}
