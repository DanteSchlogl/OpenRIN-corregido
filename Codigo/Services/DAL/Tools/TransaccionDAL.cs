using System;
using System.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Services.DAL.Tools
{
    /// <summary>
    /// Contexto de transacción de la capa de datos (REQ-ARQ-007): la BLL inicia una transacción
    /// y todos los métodos de escritura de la DAL que usen la MISMA conexión configurada se
    /// enlistan automáticamente (SqlHelper la detecta y reutiliza conexión + transacción).
    /// Alcance: una transacción activa por proceso (suficiente para la aplicación de escritorio).
    /// </summary>
    internal static class TransaccionDAL
    {
        private static readonly object Candado = new object();
        private static SqlConnection? _conexion;
        private static SqlTransaction? _transaccion;
        private static string? _nombreConexion;

        public static bool Activa => _transaccion != null;

        /// <summary>Indica si la transacción activa corresponde a la conexión configurada indicada.</summary>
        public static bool UsaConexion(string nombreConexion)
            => _transaccion != null &&
               string.Equals(_nombreConexion, nombreConexion, StringComparison.OrdinalIgnoreCase);

        /// <summary>Inicia una transacción sobre la conexión configurada indicada.</summary>
        public static void Iniciar(string nombreConexion)
        {
            lock (Candado)
            {
                if (Activa)
                {
                    throw new InvalidOperationException("Ya existe una transacción activa.");
                }

                string? cadena = ConfigurationManager.ConnectionStrings[nombreConexion]?.ConnectionString;
                if (string.IsNullOrWhiteSpace(cadena))
                {
                    throw new InvalidOperationException(
                        $"Falta la cadena de conexión '{nombreConexion}' en la configuración de la aplicación.");
                }

                SqlConnection conexion = new SqlConnection(cadena);
                conexion.Open();
                _transaccion = conexion.BeginTransaction();
                _conexion = conexion;
                _nombreConexion = nombreConexion;
            }
        }

        /// <summary>Confirma (commit) la transacción activa y libera los recursos.</summary>
        public static void Confirmar()
        {
            lock (Candado)
            {
                if (!Activa)
                {
                    throw new InvalidOperationException("No hay una transacción activa para confirmar.");
                }

                try
                {
                    _transaccion!.Commit();
                }
                finally
                {
                    Limpiar();
                }
            }
        }

        /// <summary>Revierte (rollback) la transacción activa y libera los recursos.</summary>
        public static void Revertir()
        {
            lock (Candado)
            {
                if (!Activa)
                {
                    throw new InvalidOperationException("No hay una transacción activa para revertir.");
                }

                try
                {
                    _transaccion!.Rollback();
                }
                finally
                {
                    Limpiar();
                }
            }
        }

        /// <summary>Crea un comando ya asociado a la conexión y transacción activas.</summary>
        public static SqlCommand CrearComando(string textoComando, CommandType tipo, SqlParameter[] parametros)
        {
            SqlCommand comando = new SqlCommand(textoComando, _conexion!) { CommandType = tipo };
            comando.Transaction = _transaccion!;
            if (parametros.Length > 0)
            {
                comando.Parameters.AddRange(parametros);
            }
            return comando;
        }

        private static void Limpiar()
        {
            _transaccion?.Dispose();
            _conexion?.Dispose();
            _transaccion = null;
            _conexion = null;
            _nombreConexion = null;
        }
    }
}
