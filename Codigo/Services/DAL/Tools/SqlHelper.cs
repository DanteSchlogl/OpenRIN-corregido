using System;
using System.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;
using Services.DomainModel.Exceptions;

namespace Services.DAL.Tools
{
    /// <summary>
    /// Helper ADO.NET del módulo Services: ejecuta comandos parametrizados contra SQL Server,
    /// normaliza valores nulos y envuelve errores técnicos en <see cref="DataAccessException"/>
    /// para no filtrar detalles de conexión hacia las capas superiores (REQ-ARQ-004).
    /// Permite elegir la conexión configurada (por defecto "ServicesDB"; los respaldos usan "BackupString")
    /// y se ENLISTA automáticamente en la transacción activa (TransaccionDAL) cuando la conexión coincide (REQ-ARQ-007).
    /// </summary>
    internal static class SqlHelper
    {
        private const string ConexionPorDefecto = "ServicesDB";

        private static string ObtenerConexion(string nombreConexion)
        {
            string? cadena = ConfigurationManager.ConnectionStrings[nombreConexion]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(cadena))
            {
                throw new InvalidOperationException(
                    $"Falta la cadena de conexión '{nombreConexion}' en la configuración de la aplicación.");
            }
            return cadena;
        }

        /// <summary>Ejecuta un comando de escritura (conexión por defecto) y devuelve la cantidad de filas afectadas.</summary>
        public static int EjecutarComando(string textoComando, CommandType tipo, params SqlParameter[] parametros)
            => EjecutarComando(textoComando, tipo, ConexionPorDefecto, parametros);

        /// <summary>Ejecuta un comando de escritura sobre la conexión indicada (o en la transacción activa si coincide).</summary>
        public static int EjecutarComando(string textoComando, CommandType tipo, string nombreConexion, params SqlParameter[] parametros)
        {
            NormalizarNulos(parametros);
            try
            {
                if (TransaccionDAL.UsaConexion(nombreConexion))
                {
                    using (SqlCommand comando = TransaccionDAL.CrearComando(textoComando, tipo, parametros))
                    {
                        return comando.ExecuteNonQuery();
                    }
                }

                using (SqlConnection conexion = new SqlConnection(ObtenerConexion(nombreConexion)))
                using (SqlCommand comando = new SqlCommand(textoComando, conexion))
                {
                    comando.CommandType = tipo;
                    if (parametros.Length > 0)
                    {
                        comando.Parameters.AddRange(parametros);
                    }
                    conexion.Open();
                    return comando.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("No se pudo completar la operación contra la base de datos.", ex);
            }
        }

        /// <summary>Ejecuta un comando y devuelve el primer valor (conexión por defecto).</summary>
        public static object? EjecutarEscalar(string textoComando, CommandType tipo, params SqlParameter[] parametros)
            => EjecutarEscalar(textoComando, tipo, ConexionPorDefecto, parametros);

        /// <summary>Ejecuta un comando y devuelve el primer valor, sobre la conexión indicada (o en la transacción activa si coincide).</summary>
        public static object? EjecutarEscalar(string textoComando, CommandType tipo, string nombreConexion, params SqlParameter[] parametros)
        {
            NormalizarNulos(parametros);
            try
            {
                if (TransaccionDAL.UsaConexion(nombreConexion))
                {
                    using (SqlCommand comando = TransaccionDAL.CrearComando(textoComando, tipo, parametros))
                    {
                        return comando.ExecuteScalar();
                    }
                }

                using (SqlConnection conexion = new SqlConnection(ObtenerConexion(nombreConexion)))
                using (SqlCommand comando = new SqlCommand(textoComando, conexion))
                {
                    comando.CommandType = tipo;
                    if (parametros.Length > 0)
                    {
                        comando.Parameters.AddRange(parametros);
                    }
                    conexion.Open();
                    return comando.ExecuteScalar();
                }
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("No se pudo completar la operación contra la base de datos.", ex);
            }
        }

        /// <summary>Ejecuta un comando y devuelve un lector de datos (conexión por defecto).</summary>
        public static SqlDataReader EjecutarLector(string textoComando, CommandType tipo, params SqlParameter[] parametros)
            => EjecutarLector(textoComando, tipo, ConexionPorDefecto, parametros);

        /// <summary>Ejecuta un comando y devuelve un lector de datos sobre la conexión indicada
        /// (fuera de transacción, la conexión se cierra junto con el lector).</summary>
        public static SqlDataReader EjecutarLector(string textoComando, CommandType tipo, string nombreConexion, params SqlParameter[] parametros)
        {
            NormalizarNulos(parametros);
            try
            {
                if (TransaccionDAL.UsaConexion(nombreConexion))
                {
                    // En transacción, el comando usa la conexión compartida: no se cierra junto al lector
                    // y el comando queda para recolección de basura (se libera al terminar la transacción).
                    SqlCommand comandoTx = TransaccionDAL.CrearComando(textoComando, tipo, parametros);
                    return comandoTx.ExecuteReader();
                }

                SqlConnection conexion = new SqlConnection(ObtenerConexion(nombreConexion));
                using (SqlCommand comando = new SqlCommand(textoComando, conexion))
                {
                    comando.CommandType = tipo;
                    if (parametros.Length > 0)
                    {
                        comando.Parameters.AddRange(parametros);
                    }
                    conexion.Open();
                    return comando.ExecuteReader(CommandBehavior.CloseConnection);
                }
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("No se pudo completar la operación contra la base de datos.", ex);
            }
        }

        private static void NormalizarNulos(SqlParameter[] parametros)
        {
            foreach (SqlParameter parametro in parametros)
            {
                if (parametro.Value == null)
                {
                    parametro.Value = DBNull.Value;
                }
            }
        }
    }
}
