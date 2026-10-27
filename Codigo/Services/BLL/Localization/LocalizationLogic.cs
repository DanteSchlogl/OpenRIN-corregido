using System;
using System.Collections.Generic;
using System.Linq;
using Services.DAL.Implementations;
using Services.DAL.Interfaces;
using Services.DomainModel;

namespace Services.BLL.Localization
{
    /// <summary>
    /// Núcleo de internacionalización (T05): los idiomas y sus leyendas viven en la base de
    /// datos —sin hojas de recursos estáticos— y el cambio de idioma es dinámico, en caliente.
    /// Aplica el patrón observer: al cambiar el idioma (o editar las leyendas del idioma
    /// activo desde la administración) notifica a los suscriptores mediante el evento
    /// IdiomaCambiado. También permite incorporar idiomas y leyendas nuevas desde el sistema.
    /// </summary>
    internal static class LocalizationLogic
    {
        private const string IdiomaBase = "es";
        private static readonly object Candado = new object();
        private static readonly ILocalizacionRepository Repositorio = new SqlLocalizacionRepository();

        private static string _idiomaActual = IdiomaBase;
        private static Dictionary<string, string> _textos = new Dictionary<string, string>(StringComparer.Ordinal);
        private static List<Idioma> _idiomas = new List<Idioma>();
        private static bool _cargado;

        /// <summary>Observador (patrón observer): se dispara cuando cambia el idioma activo o
        /// se editan sus leyendas; las vistas suscriptas se actualizan en caliente.</summary>
        public static event Action? IdiomaCambiado;

        /// <summary>Código del idioma activo (por defecto "es").</summary>
        public static string IdiomaActual
        {
            get { AsegurarCarga(); return _idiomaActual; }
        }

        /// <summary>Idiomas registrados y activos, tal como están en la base de datos.</summary>
        public static IReadOnlyList<Idioma> Idiomas
        {
            get { AsegurarCarga(); return _idiomas; }
        }

        /// <summary>Códigos de los idiomas activos (compatibilidad con consumidores existentes).</summary>
        public static IReadOnlyList<string> IdiomasSoportados
        {
            get { AsegurarCarga(); return _idiomas.Select(i => i.Codigo).ToList(); }
        }

        /// <summary>
        /// Obtiene el texto asociado a una clave en el idioma activo. Si la clave no existe,
        /// devuelve la clave misma y registra una advertencia en la bitácora.
        /// </summary>
        public static string ObtenerTexto(string clave)
        {
            if (string.IsNullOrWhiteSpace(clave))
            {
                return string.Empty;
            }

            AsegurarCarga();
            if (_textos.TryGetValue(clave, out string? valor))
            {
                return valor;
            }

            BitacoraLogic.Registrar(LogLevel.Warning,
                $"Clave de localización inexistente: '{clave}' (idioma {_idiomaActual}).",
                capa: "Localización");
            return clave;
        }

        /// <summary>Cambia el idioma activo en caliente y notifica a los observadores.</summary>
        public static void EstablecerIdioma(string codigoIdioma)
        {
            AsegurarCarga();
            if (string.IsNullOrWhiteSpace(codigoIdioma) || !_idiomas.Any(i => i.Codigo == codigoIdioma))
            {
                throw new ArgumentException(
                    $"Idioma no soportado: '{codigoIdioma}'. Idiomas disponibles: {string.Join(", ", _idiomas.Select(i => i.Codigo))}.");
            }

            bool cambio = !string.Equals(_idiomaActual, codigoIdioma, StringComparison.Ordinal);
            lock (Candado)
            {
                _idiomaActual = codigoIdioma;
                RecargarTextos();
            }

            BitacoraLogic.Registrar(LogLevel.Info,
                $"Idioma de la aplicación cambiado a '{codigoIdioma}'.",
                capa: "Localización");

            if (cambio)
            {
                IdiomaCambiado?.Invoke();
            }
        }

        // ------------------------------------------------------------------ administración
        // T05: el sistema incorpora idiomas y leyendas nuevas sin recompilar la aplicación.

        /// <summary>Devuelve todos los textos (clave y valor) del idioma indicado.</summary>
        public static List<TextoLocalizacion> ObtenerTextosDe(string codigoIdioma)
            => Repositorio.ObtenerTextos(codigoIdioma);

        /// <summary>Registra un idioma nuevo en la base y refresca la lista de idiomas.</summary>
        public static void RegistrarIdioma(string codigo, string nombre)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                throw new ArgumentException("El código del idioma es obligatorio.");
            }
            if (string.IsNullOrWhiteSpace(nombre))
            {
                throw new ArgumentException("El nombre del idioma es obligatorio.");
            }
            string limpio = codigo.Trim();
            if (Repositorio.ObtenerIdiomas().Any(i => string.Equals(i.Codigo, limpio, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"Ya existe un idioma con el código '{limpio}'.");
            }

            Repositorio.RegistrarIdioma(limpio, nombre.Trim());
            lock (Candado)
            {
                RecargarIdiomas();
            }
            BitacoraLogic.Registrar(LogLevel.Info,
                $"Idioma registrado: '{limpio}' ({nombre.Trim()}).",
                capa: "Localización");
        }

        /// <summary>Elimina un idioma (y sus leyendas). El idioma base y el activo no se eliminan.</summary>
        public static void EliminarIdioma(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                throw new ArgumentException("El código del idioma es obligatorio.");
            }
            if (string.Equals(codigo, IdiomaBase, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("El idioma base no puede eliminarse.");
            }
            if (string.Equals(codigo, _idiomaActual, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("No puede eliminarse el idioma que está activo en este momento.");
            }

            Repositorio.EliminarIdioma(codigo);
            lock (Candado)
            {
                RecargarIdiomas();
            }
            BitacoraLogic.Registrar(LogLevel.Info,
                $"Idioma eliminado: '{codigo}'.",
                capa: "Localización");
        }

        /// <summary>Inserta o actualiza una leyenda; si es del idioma activo, notifica al instante.</summary>
        public static void GuardarTexto(string codigoIdioma, string clave, string valor)
        {
            if (string.IsNullOrWhiteSpace(codigoIdioma))
            {
                throw new ArgumentException("El código del idioma es obligatorio.");
            }
            if (string.IsNullOrWhiteSpace(clave))
            {
                throw new ArgumentException("La clave de la leyenda es obligatoria.");
            }

            Repositorio.GuardarTexto(codigoIdioma, clave.Trim(), valor ?? string.Empty);
            bool esActivo = string.Equals(codigoIdioma, _idiomaActual, StringComparison.Ordinal);
            if (esActivo)
            {
                lock (Candado)
                {
                    RecargarTextos();
                }
            }
            BitacoraLogic.Registrar(LogLevel.Info,
                $"Leyenda '{clave.Trim()}' guardada para el idioma '{codigoIdioma}'.",
                capa: "Localización");
            if (esActivo)
            {
                IdiomaCambiado?.Invoke();
            }
        }

        /// <summary>Elimina una leyenda de un idioma; si es del idioma activo, notifica al instante.</summary>
        public static void EliminarTexto(string codigoIdioma, string clave)
        {
            Repositorio.EliminarTexto(codigoIdioma, clave);
            bool esActivo = string.Equals(codigoIdioma, _idiomaActual, StringComparison.Ordinal);
            if (esActivo)
            {
                lock (Candado)
                {
                    RecargarTextos();
                }
                IdiomaCambiado?.Invoke();
            }
            BitacoraLogic.Registrar(LogLevel.Info,
                $"Leyenda '{clave}' eliminada del idioma '{codigoIdioma}'.",
                capa: "Localización");
        }

        // ------------------------------------------------------------------ internos

        /// <summary>Carga perezosa del estado de localización desde la base (segura ante concurrencia).</summary>
        private static void AsegurarCarga()
        {
            if (_cargado)
            {
                return;
            }
            lock (Candado)
            {
                if (_cargado)
                {
                    return;
                }
                try
                {
                    RecargarIdiomas();
                    RecargarTextos();
                    _cargado = true;
                }
                catch (Exception ex)
                {
                    // Sin base disponible: se opera con la clave como texto de respaldo.
                    BitacoraLogic.Registrar(LogLevel.Error,
                        "No se pudo cargar la localización desde la base de datos: " + ex.Message,
                        capa: "Localización");
                }
            }
        }

        private static void RecargarIdiomas()
            => _idiomas = Repositorio.ObtenerIdiomas().Where(i => i.Activo).ToList();

        private static void RecargarTextos()
            => _textos = Repositorio.ObtenerTextos(_idiomaActual)
                .ToDictionary(t => t.Clave, t => t.Valor, StringComparer.Ordinal);
    }
}
