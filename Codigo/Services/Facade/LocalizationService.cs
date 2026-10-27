using System;
using System.Collections.Generic;
using Services.BLL.Localization;
using Services.DomainModel;

namespace Services.Facade
{
    /// <summary>
    /// Fachada pública de internacionalización (T05): punto de entrada único para obtener
    /// textos traducidos, cambiar el idioma en caliente (notificando a los observadores
    /// suscriptos) y administrar idiomas y leyendas desde el sistema. El contenido vive en la
    /// base de datos: no se utilizan hojas de recursos estáticos en tiempo de ejecución.
    /// </summary>
    public static class LocalizationService
    {
        /// <summary>Obtiene el texto asociado a una clave en el idioma activo (fallback: la clave).</summary>
        public static string ObtenerTexto(string clave) => LocalizationLogic.ObtenerTexto(clave);

        /// <summary>Obtiene un texto con formato aplicado (string.Format) en el idioma activo.</summary>
        public static string ObtenerTexto(string clave, params object[] argumentos)
            => string.Format(LocalizationLogic.ObtenerTexto(clave), argumentos);

        /// <summary>Cambia el idioma de la aplicación en caliente; notifica a los suscriptores.</summary>
        public static void EstablecerIdioma(string codigoIdioma) => LocalizationLogic.EstablecerIdioma(codigoIdioma);

        /// <summary>Código del idioma activo.</summary>
        public static string IdiomaActual => LocalizationLogic.IdiomaActual;

        /// <summary>Códigos de los idiomas activos soportados por la aplicación.</summary>
        public static IReadOnlyList<string> IdiomasSoportados => LocalizationLogic.IdiomasSoportados;

        /// <summary>Idiomas activos con su nombre para mostrar (desde la base de datos).</summary>
        public static IReadOnlyList<Idioma> Idiomas => LocalizationLogic.Idiomas;

        /// <summary>Suscribe una acción al observer: se ejecuta al cambiar el idioma activo o al
        /// editar las leyendas del idioma activo (patrón observer, T05).</summary>
        public static void Suscribir(Action alCambiarIdioma) => LocalizationLogic.IdiomaCambiado += alCambiarIdioma;

        /// <summary>Desuscribe una acción previamente suscripta.</summary>
        public static void Desuscribir(Action alCambiarIdioma) => LocalizationLogic.IdiomaCambiado -= alCambiarIdioma;

        /// <summary>Devuelve los textos (clave y valor) de un idioma, para la administración.</summary>
        public static List<TextoLocalizacion> ObtenerTextosDe(string codigoIdioma) => LocalizationLogic.ObtenerTextosDe(codigoIdioma);

        /// <summary>Registra un idioma nuevo (T05: incorporar idiomas desde el sistema).</summary>
        public static void RegistrarIdioma(string codigo, string nombre) => LocalizationLogic.RegistrarIdioma(codigo, nombre);

        /// <summary>Elimina un idioma junto con sus leyendas.</summary>
        public static void EliminarIdioma(string codigo) => LocalizationLogic.EliminarIdioma(codigo);

        /// <summary>Inserta o actualiza una leyenda de un idioma (T05: incorporar leyendas).</summary>
        public static void GuardarTexto(string codigoIdioma, string clave, string valor) => LocalizationLogic.GuardarTexto(codigoIdioma, clave, valor);

        /// <summary>Elimina una leyenda de un idioma.</summary>
        public static void EliminarTexto(string codigoIdioma, string clave) => LocalizationLogic.EliminarTexto(codigoIdioma, clave);
    }
}
