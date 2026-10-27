using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Negocio.DAL.Auditoria
{
    /// <summary>
    /// Utilidades de serialización para el control de cambios (T06b): convierte el estado de
    /// una fila (valores de EF) a JSON fiel al tipo original, y aplica un JSON de vuelta
    /// sobre una entidad para recomponer su estado anterior. Los enums se guardan por nombre
    /// y los decimales/fechas conservan su tipo para una restauración exacta.
    /// </summary>
    public static class AuditoriaDeCambios
    {
        private static readonly JsonSerializerOptions Opciones = new()
        {
            WriteIndented = false,
            // Sin escapes innecesarios (por ejemplo, "+" o acentos): el historial queda
            // compacto y legible en la base, sin perder fidelidad de datos.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// Serializa los valores de una fila a JSON (excluye la firma DVH). Devuelve null si
        /// la fila no tiene valores (por ejemplo, el estado "nuevo" de una baja).
        /// </summary>
        public static string? Serializar(PropertyValues? valores)
        {
            if (valores == null)
            {
                return null;
            }

            var diccionario = new Dictionary<string, object?>();
            foreach (IProperty propiedad in valores.Properties)
            {
                if (propiedad.Name == "DVH")
                {
                    continue;
                }
                object? valor = valores[propiedad.Name];
                diccionario[propiedad.Name] = valor is Enum enumeracion ? enumeracion.ToString() : valor;
            }
            return JsonSerializer.Serialize(diccionario, Opciones);
        }

        /// <summary>
        /// Aplica los valores de un JSON a la entrada indicada (restauración): actualiza cada
        /// propiedad escalar escribible, omitiendo claves y la firma de integridad.
        /// </summary>
        public static void Aplicar(EntityEntry entrada, string json)
        {
            using JsonDocument documento = JsonDocument.Parse(json);
            foreach (JsonProperty campo in documento.RootElement.EnumerateObject())
            {
                IProperty? propiedad = entrada.Metadata.FindProperty(campo.Name);
                if (propiedad == null || propiedad.Name == "DVH" || propiedad.IsPrimaryKey() || propiedad.IsShadowProperty())
                {
                    continue;
                }
                if (propiedad.PropertyInfo == null || !propiedad.PropertyInfo.CanWrite)
                {
                    continue;
                }
                entrada.Property(campo.Name).CurrentValue = Convertir(campo.Value, propiedad.ClrType);
            }
        }

        /// <summary>Convierte un valor JSON al tipo CLR de la propiedad destino.</summary>
        private static object? Convertir(JsonElement elemento, Type tipo)
        {
            if (elemento.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            Type objetivo = Nullable.GetUnderlyingType(tipo) ?? tipo;
            if (objetivo == typeof(string))
            {
                return elemento.GetString();
            }
            if (objetivo == typeof(int))
            {
                return elemento.GetInt32();
            }
            if (objetivo == typeof(bool))
            {
                return elemento.GetBoolean();
            }
            if (objetivo == typeof(decimal))
            {
                return elemento.GetDecimal();
            }
            if (objetivo == typeof(double))
            {
                return elemento.GetDouble();
            }
            if (objetivo == typeof(DateTime))
            {
                return elemento.GetDateTime();
            }
            if (objetivo.IsEnum)
            {
                return Enum.Parse(objetivo, elemento.GetString() ?? string.Empty);
            }
            return elemento.ToString();
        }
    }
}
