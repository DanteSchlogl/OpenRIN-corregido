using Negocio.DomainModel;

namespace Negocio.DAL.Repositorios
{
    /// <summary>
    /// Operaciones específicas de acceso a datos de pacientes (REQ-FUNC-001).
    /// </summary>
    public interface IPacienteRepositorio : IRepositorioGenerico<Paciente>
    {
        /// <summary>Pacientes activos ordenados por nombre completo.</summary>
        List<Paciente> BuscarActivos();

        /// <summary>Busca un paciente por documento, sin importar su estado.</summary>
        Paciente? BuscarPorDNI(string dni);

        /// <summary>Indica si existe un paciente ACTIVO con el documento indicado.
        /// Permite excluir un id (por ejemplo, al modificar el propio registro).</summary>
        bool ExisteDNIActivo(string dni, int? idExcluir = null);
    }
}
