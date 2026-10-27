using System;

namespace Services.Facade
{
    /// <summary>
    /// Sesión activa del sistema (patrón Singleton, requisito T02): instancia única que
    /// conserva el usuario autenticado y expone utilidades de autorización. La creación es
    /// perezosa y sincronizada con lock (segura ante accesos concurrentes: base preparada
    /// para la incorporación de procesamiento multi-hilo).
    /// </summary>
    public sealed class SesionActual
    {
        private static readonly object Candado = new object();
        private static SesionActual? _instancia;

        /// <summary>Instancia única de la sesión (creación perezosa segura ante concurrencia).</summary>
        public static SesionActual Instancia
        {
            get
            {
                if (_instancia == null)
                {
                    lock (Candado)
                    {
                        if (_instancia == null)
                        {
                            _instancia = new SesionActual();
                        }
                    }
                }
                return _instancia;
            }
        }

        private SesionActual()
        {
        }

        /// <summary>Usuario autenticado de la sesión, o null si aún no se inició sesión.</summary>
        public Services.DomainModel.UsuarioAutenticado? Usuario { get; private set; }

        /// <summary>Indica si hay una sesión iniciada en este proceso.</summary>
        public bool HaySesionIniciada => Usuario != null;

        /// <summary>Nombre de usuario activo (cadena vacía si no hay sesión).</summary>
        public string NombreUsuario => Usuario?.NombreUsuario ?? string.Empty;

        /// <summary>Registra el usuario autenticado (lo invoca el flujo de log-in exitoso).</summary>
        public void Iniciar(Services.DomainModel.UsuarioAutenticado usuario)
        {
            Usuario = usuario ?? throw new ArgumentNullException(nameof(usuario));
        }

        /// <summary>Cierra la sesión (log-out): limpia el usuario activo.</summary>
        public void Cerrar()
        {
            Usuario = null;
        }

        /// <summary>Indica si el usuario activo posee el permiso indicado (false si no hay sesión).</summary>
        public bool TienePermiso(string permiso)
            => Usuario != null && SeguridadService.TienePermiso(Usuario.Id, permiso);
    }
}
