using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Services.DAL.Tools;
using Services.DomainModel;
using Services.DomainModel.Validacion;

namespace Services.BLL
{
    /// <summary>
    /// Lógica de consistencia (REQ-ARQ-007): validación de entidades por atributos con reflexión
    /// ([Requerido], [Unico], [Rango]) y administración de transacciones atómicas junto con la DAL.
    /// Toda validación fallida queda registrada en la bitácora con nivel Warning.
    /// </summary>
    internal static class ConsistenciaLogic
    {
        public static bool ValidarEntidad<T>(
            T entidad,
            out List<string> mensajesDeError,
            Func<T, string, bool>? verificadorUnicidad = null)
        {
            mensajesDeError = new List<string>();

            if (entidad == null)
            {
                mensajesDeError.Add("La entidad a validar es nula.");
                RegistrarFallo(typeof(T).Name, mensajesDeError);
                return false;
            }

            foreach (PropertyInfo propiedad in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                object? valor = propiedad.GetValue(entidad);

                if (propiedad.GetCustomAttribute<RequeridoAttribute>() != null)
                {
                    bool vacio = valor == null || (valor is string texto && string.IsNullOrWhiteSpace(texto));
                    if (vacio)
                    {
                        mensajesDeError.Add($"El campo '{propiedad.Name}' es obligatorio.");
                    }
                }

                RangoAttribute? rango = propiedad.GetCustomAttribute<RangoAttribute>();
                if (rango != null && valor != null)
                {
                    if (IntentarConvertirANumero(valor, out double numero))
                    {
                        if (numero < rango.Minimo || numero > rango.Maximo)
                        {
                            mensajesDeError.Add(
                                $"El campo '{propiedad.Name}' debe estar entre {rango.Minimo} y {rango.Maximo}.");
                        }
                    }
                    else
                    {
                        mensajesDeError.Add(
                            $"El campo '{propiedad.Name}' debe ser numérico (para validar el rango).");
                    }
                }

                if (propiedad.GetCustomAttribute<UnicoAttribute>() != null && verificadorUnicidad != null)
                {
                    if (!verificadorUnicidad(entidad, propiedad.Name))
                    {
                        mensajesDeError.Add(
                            $"El valor del campo '{propiedad.Name}' ya está registrado (debe ser único).");
                    }
                }
            }

            if (mensajesDeError.Count > 0)
            {
                RegistrarFallo(typeof(T).Name, mensajesDeError);
                return false;
            }

            return true;
        }

        public static void IniciarTransaccion(string nombreConexion) => TransaccionDAL.Iniciar(nombreConexion);

        public static void ConfirmarTransaccion() => TransaccionDAL.Confirmar();

        public static void RevertirTransaccion() => TransaccionDAL.Revertir();

        public static bool TransaccionActiva => TransaccionDAL.Activa;

        public static T EjecutarEnTransaccion<T>(Func<T> operacion, string nombreConexion = "ServicesDB")
        {
            ArgumentNullException.ThrowIfNull(operacion);

            TransaccionDAL.Iniciar(nombreConexion);
            try
            {
                T resultado = operacion();
                TransaccionDAL.Confirmar();
                return resultado;
            }
            catch (Exception ex)
            {
                try
                {
                    TransaccionDAL.Revertir();
                }
                catch
                {
                    // Si la transacción ya no es utilizable, se prioriza el error original.
                }

                BitacoraLogic.Registrar(LogLevel.Warning,
                    $"Transacción revertida por error: {ex.Message}", ex, capa: "Consistencia");
                throw;
            }
        }

        /// <summary>
        /// Convierte el valor de una propiedad a número SIN pasar por formato de texto cuando el
        /// valor ya es numérico (evita el bug clásico de round-trip de culturas: "2,5" ≠ "2.5").
        /// Para textos se intenta parsear con la cultura actual.
        /// </summary>
        private static bool IntentarConvertirANumero(object valor, out double numero)
        {
            switch (valor)
            {
                case byte b: numero = b; return true;
                case short s: numero = s; return true;
                case int i: numero = i; return true;
                case long l: numero = l; return true;
                case float f: numero = f; return true;
                case double d: numero = d; return true;
                case decimal m: numero = (double)m; return true;
                default:
                    return double.TryParse(
                        valor.ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out numero);
            }
        }

        private static void RegistrarFallo(string nombreEntidad, List<string> errores)
        {
            BitacoraLogic.Registrar(LogLevel.Warning,
                $"Validación fallida de '{nombreEntidad}': {string.Join(" | ", errores)}",
                capa: "Consistencia");
        }
    }
}
