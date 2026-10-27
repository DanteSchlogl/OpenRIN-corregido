using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Services.BLL.Infrastructure
{
    /// <summary>
    /// Provee la clave maestra de cifrado de la aplicación (REQ-ARQ-002).
    /// Cadena de resolución:
    /// 1) Variable de entorno ACO_ENC_KEY (desarrollo / demo; se hashea para obtener 32 bytes).
    /// 2) Archivo local protegido con DPAPI (Windows Data Protection API, usuario actual).
    /// 3) Si no existe: se genera una clave aleatoria de 256 bits y se persiste protegida con DPAPI.
    /// La clave nunca vive en el repositorio ni en texto plano en disco.
    /// </summary>
    internal static class CryptoKeyProvider
    {
        private const string EnvVarName = "ACO_ENC_KEY";

        private static readonly string KeyDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenRIN");

        private static readonly string KeyFilePath = Path.Combine(KeyDirectory, "enc.key");

        private static readonly Lazy<byte[]> Clave = new Lazy<byte[]>(CargarOCrearClave);

        /// <summary>Devuelve la clave maestra (32 bytes) según la cadena de prioridad.</summary>
        public static byte[] ObtenerClave() => Clave.Value;

        private static byte[] CargarOCrearClave()
        {
            // 1) Variable de entorno (desarrollo / demo)
            string? claveEnv = Environment.GetEnvironmentVariable(EnvVarName);
            if (!string.IsNullOrWhiteSpace(claveEnv))
            {
                return SHA256.HashData(Encoding.UTF8.GetBytes(claveEnv));
            }

            // 2) Archivo protegido con DPAPI del usuario actual
            if (File.Exists(KeyFilePath))
            {
                byte[] bytesProtegidos = File.ReadAllBytes(KeyFilePath);
                return ProtectedData.Unprotect(bytesProtegidos, null, DataProtectionScope.CurrentUser);
            }

            // 3) Primera ejecución: se genera y se persiste protegida
            byte[] nuevaClave = RandomNumberGenerator.GetBytes(32); // AES-256
            Directory.CreateDirectory(KeyDirectory);
            File.WriteAllBytes(KeyFilePath, ProtectedData.Protect(nuevaClave, null, DataProtectionScope.CurrentUser));
            return nuevaClave;
        }
    }
}
