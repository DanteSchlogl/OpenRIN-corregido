using Microsoft.EntityFrameworkCore;
using Negocio.DAL.Context;
using Negocio.DomainModel;
using Negocio.DomainModel.Enums;

namespace Negocio.DAL.Repositorios
{
    /// <summary>
    /// Repositorio de pacientes sobre EF Core (REQ-FUNC-001): altas, búsquedas por
    /// documento y verificación de unicidad entre pacientes activos.
    /// </summary>
    public class PacienteRepositorio : RepositorioGenerico<Paciente>, IPacienteRepositorio
    {
        public PacienteRepositorio(NegocioDbContext contexto) : base(contexto)
        {
        }

        public List<Paciente> BuscarActivos()
            => Contexto.Pacientes.AsNoTracking()
                .Where(p => p.Estado == EstadoPaciente.Activo)
                .OrderBy(p => p.NombreCompleto)
                .ToList();

        public Paciente? BuscarPorDNI(string dni)
            => Contexto.Pacientes.FirstOrDefault(p => p.DNI == dni);

        public bool ExisteDNIActivo(string dni, int? idExcluir = null)
            => Contexto.Pacientes.Any(p =>
                p.DNI == dni &&
                p.Estado == EstadoPaciente.Activo &&
                (idExcluir == null || p.Id != idExcluir));
    }
}
