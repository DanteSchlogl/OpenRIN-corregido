using System;

namespace Services.DomainModel.Exceptions
{
    /// <summary>
    /// Credenciales inválidas (REQ-ARQ-006): usuario inexistente, contraseña incorrecta
    /// o usuario deshabilitado. Mensaje amigable, sin revelar detalles internos.
    /// </summary>
    public class CredencialesInvalidasException : Exception
    {
        public CredencialesInvalidasException(string mensaje)
            : base(mensaje)
        {
        }
    }
}
