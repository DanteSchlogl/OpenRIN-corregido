using Microsoft.EntityFrameworkCore;
using Negocio.DAL.Context;
using Negocio.DomainModel;
using Negocio.DomainModel.Enums;
using Negocio.DomainModel.Exceptions;
using Services.DomainModel;
using Services.Facade;

namespace Negocio.BLL
{
    /// <summary>Resultado del reporte de una medición: la medición clasificada y, si correspondió, la alerta generada.</summary>
    public record ResultadoReporteRIN(MedicionRIN Medicion, Alerta? AlertaGenerada);

    /// <summary>
    /// Lógica de triaje (REQ-FUNC-004/005/008): ingesta de valores de RIN con validaciones
    /// (rango biológicamente posible, fecha no futura, paciente activo con HC configurada),
    /// cálculo automático de criticidad (Baja dentro del rango · Media fuera por menos de ±0.5 ·
    /// Alta fuera por ±0.5 o más), detección de tendencia peligrosa (dos reportes consecutivos
    /// en el borde ±0.1) y disparo automático de la alerta urgente.
    /// </summary>
    public class MedicionRINLogic
    {
        /// <summary>Distancia mínima fuera del rango para criticidad Alta (REQ-FUNC-005).</summary>
        public const decimal DistanciaAlta = 0.5m;

        /// <summary>Margen considerado "borde del rango" para la tendencia peligrosa (REQ-FUNC-008).</summary>
        public const decimal MargenBorde = 0.1m;

        private readonly NegocioDbContext _contexto;

        public MedicionRINLogic(NegocioDbContext contexto)
        {
            _contexto = contexto;
        }

        /// <summary>Última medición registrada de un paciente (la más reciente por fecha).</summary>
        public MedicionRIN? ObtenerUltima(int idPaciente)
            => _contexto.MedicionesRIN.AsNoTracking()
                .Where(m => m.IdPaciente == idPaciente)
                .OrderByDescending(m => m.FechaMedicion).ThenByDescending(m => m.Id)
                .FirstOrDefault();

        /// <summary>Historial completo de mediciones de un paciente, del más reciente al más antiguo.</summary>
        public List<MedicionRIN> ObtenerHistorial(int idPaciente)
            => _contexto.MedicionesRIN.AsNoTracking()
                .Where(m => m.IdPaciente == idPaciente)
                .OrderByDescending(m => m.FechaMedicion).ThenByDescending(m => m.Id)
                .ToList();

        /// <summary>
        /// Registra un reporte de RIN (digital o telefónico), lo clasifica automáticamente
        /// y genera la alerta urgente si la criticidad resulta Alta.
        /// </summary>
        public ResultadoReporteRIN Reportar(MedicionRIN medicion, string usuarioResponsable = "")
        {
            if (medicion == null)
            {
                throw new ArgumentNullException(nameof(medicion));
            }

            Paciente paciente = _contexto.Pacientes.Find(medicion.IdPaciente)
                ?? throw new ValidacionNegocioException($"No existe el paciente con Id {medicion.IdPaciente}.");
            if (paciente.Estado != EstadoPaciente.Activo)
            {
                throw new ValidacionNegocioException("Solo pueden reportarse mediciones de pacientes activos.");
            }

            HistoriaClinica hc = _contexto.HistoriasClinicas.AsNoTracking()
                .FirstOrDefault(h => h.IdPaciente == medicion.IdPaciente)
                ?? throw new ValidacionNegocioException(
                    "El paciente no posee una historia clínica configurada (se requiere el rango terapéutico para el triaje).");

            var errores = new List<string>();
            if (!ConsistenciaService.ValidarEntidad(medicion, out List<string> erroresAtributos))
            {
                errores.AddRange(erroresAtributos);
            }
            if (medicion.FechaMedicion.Date > DateTime.Today)
            {
                errores.Add("La fecha de la medición no puede ser futura.");
            }
            if (errores.Count > 0)
            {
                throw ReglasNegocio.Rechazar(errores, $"Reporte de RIN del paciente Id {medicion.IdPaciente}", usuarioResponsable);
            }

            medicion.FechaRegistro = DateTime.Now;
            medicion.PorTendenciaPeligrosa = false;

            // ---- Criticidad base (REQ-FUNC-005) ----
            medicion.NivelCriticidad = CalcularCriticidad(medicion.ValorRIN, hc.LimiteInferiorRIN, hc.LimiteSuperiorRIN);

            // ---- Tendencia peligrosa (REQ-FUNC-008): dos reportes consecutivos en el borde (±0.1) ----
            MedicionRIN? anterior = _contexto.MedicionesRIN.AsNoTracking()
                .Where(m => m.IdPaciente == medicion.IdPaciente)
                .OrderByDescending(m => m.FechaMedicion).ThenByDescending(m => m.Id)
                .FirstOrDefault();

            if (anterior != null
                && EnBordeDelRango(medicion.ValorRIN, hc)
                && EnBordeDelRango(anterior.ValorRIN, hc))
            {
                medicion.NivelCriticidad = NivelCriticidad.Alta;
                medicion.PorTendenciaPeligrosa = true;
            }

            _contexto.MedicionesRIN.Add(medicion);
            _contexto.SaveChanges();

            BitacoraService.Registrar(LogLevel.Info,
                $"RIN reportado: {medicion.ValorRIN} (canal {medicion.Canal}) para '{paciente.NombreCompleto}' (Id {paciente.Id}); criticidad {medicion.NivelCriticidad}{(medicion.PorTendenciaPeligrosa ? " por TENDENCIA PELIGROSA" : "")}.",
                null, usuarioResponsable, ReglasNegocio.Capa);

            // ---- Alerta automática si la criticidad es Alta (REQ-FUNC-005 → 007/008) ----
            Alerta? alerta = null;
            if (medicion.NivelCriticidad == NivelCriticidad.Alta)
            {
                alerta = new Alerta
                {
                    IdPaciente = paciente.Id,
                    IdMedicion = medicion.Id,
                    Tipo = medicion.PorTendenciaPeligrosa ? TipoAlerta.TendenciaPeligrosa : TipoAlerta.CriticidadAlta,
                    NivelCriticidad = NivelCriticidad.Alta,
                    Descripcion = medicion.PorTendenciaPeligrosa
                        ? $"Tendencia peligrosa: dos reportes consecutivos en el borde del rango [{hc.LimiteInferiorRIN}-{hc.LimiteSuperiorRIN}] (último RIN {medicion.ValorRIN})."
                        : $"Valor de RIN {medicion.ValorRIN} fuera del rango terapéutico [{hc.LimiteInferiorRIN}-{hc.LimiteSuperiorRIN}] con criticidad Alta.",
                    Estado = EstadoAlerta.Activa,
                    NotificadaMedico = true,
                    NotificadaAdministrativo = true
                };
                _contexto.Alertas.Add(alerta);
                _contexto.SaveChanges();

                BitacoraService.Registrar(LogLevel.Warning,
                    $"ALERTA {alerta.Tipo} activada para '{paciente.NombreCompleto}' (Id {paciente.Id}): {alerta.Descripcion}",
                    null, usuarioResponsable, ReglasNegocio.Capa);
            }

            return new ResultadoReporteRIN(medicion, alerta);
        }

        /// <summary>
        /// Criticidad según la distancia al rango: Baja (dentro), Media (fuera por menos de 0.5)
        /// y Alta (fuera por 0.5 o más). Regla del REQ-FUNC-005.
        /// </summary>
        public static NivelCriticidad CalcularCriticidad(decimal valor, decimal limiteInferior, decimal limiteSuperior)
        {
            if (valor >= limiteInferior && valor <= limiteSuperior)
            {
                return NivelCriticidad.Baja;
            }

            decimal distancia = valor < limiteInferior ? limiteInferior - valor : valor - limiteSuperior;
            return distancia < DistanciaAlta ? NivelCriticidad.Media : NivelCriticidad.Alta;
        }

        /// <summary>Indica si el valor está dentro del margen de ±0.1 alrededor de alguno de los límites.</summary>
        private static bool EnBordeDelRango(decimal valor, HistoriaClinica hc)
            => Math.Abs(valor - hc.LimiteInferiorRIN) <= MargenBorde
            || Math.Abs(valor - hc.LimiteSuperiorRIN) <= MargenBorde;
    }
}
