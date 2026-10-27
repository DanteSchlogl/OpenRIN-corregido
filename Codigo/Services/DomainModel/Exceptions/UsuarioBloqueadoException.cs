using System;

namespace Services.DomainModel.Exceptions
{
    /// <summary>
    /// Cuenta bloqueada temporalmente por la política anti fuerza bruta (REQ-ARQ-006):
    /// 5 intentos fallidos consecutivos → bloqueo de 15 minutos.
    /// </summary>
    public class UsuarioBloqueadoException : Exception
    {
        public UsuarioBloqueadoException(string mensaje)
            : base(mensaje)
        {
        }
    }
}
