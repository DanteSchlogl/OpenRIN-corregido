using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Negocio.DomainModel
{
    /// <summary>
    /// Cálculo de dígitos verificadores (integridad de datos, DVH/DVV): el DVH firma los campos
    /// escalares de una fila y el DVV firma el conjunto de DVH de una tabla. La MISMA rutina se
    /// usa al escribir (contexto EF) y al auditar (BLL), garantizando firmas reproducibles.
    /// </summary>
    public static class DigitoVerificador
    {
        /// <summary>Calcula el DVH de una entidad: SHA-256 (hex) sobre sus campos escalares ordenados por nombre.</summary>
        public static string CalcularDVH(object entidad)
        {
            var campos = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (System.Reflection.PropertyInfo propiedad in entidad.GetType().GetProperties())
            {
                if (propiedad.Name == "DVH" || !propiedad.CanRead)
                {
                    continue;
                }
                System.Type tipo = propiedad.PropertyType;
                if (tipo != typeof(string) && !tipo.IsValueType)
                {
                    continue; // navegaciones u objetos complejos: no participan de la firma
                }
                campos[propiedad.Name] = Formatear(propiedad.GetValue(entidad));
            }

            var cuerpo = new StringBuilder();
            foreach (KeyValuePair<string, string> campo in campos)
            {
                cuerpo.Append(campo.Key).Append('=').Append(campo.Value).Append('|');
            }
            return Hash(cuerpo.ToString());
        }

        /// <summary>Calcula el DVV de una tabla: SHA-256 (hex) sobre su nombre y los DVH de todas sus filas ordenadas por Id.</summary>
        public static string CalcularDVV(string tabla, IEnumerable<(int Id, string? Dvh)> filas)
        {
            var cuerpo = new StringBuilder();
            cuerpo.Append(tabla).Append('#');
            foreach ((int Id, string? Dvh) fila in filas.OrderBy(f => f.Id))
            {
                cuerpo.Append(fila.Id).Append(':').Append(fila.Dvh ?? "∅").Append('|');
            }
            return Hash(cuerpo.ToString());
        }

        /// <summary>Formatea un valor escalar de forma determinista (invariante; decimales redondeados a 4 posiciones).</summary>
        public static string Formatear(object? valor) => valor switch
        {
            null => string.Empty,
            string texto => texto,
            bool booleano => booleano ? "1" : "0",
            DateTime fecha => fecha.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture),
            decimal numero => decimal.Round(numero, 4).ToString("0.####", CultureInfo.InvariantCulture),
            Enum enumeracion => enumeracion.ToString(),
            IFormattable formateable => formateable.ToString(null, CultureInfo.InvariantCulture),
            _ => valor.ToString() ?? string.Empty
        };

        private static string Hash(string contenido)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(contenido));
            return Convert.ToHexString(bytes);
        }
    }
}
