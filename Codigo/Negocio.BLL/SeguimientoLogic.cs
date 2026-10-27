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
    /// <summary>Resultado de registrar un intento de contacto del protocolo de seguimiento.</summary>
    public record ResultadoIntentoContacto(Seguimiento Intento, bool CerroAlerta, bool EscaladaAlMedico);

    /// <summary>
    /// Lógica de seguimiento clínico (REQ-FUNC-010): detecta plazos de reporte vencidos según
    /// la periodicidad configurada en la HC (último RIN + periodicidad), genera alertas de
    /// ausencia idempotentes, administra el protocolo progresivo de contacto (umbral de
    /// intentos fallidos → escalado al médico) y registra las decisiones clínicas, que pueden
    /// actualizar automáticamente la periodicidad de la HC.
    /// </summary>
    public class SeguimientoLogic
    {
        /// <summary>Intentos fallidos que disparan el escalado de la alerta al médico.</summary>
        public const int UmbralIntentosFallidos = 3;

        private readonly NegocioDbContext _contexto;

        public SeguimientoLogic(NegocioDbContext contexto)
        {
            _contexto = contexto;
        }

        /// <summary>
        /// Recorre los pacientes activos con historia clínica configurada y genera una alerta
        /// de ausencia por cada plazo de reporte vencido que no tenga ya una alerta activa del
        /// mismo tipo. Devuelve las alertas creadas en esta pasada.
        /// </summary>
        public List<Alerta> DetectarPlazosVencidos()
        {
            DateTime hoy = DateTime.Today;
            var creadas = new List<Alerta>();

            var idsActivos = _contexto.Pacientes.AsNoTracking()
                .Where(p => p.Estado == EstadoPaciente.Activo)
                .Select(p => p.Id)
                .ToHashSet();
            var historias = _contexto.HistoriasClinicas.AsNoTracking()
                .Where(h => idsActivos.Contains(h.IdPaciente))
                .ToList();

            foreach (HistoriaClinica hc in historias)
            {
                MedicionRIN? ultima = _contexto.MedicionesRIN.AsNoTracking()
                    .Where(m => m.IdPaciente == hc.IdPaciente)
                    .OrderByDescending(m => m.FechaMedicion).ThenByDescending(m => m.Id)
                    .FirstOrDefault();

                DateTime fechaBase = ultima?.FechaMedicion.Date ?? hc.FechaConfiguracion.Date;
                DateTime vencimiento = fechaBase.AddDays(hc.PeriodicidadDias);
                if (hoy <= vencimiento)
                {
                    continue; // dentro del plazo
                }

                bool yaTieneAlerta = _contexto.Alertas.Any(a =>
                    a.IdPaciente == hc.IdPaciente &&
                    a.Tipo == TipoAlerta.ReporteAusente &&
                    a.Estado == EstadoAlerta.Activa);
                if (yaTieneAlerta)
                {
                    continue; // ya está en protocolo
                }

                Paciente paciente = _contexto.Pacientes.Find(hc.IdPaciente)!;
                int diasVencido = (int)(hoy - vencimiento).TotalDays;
                var alerta = new Alerta
                {
                    IdPaciente = hc.IdPaciente,
                    IdMedicion = null,
                    Tipo = TipoAlerta.ReporteAusente,
                    Estado = EstadoAlerta.Activa,
                    Descripcion = $"Reporte vencido: '{paciente.NombreCompleto}' no registra RIN desde el {fechaBase:dd/MM/yyyy} " +
                                  $"(vencido hace {diasVencido} día(s); periodicidad configurada: {hc.PeriodicidadDias} días).",
                    NotificadaMedico = true,
                    NotificadaAdministrativo = true
                };
                _contexto.Alertas.Add(alerta);
                _contexto.SaveChanges();
                creadas.Add(alerta);

                BitacoraService.Registrar(LogLevel.Warning,
                    $"Alerta de ausencia generada para '{paciente.NombreCompleto}' (Id {paciente.Id}): {alerta.Descripcion}",
                    null, "", ReglasNegocio.Capa);
            }

            return creadas;
        }

        /// <summary>
        /// Registra un intento de contacto del protocolo progresivo. Si el resultado es
        /// exitoso, cierra la alerta de ausencia; si se alcanza el umbral de intentos
        /// fallidos (desde la generación de la alerta), la escala al médico.
        /// </summary>
        public ResultadoIntentoContacto RegistrarIntentoContacto(Seguimiento intento, string usuarioResponsable = "")
        {
            if (intento == null)
            {
                throw new ArgumentNullException(nameof(intento));
            }

            Paciente paciente = _contexto.Pacientes.Find(intento.IdPaciente)
                ?? throw new ValidacionNegocioException($"No existe el paciente con Id {intento.IdPaciente}.");
            if (paciente.Estado != EstadoPaciente.Activo)
            {
                throw new ValidacionNegocioException("Solo pueden registrarse intentos de contacto de pacientes activos.");
            }

            Alerta alerta = _contexto.Alertas
                .Where(a => a.IdPaciente == intento.IdPaciente &&
                            a.Tipo == TipoAlerta.ReporteAusente &&
                            a.Estado == EstadoAlerta.Activa)
                .OrderByDescending(a => a.FechaGeneracion)
                .FirstOrDefault()
                ?? throw new ValidacionNegocioException(
                    "El paciente no posee una alerta de ausencia activa (el protocolo de contacto no aplica).");

            intento.FechaRegistro = DateTime.Now;
            intento.IdUsuarioRegistro = ObtenerIdUsuario(usuarioResponsable);
            intento.DecisionClinica = DecisionClinica.Ninguna;
            _contexto.Seguimientos.Add(intento);
            _contexto.SaveChanges();

            bool cerro = false;
            bool escalo = false;

            if (intento.Resultado == ResultadoContacto.Exitoso)
            {
                alerta.Estado = EstadoAlerta.Resuelta;
                alerta.FechaResolucion = DateTime.Now;
                alerta.AccionResolucion = $"Contacto exitoso ({intento.TipoContacto}) registrado por el administrativo.";
                alerta.IdUsuarioResolucion = intento.IdUsuarioRegistro;
                _contexto.SaveChanges();
                cerro = true;

                BitacoraService.Registrar(LogLevel.Info,
                    $"Contacto exitoso con '{paciente.NombreCompleto}': alerta de ausencia Id {alerta.Id} cerrada.",
                    null, usuarioResponsable, ReglasNegocio.Capa);
            }
            else
            {
                int fallidos = _contexto.Seguimientos.AsNoTracking()
                    .Count(s => s.IdPaciente == intento.IdPaciente &&
                                s.Resultado == ResultadoContacto.Fallido &&
                                s.FechaRegistro >= alerta.FechaGeneracion);

                if (fallidos >= UmbralIntentosFallidos && !alerta.EscaladaAMedico)
                {
                    alerta.EscaladaAMedico = true;
                    alerta.FechaEscalada = DateTime.Now;
                    _contexto.SaveChanges();
                    escalo = true;

                    BitacoraService.Registrar(LogLevel.Warning,
                        $"ESCALADA al médico: '{paciente.NombreCompleto}' acumuló {fallidos} intento(s) fallido(s) de contacto (alerta Id {alerta.Id}).",
                        null, usuarioResponsable, ReglasNegocio.Capa);
                }
                else
                {
                    BitacoraService.Registrar(LogLevel.Info,
                        $"Intento de contacto fallido con '{paciente.NombreCompleto}' ({fallidos} acumulado(s); umbral {UmbralIntentosFallidos}).",
                        null, usuarioResponsable, ReglasNegocio.Capa);
                }
            }

            return new ResultadoIntentoContacto(intento, cerro, escalo);
        }

        /// <summary>
        /// Registra la decisión clínica del médico sobre el último intento del protocolo.
        /// Para la decisión 'Cambiar periodicidad' actualiza automáticamente la periodicidad
        /// de la HC; en todos los casos cierra la alerta de ausencia activa (el episodio pasa
        /// a manejo clínico).
        /// </summary>
        public Seguimiento RegistrarDecisionClinica(
            int idPaciente,
            DecisionClinica decision,
            string detalle,
            int? nuevaPeriodicidad = null,
            string usuarioResponsable = "")
        {
            if (decision == DecisionClinica.Ninguna)
            {
                throw ReglasNegocio.Rechazar(
                    new List<string> { "Debe indicar una decisión clínica válida." },
                    $"Decisión clínica del paciente Id {idPaciente}", usuarioResponsable);
            }
            if (string.IsNullOrWhiteSpace(detalle))
            {
                throw ReglasNegocio.Rechazar(
                    new List<string> { "El detalle de la decisión es obligatorio." },
                    $"Decisión clínica del paciente Id {idPaciente}", usuarioResponsable);
            }
            if (decision == DecisionClinica.CambiarPeriodicidad && nuevaPeriodicidad == null)
            {
                throw ReglasNegocio.Rechazar(
                    new List<string> { "Indique la nueva periodicidad (en días) para la decisión 'Cambiar periodicidad'." },
                    $"Decisión clínica del paciente Id {idPaciente}", usuarioResponsable);
            }
            if (decision != DecisionClinica.CambiarPeriodicidad && nuevaPeriodicidad != null)
            {
                throw ReglasNegocio.Rechazar(
                    new List<string> { "La nueva periodicidad solo aplica a la decisión 'Cambiar periodicidad'." },
                    $"Decisión clínica del paciente Id {idPaciente}", usuarioResponsable);
            }

            Paciente paciente = _contexto.Pacientes.Find(idPaciente)
                ?? throw new ValidacionNegocioException($"No existe el paciente con Id {idPaciente}.");

            Seguimiento ultimo = _contexto.Seguimientos
                .Where(s => s.IdPaciente == idPaciente)
                .OrderByDescending(s => s.FechaRegistro).ThenByDescending(s => s.Id)
                .FirstOrDefault()
                ?? throw new ValidacionNegocioException("No hay intentos de contacto registrados para este paciente.");

            ultimo.DecisionClinica = decision;
            ultimo.DetalleDecision = detalle.Trim();
            ultimo.IdUsuarioDecision = ObtenerIdUsuario(usuarioResponsable);
            ultimo.FechaDecision = DateTime.Now;

            if (decision == DecisionClinica.CambiarPeriodicidad && nuevaPeriodicidad.HasValue)
            {
                var hcLogic = new HistoriaClinicaLogic(_contexto);
                hcLogic.ActualizarPeriodicidad(idPaciente, nuevaPeriodicidad.Value, usuarioResponsable);
            }

            // La decisión clínica cierra el episodio: la alerta de ausencia activa se resuelve.
            Alerta? alertaActiva = _contexto.Alertas
                .Where(a => a.IdPaciente == idPaciente &&
                            a.Tipo == TipoAlerta.ReporteAusente &&
                            a.Estado == EstadoAlerta.Activa)
                .OrderByDescending(a => a.FechaGeneracion)
                .FirstOrDefault();
            if (alertaActiva != null)
            {
                alertaActiva.Estado = EstadoAlerta.Resuelta;
                alertaActiva.FechaResolucion = DateTime.Now;
                alertaActiva.AccionResolucion = $"Decisión clínica: {decision} — {detalle.Trim()}";
                alertaActiva.IdUsuarioResolucion = ultimo.IdUsuarioDecision;
            }

            _contexto.SaveChanges();

            BitacoraService.Registrar(LogLevel.Info,
                $"Decisión clínica registrada para '{paciente.NombreCompleto}': {decision} — {detalle.Trim()}" +
                (decision == DecisionClinica.CambiarPeriodicidad ? $" (periodicidad → {nuevaPeriodicidad} días)" : ""),
                null, usuarioResponsable, ReglasNegocio.Capa);

            return ultimo;
        }

        /// <summary>Historial de intentos y decisiones del paciente, del más reciente al más antiguo.</summary>
        public List<Seguimiento> ObtenerSeguimientos(int idPaciente)
            => _contexto.Seguimientos.AsNoTracking()
                .Where(s => s.IdPaciente == idPaciente)
                .OrderByDescending(s => s.FechaRegistro).ThenByDescending(s => s.Id)
                .ToList();

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
