using System;

namespace Services.DomainModel
{
    /// <summary>
    /// Usuario del sistema (tabla Usuarios de la base Services). Incluye el estado de acceso
    /// utilizado por la política anti fuerza bruta (REQ-ARQ-006) y el idioma preferido (REQ-ARQ-001).
    /// </summary>
    public class Usuario
    {
        public int Id { get; set; }

        public string NombreUsuario { get; set; } = string.Empty;

        public string NombreCompleto { get; set; } = string.Empty;

        /// <summary>Hash de la contraseña (formato "sal.iteraciones.hash", PBKDF2-SHA256). Nunca se almacena texto plano.</summary>
        public string HashPassword { get; set; } = string.Empty;

        public string Perfil { get; set; } = string.Empty;

        public string? Email { get; set; }

        /// <summary>Pregunta de seguridad para la recuperación de contraseña.</summary>
        public string? PreguntaSeguridad { get; set; }

        /// <summary>Hash de la respuesta de seguridad (mismo formato que HashPassword).</summary>
        public string? RespuestaHash { get; set; }

        /// <summary>Idioma preferido del usuario (código de cultura, ej. "es", "en", "zh-CN").</summary>
        public string? Idioma { get; set; }

        public bool Activo { get; set; } = true;

        /// <summary>Intentos fallidos consecutivos desde el último acceso exitoso.</summary>
        public int IntentosFallidos { get; set; }

        /// <summary>Si tiene valor futuro, la cuenta está bloqueada temporalmente hasta esa fecha.</summary>
        public DateTime? BloqueadoHasta { get; set; }
    }
}
