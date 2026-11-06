using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using Services.DAL.Factory;
using Services.DAL.Interfaces;
using Services.DomainModel;
using Services.DomainModel.Exceptions;

namespace Services.BLL.Localization
{
    /// <summary>
    /// Núcleo de internacionalización (T05). Los textos de los idiomas que se entregan (es, en, zh-CN) están en
    /// archivos .resx: Textos.resx es el idioma base y cada idioma tiene el suyo (Textos.en.resx, Textos.zh-CN.resx),
    /// que se compilan en ensamblados satélite, una carpeta por cultura junto al programa. La base de datos
    /// (tablas Idiomas y Textos) agrega idiomas nuevos y permite corregir leyendas sin recompilar: sus textos
    /// pisan a los del .resx. El cambio de idioma es dinámico, en caliente.
    /// Aplica el patrón observer: al cambiar el idioma (o editar las leyendas del idioma
    /// activo desde la administración) notifica a los suscriptores mediante el evento
    /// IdiomaCambiado.
    /// </summary>
    internal static class LocalizationLogic
    {
        private const string IdiomaBase = "es";
        private static readonly object Candado = new object();
        private static readonly ILocalizacionRepository Repositorio = RepositorioFactory.Crear<ILocalizacionRepository>();
        private static readonly ResourceManager Recursos = new ResourceManager("Services.BLL.Localization.Textos", typeof(LocalizationLogic).Assembly);

        /// <summary>Idiomas que se entregan compilados como recursos (sirven aunque la base no esté disponible).</summary>
        private static readonly (string Codigo, string Nombre)[] Embebidos =
        {
            ("es", "Español (Latinoamérica)"), ("en", "English (US)"), ("zh-CN", "中文（简体）")
        };

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
            if (!_cargado && string.Equals(codigoIdioma, IdiomaBase, StringComparison.Ordinal))
            {
                // Sin base de datos disponible el idioma base se acepta igual: los textos se
                // muestran con su clave y la carga se reintenta en la próxima consulta.
                lock (Candado)
                {
                    _idiomaActual = IdiomaBase;
                }
                return;
            }
            if (string.IsNullOrWhiteSpace(codigoIdioma) || !_idiomas.Any(i => i.Codigo == codigoIdioma))
            {
                throw new DatoInvalidoException(
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
        {
            Dictionary<string, string> embebidos = LeerEmbebidos(codigoIdioma);
            Dictionary<string, TextoLocalizacion> corregidos = LeerDeLaBase(codigoIdioma)
                .ToDictionary(t => t.Clave, t => t, StringComparer.Ordinal);

            var resultado = new List<TextoLocalizacion>();
            foreach (KeyValuePair<string, string> par in embebidos)
            {
                corregidos.TryGetValue(par.Key, out TextoLocalizacion? pisado);
                resultado.Add(new TextoLocalizacion
                {
                    Id = pisado?.Id ?? 0,
                    CodigoIdioma = codigoIdioma,
                    Clave = par.Key,
                    Valor = pisado?.Valor ?? par.Value
                });
            }
            resultado.AddRange(corregidos.Values.Where(t => !embebidos.ContainsKey(t.Clave)));
            return resultado.OrderBy(t => t.Clave, StringComparer.Ordinal).ToList();
        }

        /// <summary>Registra un idioma nuevo en la base y refresca la lista de idiomas.</summary>
        public static void RegistrarIdioma(string codigo, string nombre)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                throw new DatoInvalidoException("El código del idioma es obligatorio.");
            }
            if (string.IsNullOrWhiteSpace(nombre))
            {
                throw new DatoInvalidoException("El nombre del idioma es obligatorio.");
            }
            string limpio = codigo.Trim();
            if (Repositorio.ObtenerIdiomas().Any(i => string.Equals(i.Codigo, limpio, StringComparison.OrdinalIgnoreCase)))
            {
                throw new RegistroDuplicadoException($"Ya existe un idioma con el código '{limpio}'.");
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
                throw new DatoInvalidoException("El código del idioma es obligatorio.");
            }
            if (string.Equals(codigo, IdiomaBase, StringComparison.OrdinalIgnoreCase))
            {
                throw new DatoInvalidoException("El idioma base no puede eliminarse.");
            }
            if (string.Equals(codigo, _idiomaActual, StringComparison.OrdinalIgnoreCase))
            {
                throw new DatoInvalidoException("No puede eliminarse el idioma que está activo en este momento.");
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
                throw new DatoInvalidoException("El código del idioma es obligatorio.");
            }
            if (string.IsNullOrWhiteSpace(clave))
            {
                throw new DatoInvalidoException("La clave de la leyenda es obligatoria.");
            }

            // La leyenda guardada queda en la base y pisa a la del .resx del idioma.
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
                // Los idiomas entregados vienen de los recursos: la carga no depende de la base de datos.
                RecargarIdiomas();
                RecargarTextos();
                _cargado = true;
            }
        }

        private static void RecargarIdiomas()
        {
            try
            {
                _idiomas = Repositorio.ObtenerIdiomas().Where(i => i.Activo).ToList();
            }
            catch (Exception ex)
            {
                BitacoraLogic.Registrar(LogLevel.Warning,
                    "No se pudo leer la tabla Idiomas; se usan los idiomas entregados: " + ex.Message, capa: "Localización");
                _idiomas = Embebidos.Select(e => new Idioma { Codigo = e.Codigo, Nombre = e.Nombre, Activo = true }).ToList();
            }
        }

        private static void RecargarTextos()
        {
            Dictionary<string, string> textos = LeerEmbebidos(_idiomaActual);
            foreach (TextoLocalizacion t in LeerDeLaBase(_idiomaActual))
            {
                textos[t.Clave] = t.Valor; // las leyendas corregidas desde la administración pisan a las del .resx
            }
            _textos = textos;
        }

        private static List<TextoLocalizacion> LeerDeLaBase(string codigoIdioma)
        {
            try
            {
                return Repositorio.ObtenerTextos(codigoIdioma);
            }
            catch (Exception ex)
            {
                BitacoraLogic.Registrar(LogLevel.Warning,
                    $"No se pudieron leer las leyendas de la base para '{codigoIdioma}'; se usan las del .resx: " + ex.Message,
                    capa: "Localización");
                return new List<TextoLocalizacion>();
            }
        }

        /// <summary>Textos del .resx del idioma (ensamblado satélite de su cultura; el base viene en el principal).</summary>
        private static Dictionary<string, string> LeerEmbebidos(string codigoIdioma)
        {
            var textos = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                CultureInfo cultura = string.Equals(codigoIdioma, IdiomaBase, StringComparison.Ordinal)
                    ? CultureInfo.InvariantCulture
                    : CultureInfo.GetCultureInfo(codigoIdioma);
                ResourceSet? conjunto = Recursos.GetResourceSet(cultura, createIfNotExists: true, tryParents: false);
                if (conjunto != null)
                {
                    foreach (DictionaryEntry entrada in conjunto)
                    {
                        if (entrada.Key is string clave && entrada.Value is string valor)
                        {
                            textos[clave] = valor;
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is MissingManifestResourceException || ex is CultureNotFoundException || ex is System.IO.FileNotFoundException)
            {
                // Idioma agregado desde la administración: no tiene .resx, solo leyendas de la base.
            }
            return textos;
        }
    }
}
