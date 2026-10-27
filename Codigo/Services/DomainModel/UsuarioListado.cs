using System;

namespace Services.DomainModel
{
    /// <summary>
    /// Vista de usuario para la administración del sistema (pantalla Sistema): expone solo
    /// datos no sensibles (sin hash de contraseña ni pregunta de seguridad).
    /// </summary>
    public record UsuarioListado(
        int Id,
        string NombreUsuario,
        string NombreCompleto,
        string Perfil,
        string? Email,
        bool Activo,
        int IntentosFallidos,
        DateTime? BloqueadoHasta);
}
