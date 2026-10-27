using Microsoft.EntityFrameworkCore;
using Negocio.DAL.Auditoria;
using Negocio.DAL.Context;
using Negocio.DomainModel;

namespace Negocio.BLL
{
    /// <summary>
    /// Control de cambios (T06b): expone el historial de auditoría por entidad y registro, y
    /// permite recomponer el estado anterior de un objeto a partir de la fotografía previa.
    /// La restauración pasa por el mismo punto de guardado que el resto del sistema: la fila
    /// vuelve a firmarse (DVH/DVV) y la propia restauración queda auditada como un cambio más.
    /// </summary>
    public class AuditoriaLogic
    {
        private readonly NegocioDbContext _contexto;

        /// <summary>Crea la lógica de auditoría sobre el contexto de negocio indicado.</summary>
        public AuditoriaLogic(NegocioDbContext contexto)
        {
            _contexto = contexto;
        }

        /// <summary>
        /// Historial de cambios (más recientes primero), filtrable por entidad y/o número de
        /// registro. Responde a "¿quién, cuándo y qué?" para la trazabilidad solicitada.
        /// </summary>
        public List<CambioAuditado> ObtenerHistorial(string? entidad = null, int? idRegistro = null, int maximo = 200)
        {
            return _contexto.AuditoriaCambios.AsNoTracking()
                .Where(c => (entidad == null || c.Entidad == entidad)
                    && (idRegistro == null || c.IdRegistro == idRegistro))
                .OrderByDescending(c => c.Id)
                .Take(maximo)
                .ToList();
        }

        /// <summary>
        /// Recomponer el estado anterior de un objeto: aplica la fotografía previa del cambio
        /// indicado y persiste (con refirma de integridad y nueva auditoría). Devuelve un
        /// mensaje apto para la interfaz.
        /// </summary>
        public (bool Ok, string Mensaje) Restaurar(int idCambio, string usuario)
        {
            CambioAuditado? cambio = _contexto.AuditoriaCambios.AsNoTracking()
                .FirstOrDefault(c => c.Id == idCambio);
            if (cambio == null)
            {
                return (false, "El registro de auditoría indicado no existe.");
            }
            if (string.IsNullOrEmpty(cambio.DatosAnteriores))
            {
                return (false, "Este cambio no tiene un estado anterior recomponible (es un alta).");
            }

            (object? entidad, bool existe) = CargarEntidad(cambio.Entidad, cambio.IdRegistro);
            if (!existe || entidad == null)
            {
                return (false, "El registro original ya no existe en la base de datos (no puede recomponerse sobre una fila eliminada).");
            }

            var entrada = _contexto.Entry(entidad);
            AuditoriaDeCambios.Aplicar(entrada, cambio.DatosAnteriores);
            _contexto.UsuarioAuditoria = usuario;
            _contexto.SaveChanges();
            return (true, "Estado anterior recompuesto correctamente.");
        }

        /// <summary>Carga la entidad indicada por nombre de tabla y número de registro.</summary>
        private (object? Entidad, bool Existe) CargarEntidad(string entidad, int id)
        {
            switch (entidad)
            {
                case "Pacientes": { var e = _contexto.Pacientes.FirstOrDefault(x => x.Id == id); return (e, e != null); }
                case "ObrasSociales": { var e = _contexto.ObrasSociales.FirstOrDefault(x => x.Id == id); return (e, e != null); }
                case "Diagnosticos": { var e = _contexto.Diagnosticos.FirstOrDefault(x => x.Id == id); return (e, e != null); }
                case "HistoriasClinicas": { var e = _contexto.HistoriasClinicas.FirstOrDefault(x => x.Id == id); return (e, e != null); }
                case "EventosAdversos": { var e = _contexto.EventosAdversos.FirstOrDefault(x => x.Id == id); return (e, e != null); }
                case "MedicionesRIN": { var e = _contexto.MedicionesRIN.FirstOrDefault(x => x.Id == id); return (e, e != null); }
                case "Alertas": { var e = _contexto.Alertas.FirstOrDefault(x => x.Id == id); return (e, e != null); }
                case "Turnos": { var e = _contexto.Turnos.FirstOrDefault(x => x.Id == id); return (e, e != null); }
                case "Seguimientos": { var e = _contexto.Seguimientos.FirstOrDefault(x => x.Id == id); return (e, e != null); }
                case "Usuarios": { var e = _contexto.Usuarios.FirstOrDefault(x => x.Id == id); return (e, e != null); }
                case "ReportesEstadisticos": { var e = _contexto.ReportesEstadisticos.FirstOrDefault(x => x.Id == id); return (e, e != null); }
                default: return (null, false);
            }
        }
    }
}
