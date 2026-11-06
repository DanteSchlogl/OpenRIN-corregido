using System;

namespace Services.DomainModel.Exceptions
{
    /// <summary>
    /// Base de las excepciones controladas del sistema: su mensaje está pensado para el usuario,
    /// por eso la pantalla lo muestra tal cual en lugar de «Error inesperado».
    /// </summary>
    public class ReglaNegocioException : Exception
    {
        public ReglaNegocioException(string mensaje, Exception? innerException = null)
            : base(mensaje, innerException)
        {
        }
    }
}
