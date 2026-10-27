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
    /// Lógica de configuración de la historia clínica hematológica (REQ-FUNC-002): valida
    /// el rango terapéutico (inferior &lt; superior), la medicación, la periodicidad y las
    /// fechas de control, y mantiene una única HC por paciente. Es el insumo indispensable
    /// del cálculo de criticidad de los reportes de RIN.
    /// </summary>
    public class HistoriaClinicaLogic
    {
        private readonly NegocioDbContext _contexto;

        public HistoriaClinicaLogic(NegocioDbContext contexto)
        {
            _contexto = contexto;
        }

        /// <summary>Crea o actualiza la historia clínica de un paciente activo.</summary>
        public HistoriaClinica Configurar(HistoriaClinica configuracion, string usuarioResponsable = "")
        {
            if (configuracion == null)
            {
                throw new ArgumentNullException(nameof(configuracion));
            }

            Paciente paciente = _contexto.Pacientes.Find(configuracion.IdPaciente)
                ?? throw new ValidacionNegocioException($"No existe el paciente con Id {configuracion.IdPaciente}.");
            if (paciente.Estado != EstadoPaciente.Activo)
            {
                throw new ValidacionNegocioException("Solo se puede configurar la historia clínica de pacientes activos.");
            }

            var errores = new List<string>();
            if (!ConsistenciaService.ValidarEntidad(configuracion, out List<string> erroresAtributos))
            {
                errores.AddRange(erroresAtributos);
            }
            if (configuracion.LimiteInferiorRIN >= configuracion.LimiteSuperiorRIN)
            {
                errores.Add("El límite inferior del rango terapéutico debe ser menor que el límite superior.");
            }
            if (configuracion.ProximaFechaControl.HasValue && configuracion.ProximaFechaControl.Value.Date <= DateTime.Today)
            {
                errores.Add("La fecha del próximo control debe ser posterior a la fecha actual.");
            }
            if (configuracion.IdDiagnostico.HasValue && !_contexto.Diagnosticos.Any(d => d.Id == configuracion.IdDiagnostico.Value))
            {
                errores.Add("El diagnóstico indicado no existe.");
            }
            if (errores.Count > 0)
            {
                throw ReglasNegocio.Rechazar(errores, $"Configuración de HC del paciente Id {configuracion.IdPaciente}", usuarioResponsable);
            }

            HistoriaClinica? existente = _contexto.HistoriasClinicas
                .FirstOrDefault(h => h.IdPaciente == configuracion.IdPaciente);

            if (existente == null)
            {
                configuracion.FechaConfiguracion = DateTime.Now;
                configuracion.UltimaActualizacion = DateTime.Now;
                _contexto.HistoriasClinicas.Add(configuracion);
                _contexto.SaveChanges();

                BitacoraService.Registrar(LogLevel.Info,
                    $"Historia clínica creada para el paciente '{paciente.NombreCompleto}' (Id {paciente.Id}); rango {configuracion.LimiteInferiorRIN}-{configuracion.LimiteSuperiorRIN}, periodicidad {configuracion.PeriodicidadDias} días.",
                    null, usuarioResponsable, ReglasNegocio.Capa);

                return configuracion;
            }

            existente.IdDiagnostico = configuracion.IdDiagnostico;
            existente.LimiteInferiorRIN = configuracion.LimiteInferiorRIN;
            existente.LimiteSuperiorRIN = configuracion.LimiteSuperiorRIN;
            existente.Medicamento = configuracion.Medicamento;
            existente.Dosis = configuracion.Dosis;
            existente.PeriodicidadDias = configuracion.PeriodicidadDias;
            existente.ProximaFechaControl = configuracion.ProximaFechaControl;
            existente.UltimaActualizacion = DateTime.Now;
            _contexto.SaveChanges();

            BitacoraService.Registrar(LogLevel.Info,
                $"Historia clínica actualizada del paciente '{paciente.NombreCompleto}' (Id {paciente.Id}); rango {existente.LimiteInferiorRIN}-{existente.LimiteSuperiorRIN}, periodicidad {existente.PeriodicidadDias} días.",
                null, usuarioResponsable, ReglasNegocio.Capa);

            return existente;
        }

        /// <summary>Obtiene la historia clínica de un paciente (o null si no está configurada).</summary>
        public HistoriaClinica? ObtenerPorPaciente(int idPaciente)
            => _contexto.HistoriasClinicas.AsNoTracking()
                .FirstOrDefault(h => h.IdPaciente == idPaciente);

        /// <summary>Actualiza la periodicidad esperada de reporte (usado por el seguimiento clínico, REQ-FUNC-010).</summary>
        public void ActualizarPeriodicidad(int idPaciente, int dias, string usuarioResponsable = "")
        {
            if (dias < 1 || dias > 365)
            {
                throw ReglasNegocio.Rechazar(
                    new List<string> { "La periodicidad debe estar entre 1 y 365 días." },
                    $"Actualización de periodicidad del paciente Id {idPaciente}", usuarioResponsable);
            }

            HistoriaClinica hc = _contexto.HistoriasClinicas.FirstOrDefault(h => h.IdPaciente == idPaciente)
                ?? throw new ValidacionNegocioException("El paciente no posee una historia clínica configurada.");

            int anterior = hc.PeriodicidadDias;
            hc.PeriodicidadDias = dias;
            hc.UltimaActualizacion = DateTime.Now;
            _contexto.SaveChanges();

            BitacoraService.Registrar(LogLevel.Info,
                $"Periodicidad de reporte del paciente Id {idPaciente} actualizada de {anterior} a {dias} días.",
                null, usuarioResponsable, ReglasNegocio.Capa);
        }
    }
}
