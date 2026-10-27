using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Services.DAL.Interfaces;
using Services.DAL.Tools;
using Services.DomainModel;

namespace Services.DAL.Implementations
{
    /// <summary>
    /// Persistencia de usuarios y permisos en la base Services (ADO.NET, REQ-ARQ-006).
    /// </summary>
    public class SqlUsuarioRepository : IUsuarioRepository
    {
        private const string Columnas = @"
            Id, NombreUsuario, NombreCompleto, HashPassword, Perfil, Email,
            PreguntaSeguridad, RespuestaHash, Idioma, Activo, IntentosFallidos, BloqueadoHasta";

        /// <inheritdoc />
        public Usuario? ObtenerPorNombreUsuario(string nombreUsuario)
        {
            string sql = $"SELECT {Columnas} FROM dbo.Usuarios WHERE NombreUsuario = @NombreUsuario;";
            using SqlDataReader lector = SqlHelper.EjecutarLector(
                sql, CommandType.Text, new SqlParameter("@NombreUsuario", nombreUsuario));
            return lector.Read() ? Mapear(lector) : null;
        }

        /// <inheritdoc />
        public Usuario? ObtenerPorId(int id)
        {
            string sql = $"SELECT {Columnas} FROM dbo.Usuarios WHERE Id = @Id;";
            using SqlDataReader lector = SqlHelper.EjecutarLector(
                sql, CommandType.Text, new SqlParameter("@Id", id));
            return lector.Read() ? Mapear(lector) : null;
        }

        /// <inheritdoc />
        public bool ExisteNombreUsuario(string nombreUsuario)
        {
            const string sql = "SELECT COUNT(*) FROM dbo.Usuarios WHERE NombreUsuario = @NombreUsuario;";
            object? cantidad = SqlHelper.EjecutarEscalar(
                sql, CommandType.Text, new SqlParameter("@NombreUsuario", nombreUsuario));
            return Convert.ToInt32(cantidad) > 0;
        }

        /// <inheritdoc />
        public int Registrar(Usuario usuario)
        {
            const string sql = @"
                INSERT INTO dbo.Usuarios
                    (NombreUsuario, NombreCompleto, HashPassword, Perfil, Email, PreguntaSeguridad, RespuestaHash, Idioma, Activo, IntentosFallidos, BloqueadoHasta)
                OUTPUT INSERTED.Id
                VALUES
                    (@NombreUsuario, @NombreCompleto, @HashPassword, @Perfil, @Email, @PreguntaSeguridad, @RespuestaHash, @Idioma, @Activo, @IntentosFallidos, @BloqueadoHasta);";

            object? id = SqlHelper.EjecutarEscalar(
                sql,
                CommandType.Text,
                new SqlParameter("@NombreUsuario", usuario.NombreUsuario),
                new SqlParameter("@NombreCompleto", usuario.NombreCompleto),
                new SqlParameter("@HashPassword", usuario.HashPassword),
                new SqlParameter("@Perfil", usuario.Perfil),
                new SqlParameter("@Email", (object?)usuario.Email ?? DBNull.Value),
                new SqlParameter("@PreguntaSeguridad", (object?)usuario.PreguntaSeguridad ?? DBNull.Value),
                new SqlParameter("@RespuestaHash", (object?)usuario.RespuestaHash ?? DBNull.Value),
                new SqlParameter("@Idioma", (object?)usuario.Idioma ?? DBNull.Value),
                new SqlParameter("@Activo", usuario.Activo),
                new SqlParameter("@IntentosFallidos", usuario.IntentosFallidos),
                new SqlParameter("@BloqueadoHasta", (object?)usuario.BloqueadoHasta ?? DBNull.Value));

            return id == null || id == DBNull.Value ? 0 : Convert.ToInt32(id);
        }

        /// <inheritdoc />
        public void ActualizarAcceso(int idUsuario, int intentosFallidos, DateTime? bloqueadoHasta)
        {
            const string sql = @"
                UPDATE dbo.Usuarios
                SET IntentosFallidos = @IntentosFallidos,
                    BloqueadoHasta = @BloqueadoHasta
                WHERE Id = @Id;";

            SqlHelper.EjecutarComando(
                sql,
                CommandType.Text,
                new SqlParameter("@IntentosFallidos", intentosFallidos),
                new SqlParameter("@BloqueadoHasta", (object?)bloqueadoHasta ?? DBNull.Value),
                new SqlParameter("@Id", idUsuario));
        }

        /// <inheritdoc />
        public void ActualizarEstado(string nombreUsuario, bool activo)
        {
            const string sql = "UPDATE dbo.Usuarios SET Activo = @Activo WHERE NombreUsuario = @NombreUsuario;";

            SqlHelper.EjecutarComando(
                sql,
                CommandType.Text,
                new SqlParameter("@Activo", activo),
                new SqlParameter("@NombreUsuario", nombreUsuario));
        }

        /// <inheritdoc />
        public bool PerfilTienePermiso(string perfil, string permiso)
        {
            const string sql = "SELECT COUNT(*) FROM dbo.PerfilesPermisos WHERE Perfil = @Perfil AND Permiso = @Permiso;";
            object? cantidad = SqlHelper.EjecutarEscalar(
                sql,
                CommandType.Text,
                new SqlParameter("@Perfil", perfil),
                new SqlParameter("@Permiso", permiso));
            return Convert.ToInt32(cantidad) > 0;
        }

        /// <inheritdoc />
        public void ActualizarPassword(int idUsuario, string hashPassword)
        {
            const string sql = "UPDATE dbo.Usuarios SET HashPassword = @HashPassword WHERE Id = @Id;";

            SqlHelper.EjecutarComando(
                sql,
                CommandType.Text,
                new SqlParameter("@HashPassword", hashPassword),
                new SqlParameter("@Id", idUsuario));
        }

        /// <inheritdoc />
        public List<Usuario> ObtenerTodos()
        {
            string sql = $"SELECT {Columnas} FROM dbo.Usuarios ORDER BY NombreUsuario;";
            using SqlDataReader lector = SqlHelper.EjecutarLector(sql, CommandType.Text);
            var lista = new List<Usuario>();
            while (lector.Read())
            {
                lista.Add(Mapear(lector));
            }
            return lista;
        }

        private static Usuario Mapear(SqlDataReader lector) => new Usuario
        {
            Id = lector.GetInt32(0),
            NombreUsuario = lector.GetString(1),
            NombreCompleto = lector.GetString(2),
            HashPassword = lector.GetString(3),
            Perfil = lector.GetString(4),
            Email = lector.IsDBNull(5) ? null : lector.GetString(5),
            PreguntaSeguridad = lector.IsDBNull(6) ? null : lector.GetString(6),
            RespuestaHash = lector.IsDBNull(7) ? null : lector.GetString(7),
            Idioma = lector.IsDBNull(8) ? null : lector.GetString(8),
            Activo = lector.GetBoolean(9),
            IntentosFallidos = lector.GetInt32(10),
            BloqueadoHasta = lector.IsDBNull(11) ? null : lector.GetDateTime(11)
        };
    }
}
