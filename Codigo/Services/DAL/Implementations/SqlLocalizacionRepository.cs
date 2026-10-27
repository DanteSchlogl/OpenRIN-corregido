using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Services.DAL.Interfaces;
using Services.DAL.Tools;
using Services.DomainModel;

namespace Services.DAL.Implementations
{
    /// <summary>
    /// Repositorio ADO.NET de idiomas y textos (T05): el contenido de localización vive en la
    /// base (tablas Idiomas y Textos) y se administra con comandos parametrizados.
    /// </summary>
    internal class SqlLocalizacionRepository : ILocalizacionRepository
    {
        /// <summary>Devuelve todos los idiomas registrados, ordenados por código.</summary>
        public List<Idioma> ObtenerIdiomas()
        {
            var idiomas = new List<Idioma>();
            using SqlDataReader lector = SqlHelper.EjecutarLector(
                "SELECT Codigo, Nombre, Activo FROM dbo.Idiomas ORDER BY Codigo;", CommandType.Text);
            while (lector.Read())
            {
                idiomas.Add(new Idioma
                {
                    Codigo = lector.GetString(0),
                    Nombre = lector.GetString(1),
                    Activo = lector.GetBoolean(2)
                });
            }
            return idiomas;
        }

        /// <summary>Devuelve todos los textos del idioma indicado, ordenados por clave.</summary>
        public List<TextoLocalizacion> ObtenerTextos(string codigoIdioma)
        {
            var textos = new List<TextoLocalizacion>();
            using SqlDataReader lector = SqlHelper.EjecutarLector(
                "SELECT Id, CodigoIdioma, Clave, Valor FROM dbo.Textos WHERE CodigoIdioma = @codigo ORDER BY Clave;",
                CommandType.Text,
                new SqlParameter("@codigo", codigoIdioma));
            while (lector.Read())
            {
                textos.Add(new TextoLocalizacion
                {
                    Id = lector.GetInt32(0),
                    CodigoIdioma = lector.GetString(1),
                    Clave = lector.GetString(2),
                    Valor = lector.GetString(3)
                });
            }
            return textos;
        }

        /// <summary>Registra un idioma nuevo (activo por defecto).</summary>
        public void RegistrarIdioma(string codigo, string nombre)
            => SqlHelper.EjecutarComando(
                "INSERT INTO dbo.Idiomas (Codigo, Nombre, Activo) VALUES (@codigo, @nombre, 1);",
                CommandType.Text,
                new SqlParameter("@codigo", codigo),
                new SqlParameter("@nombre", nombre));

        /// <summary>Activa o desactiva un idioma.</summary>
        public void CambiarEstadoIdioma(string codigo, bool activo)
            => SqlHelper.EjecutarComando(
                "UPDATE dbo.Idiomas SET Activo = @activo WHERE Codigo = @codigo;",
                CommandType.Text,
                new SqlParameter("@activo", activo),
                new SqlParameter("@codigo", codigo));

        /// <summary>Elimina un idioma junto con todos sus textos.</summary>
        public void EliminarIdioma(string codigo)
        {
            SqlHelper.EjecutarComando(
                "DELETE FROM dbo.Textos WHERE CodigoIdioma = @codigo;",
                CommandType.Text,
                new SqlParameter("@codigo", codigo));
            SqlHelper.EjecutarComando(
                "DELETE FROM dbo.Idiomas WHERE Codigo = @codigo;",
                CommandType.Text,
                new SqlParameter("@codigo", codigo));
        }

        /// <summary>Inserta o actualiza el valor de una leyenda para un idioma (upsert).</summary>
        public void GuardarTexto(string codigoIdioma, string clave, string valor)
            => SqlHelper.EjecutarComando(
                "MERGE dbo.Textos AS destino " +
                "USING (SELECT @codigo AS CodigoIdioma, @clave AS Clave) AS origen " +
                "ON destino.CodigoIdioma = origen.CodigoIdioma AND destino.Clave = origen.Clave " +
                "WHEN MATCHED THEN UPDATE SET Valor = @valor " +
                "WHEN NOT MATCHED THEN INSERT (CodigoIdioma, Clave, Valor) VALUES (@codigo, @clave, @valor);",
                CommandType.Text,
                new SqlParameter("@codigo", codigoIdioma),
                new SqlParameter("@clave", clave),
                new SqlParameter("@valor", valor));

        /// <summary>Elimina una leyenda de un idioma.</summary>
        public void EliminarTexto(string codigoIdioma, string clave)
            => SqlHelper.EjecutarComando(
                "DELETE FROM dbo.Textos WHERE CodigoIdioma = @codigo AND Clave = @clave;",
                CommandType.Text,
                new SqlParameter("@codigo", codigoIdioma),
                new SqlParameter("@clave", clave));
    }
}
