using Microsoft.EntityFrameworkCore;
using Negocio.DAL.Context;
using Negocio.DomainModel;
using Negocio.DomainModel.Enums;
using Negocio.DomainModel.Exceptions;
using Services.DomainModel;
using Services.Facade;

namespace Negocio.BLL
{
    /// <summary>
    /// Lógica de alertas (REQ-FUNC-007): las alertas permanecen activas —destacadas en el
    /// panel— hasta que el médico registra una acción de resolución. Incluye consultas de
    /// alertas activas y por paciente, y el cierre auditado de la alerta.
    /// </summary>
    public class AlertaLogic
    {
        private readonly NegocioDbContext _contexto;

        public AlertaLogic(NegocioDbContext contexto)
        {
            _contexto = contexto;
        }

        /// <summary>Alertas activas (pendientes de resolución), de la más reciente a la más antigua.</summary>
        public List<Alerta> ObtenerActivas()
            => _contexto.Alertas.AsNoTracking()
                .Where(a => a.Estado == EstadoAlerta.Activa)
                .OrderByDescending(a => a.FechaGeneracion)
                .ToList();

        /// <summary>Todas las alertas de un paciente, de la más reciente a la más antigua.</summary>
        public List<Alerta> ObtenerPorPaciente(int idPaciente)
            => _contexto.Alertas.AsNoTracking()
                .Where(a => a.IdPaciente == idPaciente)
                .OrderByDescending(a => a.FechaGeneracion)
                .ToList();

        /// <summary>Registra la acción de resolución del médico y cierra la alerta.</summary>
        public void Resolver(int idAlerta, string accionResolucion, string usuarioResponsable = "")
        {
            Alerta alerta = _contexto.Alertas.Find(idAlerta)
                ?? throw new ValidacionNegocioException($"No existe la alerta con Id {idAlerta}.");
            if (alerta.Estado == EstadoAlerta.Resuelta)
            {
                throw new ValidacionNegocioException("La alerta ya se encuentra resuelta.");
            }
            if (string.IsNullOrWhiteSpace(accionResolucion))
            {
                throw ReglasNegocio.Rechazar(
                    new List<string> { "La acción de resolución es obligatoria." },
                    $"Resolución de alerta Id {idAlerta}", usuarioResponsable);
            }

            alerta.Estado = EstadoAlerta.Resuelta;
            alerta.FechaResolucion = DateTime.Now;
            alerta.AccionResolucion = accionResolucion.Trim();
            alerta.IdUsuarioResolucion = ObtenerIdUsuario(usuarioResponsable);
            _contexto.SaveChanges();

            BitacoraService.Registrar(LogLevel.Info,
                $"Alerta Id {alerta.Id} resuelta: {alerta.AccionResolucion}",
                null, usuarioResponsable, ReglasNegocio.Capa);
        }

        /// <summary>Busca el Id del usuario de Negocio por su nombre (auditoría de resoluciones).</summary>
        private int? ObtenerIdUsuario(string usuarioResponsable)
        {
            if (string.IsNullOrWhiteSpace(usuarioResponsable))
            {
                return null;
            }
            return _contexto.Usuarios.FirstOrDefault(u => u.NombreUsuario == usuarioResponsable)?.Id;
        }
    }
}
