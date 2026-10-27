using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Services.DAL.Interfaces;
using Services.DAL.Tools;
using Services.DomainModel;

namespace Services.DAL.Implementations
{
    /// <summary>
    /// Repositorio ADO.NET del sistema de permisos (T04): catálogo en árbol (tabla Permisos)
    /// y asignaciones por perfil (tabla PerfilesPermisos).
    /// </summary>
    internal class SqlPermisoRepository : IPermisoRepository
    {
        /// <summary>Devuelve el catálogo completo de permisos como filas planas.</summary>
        public List<PermisoFila> ObtenerCatalogo()
        {
            var filas = new List<PermisoFila>();
            using SqlDataReader lector = SqlHelper.EjecutarLector(
                "SELECT Codigo, Nombre, Tipo, CodigoPadre FROM dbo.Permisos ORDER BY Codigo;",
                CommandType.Text);
            while (lector.Read())
            {
                filas.Add(new PermisoFila(
                    lector.GetString(0),
                    lector.GetString(1),
                    lector.GetString(2),
                    lector.IsDBNull(3) ? null : lector.GetString(3)));
            }
            return filas;
        }

        /// <summary>Perfiles conocidos: asignaciones existentes más perfiles en uso por usuarios.</summary>
        public List<string> ObtenerPerfiles()
        {
            var perfiles = new List<string>();
            using SqlDataReader lector = SqlHelper.EjecutarLector(
                "SELECT DISTINCT Perfil FROM dbo.PerfilesPermisos " +
                "UNION SELECT DISTINCT Perfil FROM dbo.Usuarios ORDER BY Perfil;",
                CommandType.Text);
            while (lector.Read())
            {
                perfiles.Add(lector.GetString(0));
            }
            return perfiles;
        }

        /// <summary>Códigos asignados a un perfil.</summary>
        public List<string> ObtenerAsignacionesDePerfil(string perfil)
        {
            var codigos = new List<string>();
            using SqlDataReader lector = SqlHelper.EjecutarLector(
                "SELECT Permiso FROM dbo.PerfilesPermisos WHERE Perfil = @perfil ORDER BY Permiso;",
                CommandType.Text,
                new SqlParameter("@perfil", perfil));
            while (lector.Read())
            {
                codigos.Add(lector.GetString(0));
            }
            return codigos;
        }

        /// <summary>Reemplaza las asignaciones del perfil: borra las anteriores e inserta las nuevas.</summary>
        public void ReemplazarAsignaciones(string perfil, List<string> codigos)
        {
            SqlHelper.EjecutarComando(
                "DELETE FROM dbo.PerfilesPermisos WHERE Perfil = @perfil;",
                CommandType.Text,
                new SqlParameter("@perfil", perfil));
            foreach (string codigo in codigos)
            {
                SqlHelper.EjecutarComando(
                    "INSERT INTO dbo.PerfilesPermisos (Perfil, Permiso) VALUES (@perfil, @permiso);",
                    CommandType.Text,
                    new SqlParameter("@perfil", perfil),
                    new SqlParameter("@permiso", codigo));
            }
        }
    }
}
