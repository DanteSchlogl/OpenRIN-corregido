using System;
using System.Configuration;
using System.Reflection;
using Services.DomainModel.Exceptions;

namespace Services.DAL.Factory
{
    /// <summary>
    /// Fábrica de repositorios con reflexión. Lee del archivo de configuración el nombre completo de la
    /// clase que implementa cada interfaz (clave «Repositorio.NombreDeLaInterfaz») y crea la instancia.
    /// Valida que la clase exista, sea concreta, implemente la interfaz y tenga el constructor pedido;
    /// si algo falla lanza <see cref="ConfiguracionInvalidaException"/> con un mensaje que dice qué corregir.
    /// Las clases de acceso a datos son <c>internal</c>: esta fábrica y las interfaces son la única puerta.
    /// </summary>
    public static class RepositorioFactory
    {
        private const string Prefijo = "Repositorio.";

        /// <summary>Crea el repositorio configurado para la interfaz <typeparamref name="TInterfaz"/>.</summary>
        /// <param name="alias">Distingue varias implementaciones de la misma interfaz (clave «Repositorio.Interfaz.alias»).</param>
        /// <param name="argumentos">Argumentos del constructor; sin argumentos se exige un constructor sin parámetros.</param>
        public static TInterfaz Crear<TInterfaz>(string? alias = null, params object[] argumentos) where TInterfaz : class
        {
            string clave = Prefijo + typeof(TInterfaz).Name + (string.IsNullOrEmpty(alias) ? "" : "." + alias);
            return CrearDesdeNombre<TInterfaz>(clave, ConfigurationManager.AppSettings[clave], argumentos);
        }

        /// <summary>Valida el nombre de clase indicado y crea la instancia (separado de la lectura de la configuración para poder probarlo).</summary>
        internal static TInterfaz CrearDesdeNombre<TInterfaz>(string clave, string? nombreClase, object[] argumentos) where TInterfaz : class
        {
            if (string.IsNullOrWhiteSpace(nombreClase))
            {
                throw new ConfiguracionInvalidaException(
                    $"Falta la clave '{clave}' en la configuración: indique la clase que implementa {typeof(TInterfaz).Name}.");
            }

            nombreClase = nombreClase.Trim();
            Type? tipo = BuscarTipo(nombreClase);
            if (tipo == null)
            {
                throw new ConfiguracionInvalidaException(
                    $"La clase '{nombreClase}' indicada en '{clave}' no existe.");
            }
            if (tipo.IsAbstract || tipo.IsInterface)
            {
                throw new ConfiguracionInvalidaException(
                    $"La clase '{nombreClase}' indicada en '{clave}' no es una clase concreta.");
            }
            if (!typeof(TInterfaz).IsAssignableFrom(tipo))
            {
                throw new ConfiguracionInvalidaException(
                    $"La clase '{nombreClase}' indicada en '{clave}' no implementa {typeof(TInterfaz).Name}.");
            }

            Type[] firma = Array.ConvertAll(argumentos, a => a.GetType());
            ConstructorInfo? constructor = Array.Find(
                tipo.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
                c => ParametrosCompatibles(c.GetParameters(), firma));
            if (constructor == null)
            {
                string pedido = argumentos.Length == 0 ? "un constructor sin parámetros" : "un constructor con los argumentos " + string.Join(", ", Array.ConvertAll(firma, t => t.Name));
                throw new ConfiguracionInvalidaException(
                    $"La clase '{nombreClase}' indicada en '{clave}' no tiene {pedido}.");
            }

            try
            {
                return (TInterfaz)constructor.Invoke(argumentos);
            }
            catch (TargetInvocationException ex)
            {
                throw new ConfiguracionInvalidaException(
                    $"No se pudo crear '{nombreClase}' (clave '{clave}'): {ex.InnerException?.Message ?? ex.Message}", ex.InnerException);
            }
        }

        private static Type? BuscarTipo(string nombre)
        {
            Type? tipo = Type.GetType(nombre, throwOnError: false);
            if (tipo != null)
            {
                return tipo;
            }

            foreach (Assembly ensamblado in AppDomain.CurrentDomain.GetAssemblies())
            {
                tipo = ensamblado.GetType(nombre, throwOnError: false);
                if (tipo != null)
                {
                    return tipo;
                }
            }
            return null;
        }

        private static bool ParametrosCompatibles(ParameterInfo[] parametros, Type[] firma)
        {
            if (parametros.Length != firma.Length)
            {
                return false;
            }
            for (int i = 0; i < parametros.Length; i++)
            {
                if (!parametros[i].ParameterType.IsAssignableFrom(firma[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
