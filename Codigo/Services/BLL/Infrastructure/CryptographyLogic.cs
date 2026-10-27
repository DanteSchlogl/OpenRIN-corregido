using System;
using System.Security.Cryptography;
using System.Text;

namespace Services.BLL.Infrastructure
{
    /// <summary>
    /// Núcleo criptográfico del módulo Services (REQ-ARQ-002).
    /// Cifrado simétrico AES-256 (CBC + IV aleatorio por operación, PKCS7) y
    /// hashing de contraseñas con PBKDF2-SHA256 y sal aleatoria por usuario ("números semilla").
    /// Formato del texto cifrado: Base64( [IV 16 bytes] + [ciphertext] ).
    /// Formato del hash: "salBase64.iteraciones.hashBase64".
    /// </summary>
    internal static class CryptographyLogic
    {
        private const int TamanoSalt = 16;            // bytes de sal para PBKDF2
        private const int IteracionesPbkdf2 = 120000; // costo del KDF (recomendación OWASP)
        private const int TamanoHash = 32;            // 256 bits
        private const int TamanoIv = 16;              // tamaño de bloque AES

        /// <summary>Encripta un texto plano con AES-256. Devuelve el texto cifrado en Base64.</summary>
        public static string Encriptar(string textoPlano)
        {
            ArgumentNullException.ThrowIfNull(textoPlano);

            byte[] clave = CryptoKeyProvider.ObtenerClave();
            byte[] plano = Encoding.UTF8.GetBytes(textoPlano);

            using (Aes aes = Aes.Create())
            {
                aes.Key = clave;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV(); // IV aleatorio por cada operación

                using (ICryptoTransform encriptador = aes.CreateEncryptor())
                {
                    byte[] cifrado = encriptador.TransformFinalBlock(plano, 0, plano.Length);
                    byte[] resultado = new byte[TamanoIv + cifrado.Length];
                    Buffer.BlockCopy(aes.IV, 0, resultado, 0, TamanoIv);
                    Buffer.BlockCopy(cifrado, 0, resultado, TamanoIv, cifrado.Length);
                    return Convert.ToBase64String(resultado);
                }
            }
        }

        /// <summary>Desencripta un texto cifrado previamente con <see cref="Encriptar"/> (Base64).</summary>
        public static string Desencriptar(string textoCifrado)
        {
            ArgumentNullException.ThrowIfNull(textoCifrado);

            byte[] datos = Convert.FromBase64String(textoCifrado);
            if (datos.Length <= TamanoIv)
            {
                throw new CryptographicException("El texto cifrado no tiene el formato esperado.");
            }

            byte[] iv = new byte[TamanoIv];
            Buffer.BlockCopy(datos, 0, iv, 0, TamanoIv);
            byte[] cifrado = new byte[datos.Length - TamanoIv];
            Buffer.BlockCopy(datos, TamanoIv, cifrado, 0, cifrado.Length);

            using (Aes aes = Aes.Create())
            {
                aes.Key = CryptoKeyProvider.ObtenerClave();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.IV = iv;

                using (ICryptoTransform desencriptador = aes.CreateDecryptor())
                {
                    byte[] plano = desencriptador.TransformFinalBlock(cifrado, 0, cifrado.Length);
                    return Encoding.UTF8.GetString(plano);
                }
            }
        }

        /// <summary>Hashea un texto (contraseñas) con PBKDF2-SHA256 + sal aleatoria por operación.</summary>
        public static string Hashear(string texto)
        {
            ArgumentNullException.ThrowIfNull(texto);

            byte[] salt = RandomNumberGenerator.GetBytes(TamanoSalt);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(texto), salt, IteracionesPbkdf2, HashAlgorithmName.SHA256, TamanoHash);

            return $"{Convert.ToBase64String(salt)}.{IteracionesPbkdf2}.{Convert.ToBase64String(hash)}";
        }

        /// <summary>Verifica un texto plano contra un hash generado por <see cref="Hashear"/>.</summary>
        public static bool VerificarHash(string texto, string hashAlmacenado)
        {
            ArgumentNullException.ThrowIfNull(texto);
            ArgumentNullException.ThrowIfNull(hashAlmacenado);

            string[] partes = hashAlmacenado.Split('.');
            if (partes.Length != 3 || !int.TryParse(partes[1], out int iteraciones))
            {
                return false;
            }

            byte[] salt;
            byte[] esperado;
            try
            {
                salt = Convert.FromBase64String(partes[0]);
                esperado = Convert.FromBase64String(partes[2]);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] calculado = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(texto), salt, iteraciones, HashAlgorithmName.SHA256, esperado.Length);

            return CryptographicOperations.FixedTimeEquals(esperado, calculado);
        }
    }
}
