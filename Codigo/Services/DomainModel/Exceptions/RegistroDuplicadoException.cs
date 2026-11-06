namespace Services.DomainModel.Exceptions
{
    /// <summary>Ya existe un registro con el mismo valor único (usuario, idioma, DNI...).</summary>
    public class RegistroDuplicadoException : ReglaNegocioException
    {
        public RegistroDuplicadoException(string mensaje) : base(mensaje)
        {
        }
    }
}
