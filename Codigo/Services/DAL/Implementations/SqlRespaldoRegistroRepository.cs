using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Services.DAL.Interfaces;
using Services.DAL.Tools;
using Services.DomainModel;

namespace Services.DAL.Implementations
{
    /// <summary>Registro de respaldos en la tabla Respaldos, con ADO.NET parametrizado vía <see cref="SqlHelper"/>.</summary>
    internal class SqlRespaldoRegistroRepository : IRespaldoRegistroRepository
    {
        private const string Columnas = "Id, Fecha, BaseDatos, Archivo, TamanoBytes, Origen, Sano, Usuario, Observaciones";

        /// <inheritdoc />
        public int Registrar(RespaldoRegistro r)
        {
            const string sql = @"
                INSERT INTO dbo.Respaldos (Fecha, BaseDatos, Archivo, TamanoBytes, Origen, Sano, Usuario, Observaciones)
                VALUES (@Fecha, @BaseDatos, @Archivo, @Tamano, @Origen, @Sano, @Usuario, @Obs);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            object? id = SqlHelper.EjecutarEscalar(sql, CommandType.Text,
                new SqlParameter("@Fecha", r.Fecha == default ? DateTime.Now : r.Fecha),
                new SqlParameter("@BaseDatos", r.BaseDatos),
                new SqlParameter("@Archivo", r.Archivo),
                new SqlParameter("@Tamano", r.TamanoBytes),
                new SqlParameter("@Origen", r.Origen),
                new SqlParameter("@Sano", r.Sano),
                new SqlParameter("@Usuario", r.Usuario),
                new SqlParameter("@Obs", r.Observaciones));
            return Convert.ToInt32(id);
        }

        /// <inheritdoc />
        public void MarcarSano(int id, bool sano, string observaciones)
        {
            SqlHelper.EjecutarComando(
                "UPDATE dbo.Respaldos SET Sano = @Sano, Observaciones = @Obs WHERE Id = @Id;", CommandType.Text,
                new SqlParameter("@Sano", sano), new SqlParameter("@Obs", observaciones), new SqlParameter("@Id", id));
        }

        /// <inheritdoc />
        public List<RespaldoRegistro> ListarUltimos(int cantidad)
            => Leer($"SELECT TOP (@N) {Columnas} FROM dbo.Respaldos ORDER BY Fecha DESC, Id DESC;",
                new SqlParameter("@N", cantidad));

        /// <inheritdoc />
        public List<RespaldoRegistro> ListarPorOrigen(string baseDatos, string origen)
            => Leer($"SELECT {Columnas} FROM dbo.Respaldos WHERE BaseDatos = @Base AND Origen = @Origen ORDER BY Fecha DESC, Id DESC;",
                new SqlParameter("@Base", baseDatos), new SqlParameter("@Origen", origen));

        /// <inheritdoc />
        public void Eliminar(int id)
            => SqlHelper.EjecutarComando("DELETE FROM dbo.Respaldos WHERE Id = @Id;", CommandType.Text, new SqlParameter("@Id", id));

        private static List<RespaldoRegistro> Leer(string sql, params SqlParameter[] parametros)
        {
            var lista = new List<RespaldoRegistro>();
            using SqlDataReader l = SqlHelper.EjecutarLector(sql, CommandType.Text, parametros);
            while (l.Read())
            {
                lista.Add(new RespaldoRegistro
                {
                    Id = l.GetInt32(0),
                    Fecha = l.GetDateTime(1),
                    BaseDatos = l.GetString(2),
                    Archivo = l.GetString(3),
                    TamanoBytes = l.GetInt64(4),
                    Origen = l.GetString(5),
                    Sano = l.GetBoolean(6),
                    Usuario = l.IsDBNull(7) ? "" : l.GetString(7),
                    Observaciones = l.IsDBNull(8) ? "" : l.GetString(8)
                });
            }
            return lista;
        }
    }
}
