using System.Collections.Generic;
using Services.DomainModel;

namespace Services.DAL.Interfaces
{
    /// <summary>Contrato del registro de respaldos (tabla Respaldos de OpenRIN_Services).</summary>
    public interface IRespaldoRegistroRepository
    {
        /// <summary>Inserta un registro y devuelve su identificador.</summary>
        int Registrar(RespaldoRegistro registro);

        /// <summary>Marca un respaldo como sano o no sano.</summary>
        void MarcarSano(int id, bool sano, string observaciones);

        /// <summary>Devuelve los últimos registros, del más nuevo al más antiguo.</summary>
        List<RespaldoRegistro> ListarUltimos(int cantidad);

        /// <summary>Respaldos de un origen para una base, del más nuevo al más antiguo.</summary>
        List<RespaldoRegistro> ListarPorOrigen(string baseDatos, string origen);

        /// <summary>Elimina el registro (no toca el archivo).</summary>
        void Eliminar(int id);
    }
}
