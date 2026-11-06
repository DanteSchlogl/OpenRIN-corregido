using System;

namespace Services.DomainModel.Exceptions
{
    /// <summary>
    /// Error de acceso a datos (REQ-ARQ-004): envuelve errores técnicos (por ejemplo, SqlException)
    /// y expone un mensaje amigable, ocultando los detalles de conexión hacia las capas superiores.
    /// </summary>
    public class AccesoADatosException : ReglaNegocioException
    {
        public AccesoADatosException(string mensaje, Exception? innerException = null)
            : base(mensaje, innerException)
        {
        }
    }
}
