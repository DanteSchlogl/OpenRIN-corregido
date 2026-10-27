using Microsoft.EntityFrameworkCore;
using Negocio.DAL.Context;
using Negocio.DomainModel;
using Negocio.DomainModel.Enums;

namespace Negocio.BLL
{
    /// <summary>
    /// Panel de monitoreo en tiempo real (REQ-FUNC-006): ranking de pacientes activos
    /// ordenado por criticidad (Alta → Media → Baja) y, dentro de cada nivel, por antigüedad
    /// del último reporte (los más antiguos primero: llevan más tiempo sin reevaluación).
    /// Los pacientes sin reportes quedan al final. El panel se recalcula en cada consulta,
    /// de modo que la interfaz siempre muestra el estado vigente.
    /// </summary>
    public class PanelMonitoreoLogic
    {
        private readonly NegocioDbContext _contexto;

        public PanelMonitoreoLogic(NegocioDbContext contexto)
        {
            _contexto = contexto;
        }

        /// <summary>Obtiene el ranking de monitoreo de todos los pacientes activos.</summary>
        public List<FilaPanelMonitoreo> ObtenerPanel()
        {
            var pacientes = _contexto.Pacientes.AsNoTracking()
                .Where(p => p.Estado == EstadoPaciente.Activo)
                .Select(p => new { p.Id, p.NombreCompleto, p.DNI })
                .ToList();

            var mediciones = _contexto.MedicionesRIN.AsNoTracking()
                .OrderByDescending(m => m.FechaMedicion).ThenByDescending(m => m.Id)
                .ToList();

            var alertasActivas = _contexto.Alertas.AsNoTracking()
                .Where(a => a.Estado == EstadoAlerta.Activa)
                .Select(a => a.IdPaciente)
                .Distinct()
                .ToHashSet();

            var filas = new List<FilaPanelMonitoreo>();
            foreach (var p in pacientes)
            {
                MedicionRIN? ultima = mediciones.FirstOrDefault(m => m.IdPaciente == p.Id);
                filas.Add(new FilaPanelMonitoreo
                {
                    IdPaciente = p.Id,
                    NombreCompleto = p.NombreCompleto,
                    DNI = p.DNI,
                    UltimoValorRIN = ultima?.ValorRIN,
                    FechaUltimoReporte = ultima?.FechaMedicion,
                    NivelCriticidad = ultima?.NivelCriticidad,
                    PorTendenciaPeligrosa = ultima?.PorTendenciaPeligrosa ?? false,
                    TieneAlertaActiva = alertasActivas.Contains(p.Id),
                    DiasDesdeUltimoReporte = ultima == null
                        ? null
                        : (int)(DateTime.Today - ultima.FechaMedicion.Date).TotalDays
                });
            }

            return filas
                .OrderBy(f => OrdenDeCriticidad(f.NivelCriticidad))
                .ThenBy(f => f.FechaUltimoReporte ?? DateTime.MaxValue)
                .ToList();
        }

        private static int OrdenDeCriticidad(NivelCriticidad? nivel) => nivel switch
        {
            NivelCriticidad.Alta => 0,
            NivelCriticidad.Media => 1,
            NivelCriticidad.Baja => 2,
            _ => 3
        };
    }
}
