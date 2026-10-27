using System.Collections.Generic;
using Services.BLL;
using Services.DomainModel;

namespace Services.Facade
{
    /// <summary>
    /// Fachada pública del módulo de seguridad (REQ-ARQ-006): autenticación centralizada,
    /// verificación de permisos, registro de usuarios y listado para administración.
    /// Consumida por el formulario de login del Negocio, el portal de pacientes y la pantalla
    /// de administración del sistema.
    /// </summary>
    public static class SeguridadService
    {
        /// <summary>Autentica un usuario y, si las credenciales son válidas, devuelve la sesión iniciada.</summary>
        public static UsuarioAutenticado Autenticar(string usuario, string contrasena)
            => SeguridadLogic.Autenticar(usuario, contrasena);

        /// <summary>Indica si el usuario posee el permiso indicado (consulta roles y permisos).</summary>
        public static bool TienePermiso(int idUsuario, string permiso)
            => SeguridadLogic.TienePermiso(idUsuario, permiso);

        /// <summary>Registra un nuevo usuario con contraseña hasheada (PBKDF2 + sal). Devuelve el Id generado.</summary>
        public static int RegistrarUsuario(
            string nombreUsuario,
            string nombreCompleto,
            string contrasena,
            string perfil,
            string? email = null,
            string? preguntaSeguridad = null,
            string? respuestaSeguridad = null)
            => SeguridadLogic.RegistrarUsuario(nombreUsuario, nombreCompleto, contrasena, perfil, email, preguntaSeguridad, respuestaSeguridad);

        /// <summary>Indica si ya existe un usuario registrado con ese nombre.</summary>
        public static bool ExisteNombreUsuario(string nombreUsuario)
            => SeguridadLogic.ExisteNombreUsuario(nombreUsuario);

        /// <summary>Habilita o deshabilita las credenciales de un usuario (auditado en bitácora).</summary>
        public static void CambiarEstadoUsuario(string nombreUsuario, bool activo, string? motivo = null)
            => SeguridadLogic.CambiarEstadoUsuario(nombreUsuario, activo, motivo);

        /// <summary>Devuelve la pregunta de seguridad del usuario, o null si no está disponible.</summary>
        public static string? ObtenerPreguntaSeguridad(string nombreUsuario)
            => SeguridadLogic.ObtenerPreguntaSeguridad(nombreUsuario);

        /// <summary>Restablece la contraseña validando la respuesta de seguridad (auditado en bitácora).</summary>
        public static void RecuperarContrasena(string nombreUsuario, string respuestaSeguridad, string nuevaContrasena)
            => SeguridadLogic.RecuperarContrasena(nombreUsuario, respuestaSeguridad, nuevaContrasena);

        /// <summary>Lista los usuarios del sistema (sin datos sensibles) para la pantalla de administración.</summary>
        public static List<UsuarioListado> ObtenerUsuarios()
            => SeguridadLogic.ObtenerUsuarios();

        /// <summary>Perfiles conocidos por el sistema (T04) para la administración de permisos.</summary>
        public static List<string> ObtenerPerfiles()
            => SeguridadLogic.ObtenerPerfiles();

        /// <summary>Códigos de permisos asignados a un perfil.</summary>
        public static List<string> ObtenerPermisosDePerfil(string perfil)
            => SeguridadLogic.ObtenerPermisosDePerfil(perfil);

        /// <summary>Catálogo de permisos como árbol composite, para la vista en TreeView (T04).</summary>
        public static PermisoCompuesto ObtenerArbolPermisos()
            => SeguridadLogic.ObtenerArbolPermisos();

        /// <summary>Guarda (reemplaza) las asignaciones de un perfil — asignación rápida auditada.</summary>
        public static void GuardarPermisosDePerfil(string perfil, List<string> codigos)
            => SeguridadLogic.GuardarPermisosDePerfil(perfil, codigos);
    }
}
