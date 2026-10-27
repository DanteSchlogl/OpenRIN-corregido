using Microsoft.EntityFrameworkCore;
using Negocio.DAL.Context;
using Negocio.DomainModel;
using Negocio.DomainModel.Enums;
using Negocio.DomainModel.Exceptions;
using Services.DomainModel;
using Services.Facade;
using Usuario = Negocio.DomainModel.Usuario;

namespace Negocio.BLL
{
    /// <summary>Resultado de aceptar una reasignación: el turno nuevo creado y las notificaciones emitidas a los pacientes.</summary>
    public record ResultadoReasignacion(Turno TurnoNuevo, List<string> Notificaciones);

    /// <summary>
    /// Lógica de agenda y turnos (REQ-FUNC-009): agendamiento con validaciones (paciente activo,
    /// médico habilitado, fecha futura, sin doble reserva), y reasignación inteligente que cruza
    /// pacientes ESTABLES con turno (última criticidad Baja) contra pacientes CRÍTICOS sin turno
    /// (última criticidad Alta). La aceptación libera el turno del estable y lo asigna al crítico
    /// notificando a ambos; el rechazo con duda clínica se escala al médico.
    /// </summary>
    public class TurnoLogic
    {
        private readonly NegocioDbContext _contexto;

        public TurnoLogic(NegocioDbContext contexto)
        {
            _contexto = contexto;
        }

        /// <summary>Agenda de turnos en un rango de fechas, ordenada cronológicamente.</summary>
        public List<Turno> ObtenerAgenda(DateTime desde, DateTime hasta)
            => _contexto.Turnos.AsNoTracking()
                .Include(t => t.Paciente)
                .Where(t => t.FechaHora >= desde && t.FechaHora <= hasta)
                .OrderBy(t => t.FechaHora)
                .ToList();

        /// <summary>Indica si el paciente posee un turno próximo activo (pendiente o confirmado).</summary>
        public bool TieneTurnoProximo(int idPaciente)
            => _contexto.Turnos.Any(t =>
                t.IdPaciente == idPaciente &&
                t.FechaHora > DateTime.Now &&
                (t.Estado == EstadoTurno.Pendiente || t.Estado == EstadoTurno.Confirmado));

        /// <summary>Turnos próximos activos de un paciente, ordenados cronológicamente.</summary>
        public List<Turno> ObtenerTurnosProximos(int idPaciente)
            => _contexto.Turnos.AsNoTracking()
                .Where(t => t.IdPaciente == idPaciente && t.FechaHora > DateTime.Now &&
                            (t.Estado == EstadoTurno.Pendiente || t.Estado == EstadoTurno.Confirmado))
                .OrderBy(t => t.FechaHora)
                .ToList();

        /// <summary>Agenda un turno presencial validando las reglas de la agenda médica.</summary>
        public Turno AgendarTurno(Turno turno, string usuarioResponsable = "")
        {
            if (turno == null)
            {
                throw new ArgumentNullException(nameof(turno));
            }

            Paciente paciente = _contexto.Pacientes.Find(turno.IdPaciente)
                ?? throw new ValidacionNegocioException($"No existe el paciente con Id {turno.IdPaciente}.");
            if (paciente.Estado != EstadoPaciente.Activo)
            {
                throw new ValidacionNegocioException("Solo pueden agendarse turnos de pacientes activos.");
            }

            Usuario? medico = _contexto.Usuarios.Find(turno.IdUsuarioMedico);
            if (medico == null || !medico.Activo || !string.Equals(medico.Perfil, "medico", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidacionNegocioException("El médico indicado no existe o no está habilitado.");
            }

            var errores = new List<string>();
            if (turno.FechaHora <= DateTime.Now)
            {
                errores.Add("La fecha y hora del turno debe ser futura.");
            }
            if (_contexto.Turnos.Any(t =>
                    t.IdUsuarioMedico == turno.IdUsuarioMedico &&
                    t.FechaHora == turno.FechaHora &&
                    (t.Estado == EstadoTurno.Pendiente || t.Estado == EstadoTurno.Confirmado)))
            {
                errores.Add("El médico ya posee un turno agendado en ese horario.");
            }
            if (TieneTurnoProximo(turno.IdPaciente))
            {
                errores.Add("El paciente ya posee un turno próximo activo.");
            }
            if (errores.Count > 0)
            {
                throw ReglasNegocio.Rechazar(errores, $"Agenda de turno del paciente Id {turno.IdPaciente}", usuarioResponsable);
            }

            turno.Estado = EstadoTurno.Pendiente;
            turno.FechaCreacion = DateTime.Now;
            _contexto.Turnos.Add(turno);
            _contexto.SaveChanges();

            BitacoraService.Registrar(LogLevel.Info,
                $"Turno agendado: '{paciente.NombreCompleto}' con '{medico.NombreCompleto}' para el {turno.FechaHora:dd/MM/yyyy HH:mm}.",
                null, usuarioResponsable, ReglasNegocio.Capa);

            return turno;
        }

        /// <summary>
        /// Genera las sugerencias de reasignación: por cada paciente crítico sin turno
        /// (última criticidad Alta) ofrece los turnos próximos de pacientes estables
        /// (última criticidad Baja), ordenadas por la fecha del turno (la oportunidad más
        /// próxima primero).
        /// </summary>
        public List<SugerenciaReasignacion> GenerarSugerencias()
        {
            var activos = _contexto.Pacientes.AsNoTracking()
                .Where(p => p.Estado == EstadoPaciente.Activo)
                .Select(p => new { p.Id, p.NombreCompleto })
                .ToDictionary(p => p.Id, p => p.NombreCompleto);

            var ultimas = _contexto.MedicionesRIN.AsNoTracking()
                .GroupBy(m => m.IdPaciente)
                .Select(g => g.OrderByDescending(m => m.FechaMedicion).ThenByDescending(m => m.Id).First())
                .ToList();

            var criticos = ultimas
                .Where(u => u.NivelCriticidad == NivelCriticidad.Alta && activos.ContainsKey(u.IdPaciente))
                .Select(u => u.IdPaciente)
                .ToHashSet();
            var estables = ultimas
                .Where(u => u.NivelCriticidad == NivelCriticidad.Baja && activos.ContainsKey(u.IdPaciente))
                .Select(u => u.IdPaciente)
                .ToHashSet();

            var turnosFuturos = _contexto.Turnos.AsNoTracking()
                .Where(t => t.FechaHora > DateTime.Now &&
                            (t.Estado == EstadoTurno.Pendiente || t.Estado == EstadoTurno.Confirmado))
                .OrderBy(t => t.FechaHora)
                .ToList();

            var conTurno = turnosFuturos.Select(t => t.IdPaciente).ToHashSet();
            var criticosSinTurno = criticos.Where(id => !conTurno.Contains(id)).ToList();
            var turnosDeEstables = turnosFuturos.Where(t => estables.Contains(t.IdPaciente)).ToList();

            var sugerencias = new List<SugerenciaReasignacion>();
            foreach (var turno in turnosDeEstables)
            {
                foreach (int idCritico in criticosSinTurno)
                {
                    sugerencias.Add(new SugerenciaReasignacion
                    {
                        IdTurno = turno.Id,
                        IdPacienteEstable = turno.IdPaciente,
                        NombrePacienteEstable = activos[turno.IdPaciente],
                        IdPacienteCritico = idCritico,
                        NombrePacienteCritico = activos[idCritico],
                        FechaHoraTurno = turno.FechaHora,
                        IdUsuarioMedico = turno.IdUsuarioMedico,
                        Descripcion = $"Reasignar el turno del {turno.FechaHora:dd/MM/yyyy HH:mm} de " +
                                      $"'{activos[turno.IdPaciente]}' (estable) a '{activos[idCritico]}' (criticidad Alta, sin turno)."
                    });
                }
            }

            return sugerencias.OrderBy(s => s.FechaHoraTurno).ThenBy(s => s.NombrePacienteCritico).ToList();
        }

        /// <summary>
        /// Acepta una sugerencia: libera (cancela) el turno del paciente estable, crea el turno
        /// del paciente crítico en el mismo horario y notifica a ambos pacientes.
        /// </summary>
        public ResultadoReasignacion AceptarSugerencia(SugerenciaReasignacion sugerencia, string usuarioResponsable = "")
        {
            if (sugerencia == null)
            {
                throw new ArgumentNullException(nameof(sugerencia));
            }

            Turno turno = _contexto.Turnos.Find(sugerencia.IdTurno)
                ?? throw new ValidacionNegocioException($"No existe el turno con Id {sugerencia.IdTurno}.");
            if (turno.Estado != EstadoTurno.Pendiente && turno.Estado != EstadoTurno.Confirmado)
            {
                throw new ValidacionNegocioException("El turno sugerido ya no está activo (fue cancelado o atendido).");
            }

            Paciente estable = _contexto.Pacientes.Find(sugerencia.IdPacienteEstable)
                ?? throw new ValidacionNegocioException($"No existe el paciente con Id {sugerencia.IdPacienteEstable}.");
            Paciente critico = _contexto.Pacientes.Find(sugerencia.IdPacienteCritico)
                ?? throw new ValidacionNegocioException($"No existe el paciente con Id {sugerencia.IdPacienteCritico}.");
            if (critico.Estado != EstadoPaciente.Activo)
            {
                throw new ValidacionNegocioException("El paciente crítico ya no se encuentra activo.");
            }
            if (TieneTurnoProximo(critico.Id))
            {
                throw new ValidacionNegocioException("El paciente crítico ya posee un turno próximo (la sugerencia quedó obsoleta).");
            }

            using var transaccion = _contexto.Database.BeginTransaction();

            turno.Estado = EstadoTurno.Cancelado;
            turno.Observaciones = string.IsNullOrWhiteSpace(turno.Observaciones)
                ? $"Liberado para reasignación (paciente '{critico.NombreCompleto}' con criticidad Alta)."
                : $"{turno.Observaciones} | Liberado para reasignación (paciente '{critico.NombreCompleto}').";

            var nuevoTurno = new Turno
            {
                IdPaciente = critico.Id,
                IdUsuarioMedico = turno.IdUsuarioMedico,
                FechaHora = turno.FechaHora,
                Estado = EstadoTurno.Pendiente,
                Observaciones = $"Reasignado desde el turno del paciente '{estable.NombreCompleto}' (Id {estable.Id})."
            };
            _contexto.Turnos.Add(nuevoTurno);
            _contexto.SaveChanges();

            transaccion.Commit();

            var notificaciones = new List<string>
            {
                $"A '{estable.NombreCompleto}': su turno del {turno.FechaHora:dd/MM/yyyy HH:mm} fue reprogramado por optimización de la agenda; se le asignará un nuevo horario.",
                $"A '{critico.NombreCompleto}': se le asignó el turno del {nuevoTurno.FechaHora:dd/MM/yyyy HH:mm} por su criticidad clínica; aguarde confirmación."
            };
            foreach (string notificacion in notificaciones)
            {
                BitacoraService.Registrar(LogLevel.Info,
                    $"Notificación de agenda — {notificacion}",
                    null, usuarioResponsable, ReglasNegocio.Capa);
            }

            BitacoraService.Registrar(LogLevel.Info,
                $"Reasignación aceptada: turno Id {turno.Id} de '{estable.NombreCompleto}' → '{critico.NombreCompleto}' (nuevo turno Id {nuevoTurno.Id}, {nuevoTurno.FechaHora:dd/MM/yyyy HH:mm}).",
                null, usuarioResponsable, ReglasNegocio.Capa);

            return new ResultadoReasignacion(nuevoTurno, notificaciones);
        }

        /// <summary>
        /// Rechaza una sugerencia registrando el motivo. Si el rechazo responde a una duda
        /// clínica (<paramref name="escalarAlMedico"/>), la decisión se escala al médico
        /// mediante una alerta activa. El turno del paciente estable no se modifica.
        /// </summary>
        public Alerta? RechazarSugerencia(SugerenciaReasignacion sugerencia, string motivo, bool escalarAlMedico, string usuarioResponsable = "")
        {
            if (sugerencia == null)
            {
                throw new ArgumentNullException(nameof(sugerencia));
            }
            if (string.IsNullOrWhiteSpace(motivo))
            {
                throw ReglasNegocio.Rechazar(
                    new List<string> { "El motivo del rechazo es obligatorio." },
                    $"Rechazo de sugerencia de reasignación (turno Id {sugerencia.IdTurno})", usuarioResponsable);
            }

            BitacoraService.Registrar(LogLevel.Info,
                $"Sugerencia rechazada: {sugerencia.Descripcion} — Motivo: {motivo.Trim()}",
                null, usuarioResponsable, ReglasNegocio.Capa);

            if (!escalarAlMedico)
            {
                return null;
            }

            var escalacion = new Alerta
            {
                IdPaciente = sugerencia.IdPacienteCritico,
                Tipo = TipoAlerta.Otro,
                Descripcion = $"Escalación al médico: el administrativo rechazó la reasignación del turno " +
                              $"{sugerencia.FechaHoraTurno:dd/MM/yyyy HH:mm} para el paciente crítico " +
                              $"'{sugerencia.NombrePacienteCritico}'. Motivo: {motivo.Trim()}",
                Estado = EstadoAlerta.Activa,
                NotificadaMedico = true
            };
            _contexto.Alertas.Add(escalacion);
            _contexto.SaveChanges();

            BitacoraService.Registrar(LogLevel.Warning,
                $"ESCALADA al médico: decisión pendiente sobre la reasignación del paciente '{sugerencia.NombrePacienteCritico}'.",
                null, usuarioResponsable, ReglasNegocio.Capa);

            return escalacion;
        }
    }
}
