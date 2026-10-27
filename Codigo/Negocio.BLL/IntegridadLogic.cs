using Microsoft.EntityFrameworkCore;
using Negocio.DAL.Context;
using Negocio.DomainModel;

namespace Negocio.BLL
{
    /// <summary>Resultado de una auditoría de integridad (DVH/DVV).</summary>
    public record ResultadoAuditoria(List<string> TablasVerificadas, List<string> Problemas, int FilasAuditadas);

    /// <summary>
    /// Auditoría y reparación de integridad (DVH/DVV): verifica que el dígito horizontal de cada
    /// fila corresponda a sus datos y que el dígito vertical de cada tabla coincida con el
    /// conjunto de sus filas. La reparación (recalculación total) queda disponible para
    /// restablecer las firmas tras una detección.
    /// </summary>
    public class IntegridadLogic
    {
        /// <summary>Fila para auditoría: Id, DVH almacenado y la entidad materializada.</summary>
        private sealed record FilaAuditoria(int Id, string? Dvh, object Entidad);

        private readonly NegocioDbContext _contexto;
        private readonly List<(string Tabla, Func<bool, List<FilaAuditoria>> Lector)> _tablas;

        public IntegridadLogic(NegocioDbContext contexto)
        {
            _contexto = contexto;
            _tablas = new List<(string, Func<bool, List<FilaAuditoria>>)>
            {
                ("Pacientes", track =>
                {
                    var dvs = _contexto.Pacientes.AsNoTracking().Select(p => new { p.Id, D = EF.Property<string>(p, "DVH") }).ToDictionary(x => x.Id, x => x.D);
                    List<Paciente> entidades = track ? _contexto.Pacientes.ToList() : _contexto.Pacientes.AsNoTracking().ToList();
                    return entidades.Select(p => new FilaAuditoria(p.Id, (string?)dvs.GetValueOrDefault(p.Id), p)).ToList();
                }),
                ("ObrasSociales", track =>
                {
                    var dvs = _contexto.ObrasSociales.AsNoTracking().Select(o => new { o.Id, D = EF.Property<string>(o, "DVH") }).ToDictionary(x => x.Id, x => x.D);
                    List<ObraSocial> entidades = track ? _contexto.ObrasSociales.ToList() : _contexto.ObrasSociales.AsNoTracking().ToList();
                    return entidades.Select(o => new FilaAuditoria(o.Id, (string?)dvs.GetValueOrDefault(o.Id), o)).ToList();
                }),
                ("Diagnosticos", track =>
                {
                    var dvs = _contexto.Diagnosticos.AsNoTracking().Select(d => new { d.Id, D = EF.Property<string>(d, "DVH") }).ToDictionary(x => x.Id, x => x.D);
                    List<Diagnostico> entidades = track ? _contexto.Diagnosticos.ToList() : _contexto.Diagnosticos.AsNoTracking().ToList();
                    return entidades.Select(d => new FilaAuditoria(d.Id, (string?)dvs.GetValueOrDefault(d.Id), d)).ToList();
                }),
                ("HistoriasClinicas", track =>
                {
                    var dvs = _contexto.HistoriasClinicas.AsNoTracking().Select(h => new { h.Id, D = EF.Property<string>(h, "DVH") }).ToDictionary(x => x.Id, x => x.D);
                    List<HistoriaClinica> entidades = track ? _contexto.HistoriasClinicas.ToList() : _contexto.HistoriasClinicas.AsNoTracking().ToList();
                    return entidades.Select(h => new FilaAuditoria(h.Id, (string?)dvs.GetValueOrDefault(h.Id), h)).ToList();
                }),
                ("EventosAdversos", track =>
                {
                    var dvs = _contexto.EventosAdversos.AsNoTracking().Select(e => new { e.Id, D = EF.Property<string>(e, "DVH") }).ToDictionary(x => x.Id, x => x.D);
                    List<EventoAdverso> entidades = track ? _contexto.EventosAdversos.ToList() : _contexto.EventosAdversos.AsNoTracking().ToList();
                    return entidades.Select(e => new FilaAuditoria(e.Id, (string?)dvs.GetValueOrDefault(e.Id), e)).ToList();
                }),
                ("MedicionesRIN", track =>
                {
                    var dvs = _contexto.MedicionesRIN.AsNoTracking().Select(m => new { m.Id, D = EF.Property<string>(m, "DVH") }).ToDictionary(x => x.Id, x => x.D);
                    List<MedicionRIN> entidades = track ? _contexto.MedicionesRIN.ToList() : _contexto.MedicionesRIN.AsNoTracking().ToList();
                    return entidades.Select(m => new FilaAuditoria(m.Id, (string?)dvs.GetValueOrDefault(m.Id), m)).ToList();
                }),
                ("Alertas", track =>
                {
                    var dvs = _contexto.Alertas.AsNoTracking().Select(a => new { a.Id, D = EF.Property<string>(a, "DVH") }).ToDictionary(x => x.Id, x => x.D);
                    List<Alerta> entidades = track ? _contexto.Alertas.ToList() : _contexto.Alertas.AsNoTracking().ToList();
                    return entidades.Select(a => new FilaAuditoria(a.Id, (string?)dvs.GetValueOrDefault(a.Id), a)).ToList();
                }),
                ("Turnos", track =>
                {
                    var dvs = _contexto.Turnos.AsNoTracking().Select(t => new { t.Id, D = EF.Property<string>(t, "DVH") }).ToDictionary(x => x.Id, x => x.D);
                    List<Turno> entidades = track ? _contexto.Turnos.ToList() : _contexto.Turnos.AsNoTracking().ToList();
                    return entidades.Select(t => new FilaAuditoria(t.Id, (string?)dvs.GetValueOrDefault(t.Id), t)).ToList();
                }),
                ("Seguimientos", track =>
                {
                    var dvs = _contexto.Seguimientos.AsNoTracking().Select(s => new { s.Id, D = EF.Property<string>(s, "DVH") }).ToDictionary(x => x.Id, x => x.D);
                    List<Seguimiento> entidades = track ? _contexto.Seguimientos.ToList() : _contexto.Seguimientos.AsNoTracking().ToList();
                    return entidades.Select(s => new FilaAuditoria(s.Id, (string?)dvs.GetValueOrDefault(s.Id), s)).ToList();
                }),
                ("Usuarios", track =>
                {
                    var dvs = _contexto.Usuarios.AsNoTracking().Select(u => new { u.Id, D = EF.Property<string>(u, "DVH") }).ToDictionary(x => x.Id, x => x.D);
                    List<Usuario> entidades = track ? _contexto.Usuarios.ToList() : _contexto.Usuarios.AsNoTracking().ToList();
                    return entidades.Select(u => new FilaAuditoria(u.Id, (string?)dvs.GetValueOrDefault(u.Id), u)).ToList();
                }),
                ("ReportesEstadisticos", track =>
                {
                    var dvs = _contexto.ReportesEstadisticos.AsNoTracking().Select(r => new { r.Id, D = EF.Property<string>(r, "DVH") }).ToDictionary(x => x.Id, x => x.D);
                    List<ReporteEstadistico> entidades = track ? _contexto.ReportesEstadisticos.ToList() : _contexto.ReportesEstadisticos.AsNoTracking().ToList();
                    return entidades.Select(r => new FilaAuditoria(r.Id, (string?)dvs.GetValueOrDefault(r.Id), r)).ToList();
                })
            };
        }

        /// <summary>
        /// Audita la integridad de todas las tablas cubiertas: filas sin DVH, DVH que no
        /// corresponden a los datos, y DVV ausente o desincronizado.
        /// </summary>
        public ResultadoAuditoria Auditar()
        {
            var verificadas = new List<string>();
            var problemas = new List<string>();
            int filasAuditadas = 0;

            foreach ((string tabla, Func<bool, List<FilaAuditoria>> lector) in _tablas)
            {
                List<FilaAuditoria> registros = lector(false);
                filasAuditadas += registros.Count;

                int sinDvh = 0;
                int alteradas = 0;
                var idsEjemplo = new List<int>();
                foreach (FilaAuditoria fila in registros)
                {
                    if (string.IsNullOrEmpty(fila.Dvh))
                    {
                        sinDvh++;
                        if (idsEjemplo.Count < 3)
                        {
                            idsEjemplo.Add(fila.Id);
                        }
                        continue;
                    }
                    if (!string.Equals(fila.Dvh, DigitoVerificador.CalcularDVH(fila.Entidad), StringComparison.Ordinal))
                    {
                        alteradas++;
                        if (idsEjemplo.Count < 3)
                        {
                            idsEjemplo.Add(fila.Id);
                        }
                    }
                }

                if (sinDvh > 0)
                {
                    problemas.Add($"Tabla '{tabla}': {sinDvh} fila(s) sin DVH (recalcular para firmarlas).");
                }
                if (alteradas > 0)
                {
                    problemas.Add($"Tabla '{tabla}': {alteradas} fila(s) con DVH inválido — datos modificados fuera de la aplicación (Id: {string.Join(", ", idsEjemplo)}).");
                }

                string? registrado = _contexto.DigitosVerificadores.AsNoTracking()
                    .FirstOrDefault(d => d.NombreTabla == tabla)?.DVV;
                string esperado = DigitoVerificador.CalcularDVV(tabla, registros.Select(r => (r.Id, r.Dvh)));
                if (registrado == null)
                {
                    problemas.Add($"Tabla '{tabla}': sin DVV registrado (recalcular para firmarla).");
                }
                else if (!string.Equals(registrado, esperado, StringComparison.Ordinal))
                {
                    problemas.Add($"Tabla '{tabla}': el DVV no coincide con el conjunto de sus filas — tabla alterada.");
                }
                else if (sinDvh == 0 && alteradas == 0)
                {
                    verificadas.Add(tabla);
                }
            }

            return new ResultadoAuditoria(verificadas, problemas, filasAuditadas);
        }

        /// <summary>
        /// Recalcula y firma TODAS las filas y tablas cubiertas (reparación auditada tras una
        /// detección). Además de las firmas por fila (DVH), recalcula explícitamente el DVV de
        /// cada tabla —también las que no tienen filas modificadas o están vacías—, porque el
        /// guardado por sí solo no alcanza a cubrir esos casos.
        /// </summary>
        public int RecalcularTodo()
        {
            int firmadas = 0;
            foreach ((_, Func<bool, List<FilaAuditoria>> lector) in _tablas)
            {
                List<FilaAuditoria> registros = lector(true);
                foreach (FilaAuditoria fila in registros)
                {
                    _contexto.Entry(fila.Entidad).Property("DVH").CurrentValue = DigitoVerificador.CalcularDVH(fila.Entidad);
                    firmadas++;
                }
            }
            _contexto.SaveChanges();

            // Los DVV se recalculan aquí de forma explícita: leer las filas (ya firmadas) tal
            // como las lee la auditoría garantiza que "esperado" y "registrado" coincidan.
            foreach ((string tabla, Func<bool, List<FilaAuditoria>> lector) in _tablas)
            {
                List<(int Id, string? Dvh)> filas = lector(false).Select(r => (r.Id, r.Dvh)).ToList();
                string dvv = DigitoVerificador.CalcularDVV(tabla, filas);

                DigitosVerificadores? registro = _contexto.DigitosVerificadores.FirstOrDefault(d => d.NombreTabla == tabla);
                if (registro == null)
                {
                    _contexto.DigitosVerificadores.Add(new DigitosVerificadores
                    {
                        NombreTabla = tabla,
                        DVV = dvv,
                        FechaCalculo = DateTime.Now
                    });
                }
                else if (!string.Equals(registro.DVV, dvv, StringComparison.Ordinal))
                {
                    registro.DVV = dvv;
                    registro.FechaCalculo = DateTime.Now;
                }
            }
            _contexto.SaveChanges();
            return firmadas;
        }
    }
}
