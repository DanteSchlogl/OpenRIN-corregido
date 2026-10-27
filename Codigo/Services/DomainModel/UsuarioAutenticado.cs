namespace Services.DomainModel
{
    /// <summary>
    /// Sesión iniciada (REQ-ARQ-006): datos mínimos del usuario disponibles para toda la aplicación
    /// una vez autenticado.
    /// </summary>
    public class UsuarioAutenticado
    {
        public int Id { get; set; }

        public string NombreUsuario { get; set; } = string.Empty;

        public string NombreCompleto { get; set; } = string.Empty;

        public string Perfil { get; set; } = string.Empty;
    }
}
