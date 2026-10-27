using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Negocio.DAL.Context;
using Negocio.DomainModel;
using Negocio.DomainModel.Enums;
using Negocio.DomainModel.Exceptions;
using Services.DomainModel;
using Services.Facade;

namespace Negocio.BLL
{
    /// <summary>Resultado de una exportación de historial: nombre de archivo sugerido y registros a renderizar.</summary>
    public record ExportacionHistorial(string NombreArchivoSugerido, List<ItemHistorial> Items);

    /// <summary>
    /// Lógica de reportes estadísticos (REQ-FUNC-011/012/013): reporte mensual de eficacia
    /// (% de mediciones dentro del rango terapéutico por paciente, clasificación óptimo /
    /// subóptimo / deficiente y consolidado global almacenado), reporte agrupado por
    /// diagnóstico, historial clínico unificado y exportación auditada.
    /// </summary>
    public class ReporteLogic
    {
        /// <summary>Porcentaje mínimo en rango para considerar el control ÓPTIMO.</summary>
        public const decimal UmbralOptimo = 80m;

        /// <summary>Porcentaje mínimo en rango para considerar el control SUBÓPTIMO (por debajo: deficiente).</summary>
        public const decimal UmbralSuboptimo = 50m;

        /// <summary>Indicador promedio por debajo del cual un diagnóstico se destaca como bajo desempeño.</summary>
        public const decimal UmbralBajoDesempeno = 60m;

        private readonly NegocioDbContext _contexto;

        public ReporteLogic(NegocioDbContext contexto)
        {
            _contexto = contexto;
        }

        /// <summary>Clasifica el control de un paciente según su porcentaje de mediciones en rango.</summary>
        public static ClasificacionControl Clasificar(decimal porcentajeEnRango)
        {
            if (porcentajeEnRango >= UmbralOptimo)
            {
                return ClasificacionControl.Optimo;
            }
            return porcentajeEnRango >= UmbralSuboptimo ? ClasificacionControl.Suboptimo : ClasificacionControl.Deficiente;
        }

        /// <summary>
        /// Genera (o regenera, de forma idempotente por período) el reporte mensual de eficacia:
        /// consolida las mediciones del mes de todos los pacientes activos, calcula el indicador
        /// global y la clasificación de cada paciente, y lo almacena para trazabilidad.
        /// </summary>
        public ReporteEstadistico GenerarReporteMensual(int anio, int mes, string usuarioResponsable = "")
        {
            (DateTime inicio, DateTime fin) = ValidarPeriodo(anio, mes, usuarioResponsable);

            var detalles = ObtenerDetalleDelPeriodo(inicio, fin, out int sinMediciones);

            decimal indicadorGlobal = detalles.Count == 0
                ? 0m
                : Math.Round(detalles.Average(d => d.PorcentajeEnRango), 2);
            int optimos = detalles.Count(d => d.Clasificacion == nameof(ClasificacionControl.Optimo));
            int suboptimos = detalles.Count(d => d.Clasificacion == nameof(ClasificacionControl.Suboptimo));
            int deficientes = detalles.Count(d => d.Clasificacion == nameof(ClasificacionControl.Deficiente));

            var detalleCompleto = new DetalleReporteMensual
            {
                Periodo = $"{anio:D4}-{mes:D2}",
                IndicadorGlobalEficacia = indicadorGlobal,
                PacientesConDatos = detalles.Count,
                PacientesSinMediciones = sinMediciones,
                Pacientes = detalles
            };

            ReporteEstadistico? existente = _contexto.ReportesEstadisticos
                .FirstOrDefault(r => r.Anio == anio && r.Mes == mes);
            ReporteEstadistico reporte;
            if (existente == null)
            {
                reporte = new ReporteEstadistico { Anio = anio, Mes = mes };
                _contexto.ReportesEstadisticos.Add(reporte);
            }
            else
            {
                reporte = existente; // regeneración del período: se actualiza la misma fila
            }

            reporte.FechaGeneracion = DateTime.Now;
            reporte.IndicadorGlobalEficacia = indicadorGlobal;
            reporte.CantidadOptimos = optimos;
            reporte.CantidadSuboptimos = suboptimos;
            reporte.CantidadDeficientes = deficientes;
            reporte.DetalleJson = JsonSerializer.Serialize(detalleCompleto);
            _contexto.SaveChanges();

            BitacoraService.Registrar(LogLevel.Info,
                $"Reporte mensual de eficacia {anio:D4}-{mes:D2} generado: indicador global {indicadorGlobal}% " +
                $"({optimos} óptimos, {suboptimos} subóptimos, {deficientes} deficientes; {sinMediciones} sin mediciones).",
                null, usuarioResponsable, ReglasNegocio.Capa);

            return reporte;
        }

        /// <summary>
        /// Reporte de eficacia agrupado por diagnóstico (REQ-FUNC-012), opcionalmente filtrado
        /// por un diagnóstico específico. Ordenado por indicador promedio ascendente para
        /// destacar primero los grupos con bajo desempeño.
        /// </summary>
        public List<FilaReporteDiagnostico> GenerarReportePorDiagnostico(int anio, int mes, int? idDiagnostico = null, string usuarioResponsable = "")
        {
            (DateTime inicio, DateTime fin) = ValidarPeriodo(anio, mes, usuarioResponsable);

            var detalles = ObtenerDetalleDelPeriodo(inicio, fin, out _);
            var nombresDiagnosticos = _contexto.Diagnosticos.AsNoTracking().ToDictionary(d => d.Id, d => d.Nombre);

            var grupos = detalles
                .GroupBy(d => d.IdDiagnostico)
                .Where(g => idDiagnostico == null || g.Key == idDiagnostico)
                .Select(g => new FilaReporteDiagnostico
                {
                    IdDiagnostico = g.Key,
                    NombreDiagnostico = g.Key.HasValue
                        ? nombresDiagnosticos.GetValueOrDefault(g.Key.Value, "(desconocido)")
                        : "Sin diagnóstico",
                    CantidadPacientes = g.Count(),
                    IndicadorPromedio = Math.Round(g.Average(p => p.PorcentajeEnRango), 2),
                    CantidadOptimos = g.Count(p => p.Clasificacion == nameof(ClasificacionControl.Optimo)),
                    CantidadSuboptimos = g.Count(p => p.Clasificacion == nameof(ClasificacionControl.Suboptimo)),
                    CantidadDeficientes = g.Count(p => p.Clasificacion == nameof(ClasificacionControl.Deficiente))
                })
                .ToList();

            foreach (FilaReporteDiagnostico fila in grupos)
            {
                fila.BajoDesempeno = fila.IndicadorPromedio < UmbralBajoDesempeno;
            }

            return grupos
                .OrderBy(g => g.IndicadorPromedio)
                .ThenBy(g => g.NombreDiagnostico)
                .ToList();
        }

        /// <summary>
        /// Historial clínico unificado de un paciente (REQ-FUNC-013): mediciones de RIN,
        /// alertas y eventos adversos, en orden cronológico descendente.
        /// </summary>
        public List<ItemHistorial> ObtenerHistorialPaciente(int idPaciente)
        {
            bool existe = _contexto.Pacientes.AsNoTracking().Any(p => p.Id == idPaciente);
            if (!existe)
            {
                throw new ValidacionNegocioException($"No existe el paciente con Id {idPaciente}.");
            }

            var items = new List<ItemHistorial>();

            foreach (MedicionRIN m in _contexto.MedicionesRIN.AsNoTracking().Where(m => m.IdPaciente == idPaciente).ToList())
            {
                items.Add(new ItemHistorial
                {
                    Fecha = m.FechaMedicion,
                    Tipo = "RIN",
                    Descripcion = $"RIN {m.ValorRIN} (canal {m.Canal}); criticidad {(m.NivelCriticidad?.ToString() ?? "pendiente")}" +
                                  (m.PorTendenciaPeligrosa ? ", por tendencia peligrosa." : ".")
                });
            }

            foreach (Alerta a in _contexto.Alertas.AsNoTracking().Where(a => a.IdPaciente == idPaciente).ToList())
            {
                items.Add(new ItemHistorial
                {
                    Fecha = a.FechaGeneracion,
                    Tipo = "Alerta",
                    Descripcion = $"[{a.Tipo} / {a.Estado}] {a.Descripcion}" +
                                  (string.IsNullOrWhiteSpace(a.AccionResolucion) ? "" : $" — Resolución: {a.AccionResolucion}")
                });
            }

            foreach (EventoAdverso e in _contexto.EventosAdversos.AsNoTracking().Where(e => e.IdPaciente == idPaciente).ToList())
            {
                items.Add(new ItemHistorial
                {
                    Fecha = e.Fecha,
                    Tipo = "Evento adverso",
                    Descripcion = $"{e.Tipo} ({e.Gravedad}): {e.Descripcion} — Acción: {e.AccionTomada}"
                });
            }

            return items.OrderByDescending(i => i.Fecha).ToList();
        }

        /// <summary>
        /// Prepara la exportación del historial de un paciente (REQ-FUNC-013): devuelve el
        /// nombre de archivo sugerido y los registros a renderizar (el render final en PDF/Excel
        /// corresponde a la capa de presentación) y registra la acción en la auditoría.
        /// </summary>
        public ExportacionHistorial ExportarHistorialPaciente(int idPaciente, string formato, string usuarioResponsable = "")
        {
            bool formatoValido = string.Equals(formato, "PDF", StringComparison.OrdinalIgnoreCase)
                              || string.Equals(formato, "Excel", StringComparison.OrdinalIgnoreCase)
                              || string.Equals(formato, "JSON", StringComparison.OrdinalIgnoreCase);
            if (!formatoValido)
            {
                throw ReglasNegocio.Rechazar(
                    new List<string> { "El formato de exportación debe ser PDF, Excel o JSON." },
                    $"Exportación de historial del paciente Id {idPaciente}", usuarioResponsable);
            }

            Paciente paciente = _contexto.Pacientes.AsNoTracking().FirstOrDefault(p => p.Id == idPaciente)
                ?? throw new ValidacionNegocioException($"No existe el paciente con Id {idPaciente}.");

            List<ItemHistorial> items = ObtenerHistorialPaciente(idPaciente);
            string extension = formato.ToUpperInvariant() switch
            {
                "PDF" => "pdf",
                "EXCEL" => "xlsx",
                _ => "json"
            };
            string nombreLimpio = string.Concat(paciente.NombreCompleto.Where(char.IsLetterOrDigit));
            string archivo = $"Historial_{nombreLimpio}_{DateTime.Now:yyyyMMdd}.{extension}";

            BitacoraService.Registrar(LogLevel.Info,
                $"Exportación de historial solicitada para '{paciente.NombreCompleto}' (Id {paciente.Id}) en formato {formato.ToUpperInvariant()}: " +
                $"{items.Count} registro(s). Archivo sugerido: {archivo}",
                null, usuarioResponsable, ReglasNegocio.Capa);

            return new ExportacionHistorial(archivo, items);
        }

        /// <summary>Validaciones comunes de período: mes 1-12 y no futuro. Devuelve el rango [inicio, fin).</summary>
        private static (DateTime inicio, DateTime fin) ValidarPeriodo(int anio, int mes, string usuarioResponsable)
        {
            if (mes < 1 || mes > 12)
            {
                throw ReglasNegocio.Rechazar(
                    new List<string> { "El mes debe estar entre 1 y 12." },
                    $"Período {anio}-{mes}", usuarioResponsable);
            }

            DateTime inicio = new DateTime(anio, mes, 1);
            DateTime primerDiaMesActual = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            if (inicio > primerDiaMesActual)
            {
                throw ReglasNegocio.Rechazar(
                    new List<string> { "No puede generarse un reporte de un período futuro." },
                    $"Período {anio}-{mes}", usuarioResponsable);
            }

            return (inicio, inicio.AddMonths(1));
        }

        /// <summary>
        /// Calcula, para cada paciente activo con HC, su porcentaje de mediciones dentro del
        /// rango en el período. Devuelve también (out) cuántos pacientes no registraron mediciones.
        /// </summary>
        private List<DetallePaciente> ObtenerDetalleDelPeriodo(DateTime inicio, DateTime fin, out int sinMediciones)
        {
            var activos = _contexto.Pacientes.AsNoTracking()
                .Where(p => p.Estado == EstadoPaciente.Activo)
                .ToList();
            var historias = _contexto.HistoriasClinicas.AsNoTracking()
                .ToDictionary(h => h.IdPaciente);
            var mediciones = _contexto.MedicionesRIN.AsNoTracking()
                .Where(m => m.FechaMedicion >= inicio && m.FechaMedicion < fin)
                .ToList();

            var detalles = new List<DetallePaciente>();
            int contadorSinMediciones = 0;
            foreach (Paciente paciente in activos)
            {
                if (!historias.TryGetValue(paciente.Id, out HistoriaClinica? hc))
                {
                    continue; // sin HC no hay rango patrón con el cual evaluar
                }

                var propias = mediciones.Where(m => m.IdPaciente == paciente.Id).ToList();
                if (propias.Count == 0)
                {
                    contadorSinMediciones++;
                    continue;
                }

                int dentro = propias.Count(m => m.ValorRIN >= hc.LimiteInferiorRIN && m.ValorRIN <= hc.LimiteSuperiorRIN);
                decimal porcentaje = Math.Round((decimal)dentro / propias.Count * 100m, 2);
                detalles.Add(new DetallePaciente
                {
                    IdPaciente = paciente.Id,
                    IdDiagnostico = hc.IdDiagnostico,
                    NombreCompleto = paciente.NombreCompleto,
                    Mediciones = propias.Count,
                    PorcentajeEnRango = porcentaje,
                    Clasificacion = Clasificar(porcentaje).ToString()
                });
            }

            sinMediciones = contadorSinMediciones;
            return detalles;
        }

        /// <summary>Detalle serializable del reporte mensual (resumen ejecutivo + detalle por paciente).</summary>
        private sealed class DetalleReporteMensual
        {
            public string Periodo { get; set; } = string.Empty;
            public decimal IndicadorGlobalEficacia { get; set; }
            public int PacientesConDatos { get; set; }
            public int PacientesSinMediciones { get; set; }
            public List<DetallePaciente> Pacientes { get; set; } = new();
        }

        /// <summary>Detalle por paciente del reporte mensual.</summary>
        private sealed class DetallePaciente
        {
            public int IdPaciente { get; set; }
            public int? IdDiagnostico { get; set; }
            public string NombreCompleto { get; set; } = string.Empty;
            public int Mediciones { get; set; }
            public decimal PorcentajeEnRango { get; set; }
            public string Clasificacion { get; set; } = string.Empty;
        }
    }
}
