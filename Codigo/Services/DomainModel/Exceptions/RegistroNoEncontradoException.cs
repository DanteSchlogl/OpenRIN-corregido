namespace Services.DomainModel.Exceptions
{
    /// <summary>El registro pedido no existe.</summary>
    public class RegistroNoEncontradoException : ReglaNegocioException
    {
        public RegistroNoEncontradoException(string mensaje) : base(mensaje)
        {
        }
    }
}
