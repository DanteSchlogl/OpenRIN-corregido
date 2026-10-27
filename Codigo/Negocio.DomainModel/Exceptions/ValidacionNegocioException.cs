namespace Negocio.DomainModel.Exceptions
{
    /// <summary>
    /// Error de validación del negocio: agrupa los mensajes que explican por qué una
    /// operación fue rechazada (para mostrar al usuario y auditar en bitácora).
    /// </summary>
    public class ValidacionNegocioException : Exception
    {
        /// <summary>Mensajes de validación que originaron el rechazo.</summary>
        public List<string> Errores { get; }

        public ValidacionNegocioException(string mensaje) : base(mensaje)
        {
            Errores = new List<string> { mensaje };
        }

        public ValidacionNegocioException(List<string> errores) : base(string.Join(" | ", errores))
        {
            Errores = errores;
        }
    }
}
