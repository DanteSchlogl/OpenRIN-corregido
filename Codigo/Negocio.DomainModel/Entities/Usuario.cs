using Services.DomainModel.Validacion;

namespace Negocio.DomainModel
{
    /// <summary>
    /// Persona autenticada con un perfil específico (base del módulo de autenticación,
    /// REQ-ARQ-006). El registro técnico de credenciales (hash, intentos fallidos, idioma)
    /// vive en OpenRIN_Services; esta entidad representa la identidad de negocio, enlazada
    /// por NombreUsuario.
    /// </summary>
    public class Usuario
    {
        public int Id { get; set; }

        /// <summary>Nombre de usuario (igual al registrado en el módulo de seguridad).</summary>
        [Requerido]
        [Unico]
        public string NombreUsuario { get; set; } = string.Empty;

        [Requerido]
        public string NombreCompleto { get; set; } = string.Empty;

        /// <summary>Perfil de acceso: administrativo, medico, paciente, familiar o sysadmin.</summary>
        [Requerido]
        public string Perfil { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Telefono { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaAlta { get; set; } = DateTime.Now;
    }
}
