namespace Services.DomainModel.Exceptions
{
    /// <summary>Un dato recibido no cumple una regla (vacío, fuera de rango, formato incorrecto).</summary>
    public class DatoInvalidoException : ReglaNegocioException
    {
        /// <summary>Nombre del dato rechazado, si se conoce.</summary>
        public string? Campo { get; }

        public DatoInvalidoException(string mensaje, string? campo = null) : base(mensaje) => Campo = campo;
    }
}
