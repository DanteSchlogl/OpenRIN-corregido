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
    /// Lógica de registro de eventos adversos (REQ-FUNC-003): verifica que el paciente esté
    /// activo y posea historia clínica configurada, valida que la fecha no sea futura y
    /// asocia el evento al historial clínico del paciente.
    /// </summary>
    public class EventoAdversoLogic
    {
        private readonly NegocioDbContext _contexto;

        public EventoAdversoLogic(NegocioDbContext contexto)
        {
            _contexto = contexto;
        }

        /// <summary>Registra un evento adverso en la historia clínica del paciente.</summary>
        public EventoAdverso Registrar(EventoAdverso evento, string usuarioResponsable = "")
        {
            if (evento == null)
            {
                throw new ArgumentNullException(nameof(evento));
            }

            Paciente paciente = _contexto.Pacientes.Find(evento.IdPaciente)
                ?? throw new ValidacionNegocioException($"No existe el paciente con Id {evento.IdPaciente}.");
            if (paciente.Estado != EstadoPaciente.Activo)
            {
                throw new ValidacionNegocioException("Solo pueden registrarse eventos adversos de pacientes activos.");
            }
            if (!_contexto.HistoriasClinicas.Any(h => h.IdPaciente == evento.IdPaciente))
            {
                throw new ValidacionNegocioException("El paciente no posee una historia clínica configurada.");
            }

            var errores = new List<string>();
            if (!ConsistenciaService.ValidarEntidad(evento, out List<string> erroresAtributos))
            {
                errores.AddRange(erroresAtributos);
            }
            if (evento.Fecha.Date > DateTime.Today)
            {
                errores.Add("La fecha del evento no puede ser futura.");
            }
            if (errores.Count > 0)
            {
                throw ReglasNegocio.Rechazar(errores, $"Registro de evento adverso del paciente Id {evento.IdPaciente}", usuarioResponsable);
            }

            evento.FechaRegistro = DateTime.Now;
            _contexto.EventosAdversos.Add(evento);
            _contexto.SaveChanges();

            BitacoraService.Registrar(LogLevel.Info,
                $"Evento adverso registrado: {evento.Tipo} ({evento.Gravedad}) para el paciente '{paciente.NombreCompleto}' (Id {paciente.Id}).",
                null, usuarioResponsable, ReglasNegocio.Capa);

            return evento;
        }

        /// <summary>Eventos adversos de un paciente, del más reciente al más antiguo.</summary>
        public List<EventoAdverso> ObtenerPorPaciente(int idPaciente)
            => _contexto.EventosAdversos.AsNoTracking()
                .Where(e => e.IdPaciente == idPaciente)
                .OrderByDescending(e => e.Fecha)
                .ToList();
    }
}
