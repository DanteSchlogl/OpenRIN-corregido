using System;
using System.Collections.Generic;
using Services.BLL.Infrastructure;
using Services.DAL.Implementations;
using Services.DAL.Interfaces;
using Services.DomainModel;
using Services.DomainModel.Exceptions;

namespace Services.BLL
{
    /// <summary>
    /// Lógica de seguridad (REQ-ARQ-006): autenticación centralizada, autorización por permisos
    /// y política anti fuerza bruta (5 intentos fallidos consecutivos → bloqueo de 15 minutos).
    /// Auditoría completa en bitácora de accesos exitosos y fallidos.
    /// </summary>
    internal static class SeguridadLogic
    {
        private const int MaxIntentosFallidos = 5; // REQ-ARQ-006
        private const int MinutosBloqueo = 15;     // REQ-ARQ-006
        private const int LargoMinimoContrasena = 8;

        private static readonly IUsuarioRepository RepositorioUsuarios = new SqlUsuarioRepository();
        private static readonly IPermisoRepository RepositorioPermisos = new SqlPermisoRepository();

        public static UsuarioAutenticado Autenticar(string nombreUsuario, string contrasena)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(contrasena))
            {
                throw new CredencialesInvalidasException("Usuario o contraseña incorrectos.");
            }

            Usuario? usuario = RepositorioUsuarios.ObtenerPorNombreUsuario(nombreUsuario.Trim());

            if (usuario == null)
            {
                BitacoraLogic.Registrar(LogLevel.Warning,
                    $"Inicio de sesión fallido: usuario inexistente '{nombreUsuario}'.",
                    capa: "Seguridad");
                throw new CredencialesInvalidasException("Usuario o contraseña incorrectos.");
            }

            if (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value > DateTime.Now)
            {
                int minutos = (int)Math.Ceiling((usuario.BloqueadoHasta.Value - DateTime.Now).TotalMinutes);
                BitacoraLogic.Registrar(LogLevel.Warning,
                    $"Intento de acceso a cuenta bloqueada '{usuario.NombreUsuario}' (restan {minutos} min).",
                    usuario: usuario.NombreUsuario, capa: "Seguridad");
                throw new UsuarioBloqueadoException(
                    $"La cuenta está bloqueada temporalmente por seguridad. Intente nuevamente en {minutos} minuto(s).");
            }

            if (!usuario.Activo)
            {
                BitacoraLogic.Registrar(LogLevel.Warning,
                    $"Intento de acceso con usuario deshabilitado '{usuario.NombreUsuario}'.",
                    usuario: usuario.NombreUsuario, capa: "Seguridad");
                throw new CredencialesInvalidasException("El usuario no está habilitado. Contacte al administrador.");
            }

            if (!CryptographyLogic.VerificarHash(contrasena, usuario.HashPassword))
            {
                RegistrarFallo(usuario);
                throw new CredencialesInvalidasException("Usuario o contraseña incorrectos.");
            }

            // Éxito: se resetea el contador y se libera cualquier bloqueo vencido.
            if (usuario.IntentosFallidos != 0 || usuario.BloqueadoHasta.HasValue)
            {
                RepositorioUsuarios.ActualizarAcceso(usuario.Id, 0, null);
            }

            BitacoraLogic.Registrar(LogLevel.Info,
                $"Inicio de sesión exitoso de '{usuario.NombreUsuario}' (perfil {usuario.Perfil}).",
                usuario: usuario.NombreUsuario, capa: "Seguridad");

            return new UsuarioAutenticado
            {
                Id = usuario.Id,
                NombreUsuario = usuario.NombreUsuario,
                NombreCompleto = usuario.NombreCompleto,
                Perfil = usuario.Perfil
            };
        }

        /// <summary>
        /// Indica si el usuario posee el permiso indicado (T04). El perfil del usuario se arma
        /// como un permiso compuesto (patrón composite) con los códigos asignados en la base:
        /// la verificación es recursiva por el árbol de permisos. El administrador del sistema
        /// posee acceso total (criterio de diseño, REQ-ARQ-006).
        /// </summary>
        public static bool TienePermiso(int idUsuario, string permiso)
        {
            if (string.IsNullOrWhiteSpace(permiso))
            {
                return false;
            }

            Usuario? usuario = RepositorioUsuarios.ObtenerPorId(idUsuario);
            if (usuario == null || !usuario.Activo)
            {
                return false;
            }

            if (string.Equals(usuario.Perfil, "sysadmin", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            List<string> asignados = RepositorioPermisos.ObtenerAsignacionesDePerfil(usuario.Perfil);
            if (asignados.Count == 0)
            {
                return false;
            }

            List<PermisoFila> catalogo = RepositorioPermisos.ObtenerCatalogo();
            PermisoCompuesto perfilCompuesto = new(usuario.Perfil, usuario.Perfil);
            foreach (string codigo in asignados)
            {
                Permiso? nodo = ConstruirNodo(codigo, catalogo);
                if (nodo != null)
                {
                    perfilCompuesto.Agregar(nodo);
                }
            }
            return perfilCompuesto.Incluye(permiso);
        }

        /// <summary>Perfiles conocidos por el sistema (con asignaciones o usados por usuarios).</summary>
        public static List<string> ObtenerPerfiles()
            => RepositorioPermisos.ObtenerPerfiles();

        /// <summary>Códigos de permisos asignados directamente a un perfil.</summary>
        public static List<string> ObtenerPermisosDePerfil(string perfil)
            => RepositorioPermisos.ObtenerAsignacionesDePerfil(perfil);

        /// <summary>
        /// Devuelve el catálogo de permisos como árbol composite (raíz "CATALOGO" con los
        /// grupos como hijos), para la administración en un control TreeView (T04).
        /// </summary>
        public static PermisoCompuesto ObtenerArbolPermisos()
        {
            List<PermisoFila> catalogo = RepositorioPermisos.ObtenerCatalogo();
            PermisoCompuesto raiz = new("CATALOGO", "Catálogo de permisos");
            foreach (PermisoFila fila in catalogo.Where(f => f.CodigoPadre == null))
            {
                Permiso? nodo = ConstruirNodo(fila.Codigo, catalogo);
                if (nodo != null)
                {
                    raiz.Agregar(nodo);
                }
            }
            return raiz;
        }

        /// <summary>Reemplaza las asignaciones de un perfil (asignación rápida) y audita el cambio.</summary>
        public static void GuardarPermisosDePerfil(string perfil, List<string> codigos)
        {
            if (string.IsNullOrWhiteSpace(perfil))
            {
                throw new ArgumentException("El perfil es obligatorio.");
            }
            RepositorioPermisos.ReemplazarAsignaciones(perfil, codigos ?? new List<string>());
            BitacoraLogic.Registrar(LogLevel.Info,
                $"Permisos del perfil '{perfil}' actualizados ({(codigos?.Count ?? 0)} asignación/es).",
                capa: "Seguridad");
        }

        /// <summary>Construye recursivamente un nodo del árbol de permisos a partir del catálogo.</summary>
        private static Permiso? ConstruirNodo(string codigo, List<PermisoFila> catalogo)
        {
            PermisoFila? fila = catalogo.FirstOrDefault(f => string.Equals(f.Codigo, codigo, StringComparison.OrdinalIgnoreCase));
            if (fila == null)
            {
                return null;
            }
            if (!string.Equals(fila.Tipo, "C", StringComparison.OrdinalIgnoreCase))
            {
                return new PermisoAtomico(fila.Codigo, fila.Nombre);
            }
            PermisoCompuesto compuesto = new(fila.Codigo, fila.Nombre);
            foreach (PermisoFila hijo in catalogo.Where(f => string.Equals(f.CodigoPadre, fila.Codigo, StringComparison.OrdinalIgnoreCase)))
            {
                Permiso? nodoHijo = ConstruirNodo(hijo.Codigo, catalogo);
                if (nodoHijo != null)
                {
                    compuesto.Agregar(nodoHijo);
                }
            }
            return compuesto;
        }

        public static int RegistrarUsuario(
            string nombreUsuario,
            string nombreCompleto,
            string contrasena,
            string perfil,
            string? email = null,
            string? preguntaSeguridad = null,
            string? respuestaSeguridad = null)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario))
            {
                throw new ArgumentException("El nombre de usuario es obligatorio.");
            }
            if (string.IsNullOrWhiteSpace(nombreCompleto))
            {
                throw new ArgumentException("El nombre completo es obligatorio.");
            }
            if (string.IsNullOrWhiteSpace(perfil))
            {
                throw new ArgumentException("El perfil es obligatorio.");
            }
            if (string.IsNullOrWhiteSpace(contrasena) || contrasena.Length < LargoMinimoContrasena)
            {
                throw new ArgumentException($"La contraseña debe tener al menos {LargoMinimoContrasena} caracteres.");
            }
            if (RepositorioUsuarios.ExisteNombreUsuario(nombreUsuario.Trim()))
            {
                throw new ArgumentException($"Ya existe un usuario con el nombre '{nombreUsuario}'.");
            }

            Usuario usuario = new Usuario
            {
                NombreUsuario = nombreUsuario.Trim(),
                NombreCompleto = nombreCompleto.Trim(),
                HashPassword = CryptographyLogic.Hashear(contrasena),
                Perfil = perfil.Trim(),
                Email = email,
                PreguntaSeguridad = preguntaSeguridad,
                RespuestaHash = string.IsNullOrWhiteSpace(respuestaSeguridad)
                    ? null
                    : CryptographyLogic.Hashear(respuestaSeguridad),
                Activo = true,
                IntentosFallidos = 0
            };

            int id = RepositorioUsuarios.Registrar(usuario);
            BitacoraLogic.Registrar(LogLevel.Info,
                $"Usuario registrado '{usuario.NombreUsuario}' (perfil {usuario.Perfil}).",
                capa: "Seguridad");
            return id;
        }

        /// <summary>Indica si ya existe un usuario registrado con ese nombre.</summary>
        public static bool ExisteNombreUsuario(string nombreUsuario)
            => !string.IsNullOrWhiteSpace(nombreUsuario) && RepositorioUsuarios.ExisteNombreUsuario(nombreUsuario.Trim());

        /// <summary>
        /// Habilita o deshabilita las credenciales de un usuario (por ejemplo, al dar de baja
        /// o reactivar una cuenta del portal de pacientes). Queda auditado en bitácora.
        /// </summary>
        public static void CambiarEstadoUsuario(string nombreUsuario, bool activo, string? motivo = null)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario))
            {
                throw new ArgumentException("El nombre de usuario es obligatorio.");
            }

            Usuario? usuario = RepositorioUsuarios.ObtenerPorNombreUsuario(nombreUsuario.Trim());
            if (usuario == null)
            {
                throw new ArgumentException($"No existe el usuario '{nombreUsuario}'.");
            }

            if (usuario.Activo == activo)
            {
                return; // sin cambios
            }

            RepositorioUsuarios.ActualizarEstado(usuario.NombreUsuario, activo);
            BitacoraLogic.Registrar(LogLevel.Info,
                $"Usuario '{usuario.NombreUsuario}' {(activo ? "habilitado" : "deshabilitado")}" +
                (string.IsNullOrWhiteSpace(motivo) ? "." : $": {motivo}"),
                capa: "Seguridad");
        }

        /// <summary>
        /// Lista los usuarios del sistema para la pantalla de administración, exponiendo solo
        /// datos no sensibles (sin hash de contraseña ni pregunta de seguridad).
        /// </summary>
        public static List<UsuarioListado> ObtenerUsuarios()
        {
            var listado = new List<UsuarioListado>();
            foreach (Usuario usuario in RepositorioUsuarios.ObtenerTodos())
            {
                listado.Add(new UsuarioListado(
                    usuario.Id,
                    usuario.NombreUsuario,
                    usuario.NombreCompleto,
                    usuario.Perfil,
                    usuario.Email,
                    usuario.Activo,
                    usuario.IntentosFallidos,
                    usuario.BloqueadoHasta));
            }
            return listado;
        }

        /// <summary>
        /// Devuelve la pregunta de seguridad del usuario (flujo de recuperación de contraseña),
        /// o null si el usuario no existe, está inactivo o no la tiene configurada
        /// (sin revelar cuál de los casos aplica).
        /// </summary>
        public static string? ObtenerPreguntaSeguridad(string nombreUsuario)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario))
            {
                return null;
            }
            Usuario? usuario = RepositorioUsuarios.ObtenerPorNombreUsuario(nombreUsuario.Trim());
            if (usuario == null || !usuario.Activo || string.IsNullOrWhiteSpace(usuario.PreguntaSeguridad)
                || string.IsNullOrWhiteSpace(usuario.RespuestaHash))
            {
                return null;
            }
            return usuario.PreguntaSeguridad;
        }

        /// <summary>
        /// Restablece la contraseña validando la respuesta de seguridad (hash) y audita la
        /// recuperación en bitácora. La recuperación también libera bloqueos por intentos.
        /// </summary>
        public static void RecuperarContrasena(string nombreUsuario, string respuestaSeguridad, string nuevaContrasena)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario))
            {
                throw new CredencialesInvalidasException("Usuario o contraseña incorrectos.");
            }
            if (string.IsNullOrWhiteSpace(nuevaContrasena) || nuevaContrasena.Length < LargoMinimoContrasena)
            {
                throw new ArgumentException($"La contraseña debe tener al menos {LargoMinimoContrasena} caracteres.");
            }

            Usuario? usuario = RepositorioUsuarios.ObtenerPorNombreUsuario(nombreUsuario.Trim());
            if (usuario == null || !usuario.Activo || string.IsNullOrWhiteSpace(usuario.PreguntaSeguridad)
                || string.IsNullOrWhiteSpace(usuario.RespuestaHash))
            {
                throw new CredencialesInvalidasException("No se pudo validar la recuperación para ese usuario.");
            }

            if (string.IsNullOrWhiteSpace(respuestaSeguridad)
                || !CryptographyLogic.VerificarHash(respuestaSeguridad.Trim(), usuario.RespuestaHash))
            {
                BitacoraLogic.Registrar(LogLevel.Warning,
                    $"Recuperación de contraseña fallida para '{usuario.NombreUsuario}': respuesta de seguridad incorrecta.",
                    usuario: usuario.NombreUsuario, capa: "Seguridad");
                throw new CredencialesInvalidasException("La respuesta de seguridad no es correcta.");
            }

            RepositorioUsuarios.ActualizarPassword(usuario.Id, CryptographyLogic.Hashear(nuevaContrasena));
            RepositorioUsuarios.ActualizarAcceso(usuario.Id, 0, null);
            BitacoraLogic.Registrar(LogLevel.Info,
                $"Contraseña restablecida para '{usuario.NombreUsuario}' mediante pregunta de seguridad.",
                usuario: usuario.NombreUsuario, capa: "Seguridad");
        }

        private static void RegistrarFallo(Usuario usuario)
        {
            int intentos = usuario.IntentosFallidos + 1;
            DateTime? bloqueo = null;

            if (intentos >= MaxIntentosFallidos)
            {
                bloqueo = DateTime.Now.AddMinutes(MinutosBloqueo);
                intentos = 0; // el bloqueo temporal reemplaza al contador
                BitacoraLogic.Registrar(LogLevel.Warning,
                    $"Cuenta '{usuario.NombreUsuario}' BLOQUEADA por {MinutosBloqueo} minutos tras {MaxIntentosFallidos} intentos fallidos.",
                    usuario: usuario.NombreUsuario, capa: "Seguridad");
            }
            else
            {
                BitacoraLogic.Registrar(LogLevel.Warning,
                    $"Inicio de sesión fallido de '{usuario.NombreUsuario}' (intento {intentos}/{MaxIntentosFallidos}).",
                    usuario: usuario.NombreUsuario, capa: "Seguridad");
            }

            RepositorioUsuarios.ActualizarAcceso(usuario.Id, intentos, bloqueo);
        }
    }
}
