using System.Collections.Generic;
using Services.DomainModel;

namespace Services.DAL.Interfaces
{
    /// <summary>
    /// Contrato de acceso a datos de localización (T05): idiomas y textos almacenados en la
    /// base. Los consumidores (BLL) trabajan contra esta interfaz para mantener el modelo
    /// desacoplado del mecanismo de persistencia.
    /// </summary>
    internal interface ILocalizacionRepository
    {
        /// <summary>Devuelve todos los idiomas registrados (activos e inactivos).</summary>
        List<Idioma> ObtenerIdiomas();

        /// <summary>Devuelve todos los textos del idioma indicado.</summary>
        List<TextoLocalizacion> ObtenerTextos(string codigoIdioma);

        /// <summary>Registra un idioma nuevo (activo por defecto).</summary>
        void RegistrarIdioma(string codigo, string nombre);

        /// <summary>Activa o desactiva un idioma.</summary>
        void CambiarEstadoIdioma(string codigo, bool activo);

        /// <summary>Elimina un idioma junto con todos sus textos.</summary>
        void EliminarIdioma(string codigo);

        /// <summary>Inserta o actualiza el valor de una leyenda para un idioma.</summary>
        void GuardarTexto(string codigoIdioma, string clave, string valor);

        /// <summary>Elimina una leyenda de un idioma.</summary>
        void EliminarTexto(string codigoIdioma, string clave);
    }
}
