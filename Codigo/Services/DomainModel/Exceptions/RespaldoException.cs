using System;

namespace Services.DomainModel.Exceptions
{
    /// <summary>Falló un respaldo, una verificación o una restauración.</summary>
    public class RespaldoException : ReglaNegocioException
    {
        public RespaldoException(string mensaje, Exception? innerException = null) : base(mensaje, innerException)
        {
        }
    }
}
