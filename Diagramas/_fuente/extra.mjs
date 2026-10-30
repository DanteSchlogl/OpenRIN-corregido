import path from 'path';
import { Mx, save, esc, dot, cls, seq } from './lib.mjs';

const P = (id, label, actor) => ({ id, label, actor });

// ---------- Diagrama de actividad con carriles ----------
function act(spec, relName) {
  const { title, lanes, nodes, edges } = spec;
  const mx = new Mx();
  const LW = 300, X0 = 20, TOP = 52, STEP = 96;
  const rows = Math.max(...nodes.map((n) => n.row)) + 1;
  const H = TOP + 40 + rows * STEP + 20;
  mx.vertex(esc(title), 'text;html=1;strokeColor=none;fillColor=none;fontFamily=Arial;fontSize=16;fontStyle=1;fontColor=#1F3864;align=center;whiteSpace=nowrap;', X0, 6, lanes.length * LW, 28);
  lanes.forEach((l, i) => {
    mx.vertex('', 'rounded=0;html=1;fillColor=none;strokeColor=#9AA7BF;dashed=1;', X0 + i * LW, TOP + 28, LW, H - TOP - 28);
    mx.vertex(esc(l), 'rounded=0;html=1;fillColor=#DCE6F5;strokeColor=#9AA7BF;fontFamily=Arial;fontSize=12;fontStyle=1;', X0 + i * LW, TOP, LW, 28);
  });
  const ids = {};
  const colors = ['#E8F0FE|#4A78C2', '#E6F2E8|#4F8F5A', '#F1EBFA|#7D5BB0'];
  for (const n of nodes) {
    const cx = X0 + n.lane * LW + LW / 2, cy = TOP + 28 + 50 + n.row * STEP;
    const [bg, bd] = (n.color || colors[n.lane % 3]).split('|');
    if (n.type === 'start') ids[n.id] = mx.vertex('', 'ellipse;html=1;fillColor=#222222;strokeColor=#222222;', cx - 12, cy - 12, 24, 24);
    else if (n.type === 'end') ids[n.id] = mx.vertex('', 'ellipse;html=1;shape=endState;fillColor=#222222;strokeColor=#222222;', cx - 15, cy - 15, 30, 30);
    else if (n.type === 'merge') ids[n.id] = mx.vertex('', 'rhombus;html=1;fillColor=#FFFFFF;strokeColor=#555555;', cx - 14, cy - 14, 28, 28);
    else if (n.type === 'decision') ids[n.id] = mx.vertex(esc(n.text), 'rhombus;whiteSpace=wrap;html=1;fillColor=#FFF6E5;strokeColor=#C9922E;fontFamily=Arial;fontSize=11;', cx - 95, cy - 38, 190, 76);
    else ids[n.id] = mx.vertex(esc(n.text), `rounded=1;whiteSpace=wrap;html=1;fillColor=${n.fill || bg};strokeColor=${n.stroke || bd};fontFamily=Arial;fontSize=11;arcSize=20;`, cx - 105, cy - 28, 210, 56);
  }
  for (const e of edges) {
    mx.edge(esc(e.label || ''), 'edgeStyle=orthogonalEdgeStyle;rounded=0;html=1;endArrow=block;endFill=1;strokeColor=#333333;fontFamily=Arial;fontSize=11;labelBackgroundColor=#FFFFFF;', { source: ids[e.from], target: ids[e.to] });
  }
  mx.rotulo(Math.max(20, lanes.length * LW - 660), H + 6, path.basename(relName), 680);
  save(relName, mx, title);
}

const A = 'Actividad';
const N = (id, lane, row, text, o = {}) => ({ id, lane, row, text, ...o });
const S = (id, lane, row) => ({ id, lane, row, type: 'start' });
const Z = (id, lane, row) => ({ id, lane, row, type: 'end' });
const M = (id, lane, row) => ({ id, lane, row, type: 'merge' });
const Dn = (id, lane, row, text) => ({ id, lane, row, text, type: 'decision' });
const R = { fill: '#FDE8E8', stroke: '#C94C4C' };

// P01
act({ title: 'P01 — Realizar Triaje y Evaluación de Riesgo Hematológico: diagrama de actividad', lanes: ['Sistema', 'Administrativo', 'Médico hematólogo'], nodes: [
  S('s', 0, 0), N('a', 0, 1, 'Recibir la medición de RIN (reportada o recibida)'), N('b', 0, 2, 'Clasificar la criticidad según rango y tendencia'),
  Dn('d', 0, 3, '¿Criticidad Alta o tendencia peligrosa?'),
  N('c', 0, 4, 'Generar la alerta y notificar al equipo', R), N('e', 1, 4, 'Registrar el control como estable', { color: '#E6F2E8|#4F8F5A' }),
  M('m', 0, 5), N('f', 0, 6, 'Priorizar y publicar el panel de triaje'), N('g', 1, 7, 'Revisar el panel priorizado'), N('h', 2, 8, 'Atender primero los casos críticos'), Z('z', 2, 9),
], edges: [
  { from: 's', to: 'a' }, { from: 'a', to: 'b' }, { from: 'b', to: 'd' }, { from: 'd', to: 'c', label: 'sí' }, { from: 'd', to: 'e', label: 'no' },
  { from: 'c', to: 'm' }, { from: 'e', to: 'm' }, { from: 'm', to: 'f' }, { from: 'f', to: 'g' }, { from: 'g', to: 'h' }, { from: 'h', to: 'z' },
] }, `Procesos/P01 - ${A}`);

// P02
act({ title: 'P02 — Configurar Historia Clínica y Parámetros del Paciente: diagrama de actividad', lanes: ['Administrativo', 'Sistema', 'Médico hematólogo'], nodes: [
  S('s', 0, 0), N('a', 0, 1, 'Registrar al paciente (DNI, contacto, obra social)'), N('b', 1, 2, 'Validar la unicidad del DNI'), Dn('d', 1, 3, '¿DNI de otro paciente activo?'),
  N('c', 0, 4, 'Rechazar con el detalle del conflicto', R), N('e', 1, 4, 'Persistir al paciente y generar las credenciales del portal'),
  N('f', 2, 5, 'Configurar el tratamiento: medicación, dosis, límites de RIN y periodicidad'), N('g', 1, 6, 'Calcular y guardar el próximo control según la periodicidad'),
  M('m', 1, 7), Z('z', 1, 8),
], edges: [
  { from: 's', to: 'a' }, { from: 'a', to: 'b' }, { from: 'b', to: 'd' }, { from: 'd', to: 'c', label: 'sí' }, { from: 'd', to: 'e', label: 'no' },
  { from: 'e', to: 'f' }, { from: 'f', to: 'g' }, { from: 'g', to: 'm' }, { from: 'c', to: 'm' }, { from: 'm', to: 'z' },
] }, `Procesos/P02 - ${A}`);

// P03
act({ title: 'P03 — Optimizar Agenda Médica: diagrama de actividad', lanes: ['Administrativo', 'Sistema', 'Médico hematólogo'], nodes: [
  S('s', 0, 0), N('a', 0, 1, 'Consultar la agenda del período'), N('b', 1, 2, 'Cruzar turnos liberados con pacientes críticos sin turno próximo'),
  Dn('d1', 1, 3, '¿Hay sugerencias?'), N('c', 1, 4, 'Mostrar las sugerencias al administrativo'), Dn('d2', 0, 5, '¿Acepta la sugerencia?'),
  N('e', 1, 6, 'Reasignar el turno, persistir y notificar'), N('f', 0, 6, 'Registrar el motivo del rechazo', R), Dn('d3', 0, 7, '¿Escalar al médico?'),
  N('g', 2, 8, 'Atender el caso escalado'), M('m', 0, 9), Z('z', 0, 10),
], edges: [
  { from: 's', to: 'a' }, { from: 'a', to: 'b' }, { from: 'b', to: 'd1' }, { from: 'd1', to: 'c', label: 'sí' }, { from: 'd1', to: 'm', label: 'no' },
  { from: 'c', to: 'd2' }, { from: 'd2', to: 'e', label: 'sí' }, { from: 'd2', to: 'f', label: 'no' }, { from: 'f', to: 'd3' },
  { from: 'd3', to: 'g', label: 'sí' }, { from: 'd3', to: 'm', label: 'no' }, { from: 'e', to: 'm' }, { from: 'g', to: 'm' }, { from: 'm', to: 'z' },
] }, `Procesos/P03 - ${A}`);

// P04
act({ title: 'P04 — Realizar Seguimiento Clínico y Atención Preventiva: diagrama de actividad', lanes: ['Sistema', 'Administrativo', 'Médico hematólogo'], nodes: [
  S('s', 0, 0), N('a', 0, 1, 'Detectar controles vencidos y alertas activas'), N('b', 0, 2, 'Armar la lista de contactos priorizada'),
  N('c', 1, 3, 'Registrar el intento de contacto y su resultado'), Dn('d1', 1, 4, '¿Contacto exitoso?'),
  N('e', 0, 5, 'Cerrar la alerta asociada'), Dn('d2', 1, 5, '¿Requiere decisión médica?'),
  N('f', 2, 6, 'Registrar la decisión clínica (adelantar el control o derivar)'), N('g', 1, 6, 'Reintentar en el próximo ciclo (al 3.º intento fallido se escala al médico)'),
  M('m', 1, 7), Z('z', 1, 8),
], edges: [
  { from: 's', to: 'a' }, { from: 'a', to: 'b' }, { from: 'b', to: 'c' }, { from: 'c', to: 'd1' }, { from: 'd1', to: 'e', label: 'sí' }, { from: 'd1', to: 'd2', label: 'no' },
  { from: 'd2', to: 'f', label: 'sí' }, { from: 'd2', to: 'g', label: 'no' }, { from: 'e', to: 'm' }, { from: 'f', to: 'm' }, { from: 'g', to: 'm' }, { from: 'm', to: 'z' },
] }, `Procesos/P04 - ${A}`);

// P05
act({ title: 'P05 — Generar Reportes Estadísticos de Eficacia: diagrama de actividad', lanes: ['Usuario (médico / administrador)', 'Sistema'], nodes: [
  S('s', 0, 0), N('a', 0, 1, 'Elegir el período y solicitar el reporte'), N('b', 1, 2, 'Consolidar las mediciones del período'),
  N('c', 1, 3, 'Calcular el indicador global y las cantidades (óptimos / subóptimos / deficientes)'), N('d', 1, 4, 'Guardar el reporte (firma DVH/DVV + auditoría)'),
  Dn('e', 0, 5, '¿Exportar el reporte?'), N('f', 1, 6, 'Generar el archivo (PDF / Excel / JSON) y auditar'), M('m', 0, 7), Z('z', 0, 8),
], edges: [
  { from: 's', to: 'a' }, { from: 'a', to: 'b' }, { from: 'b', to: 'c' }, { from: 'c', to: 'd' }, { from: 'd', to: 'e' },
  { from: 'e', to: 'f', label: 'sí' }, { from: 'e', to: 'm', label: 'no' }, { from: 'f', to: 'm' }, { from: 'm', to: 'z' },
] }, `Procesos/P05 - ${A}`);

// ---------- T02: secuencias ----------
seq({ title: 'T02 — Verificación de integridad al iniciar la aplicación (antes del login)', parts: [P('u', 'Usuario', true), P('p', ':Program\n(static)'), P('v', ':VerificacionArranque\n(static)'), P('i', ':IntegridadLogic\n(BLL)'), P('c', ':NegocioDbContext\n(DAL)')], steps: [
  { from: 'u', to: 'p', text: '1: iniciar la aplicación' },
  { from: 'p', to: 'v', text: '2: Auditar()  (primer acto del Main, antes de dibujar ventanas)' },
  { from: 'v', to: 'i', text: '3: Auditar()  (11 tablas: DVH + DVV)' },
  { from: 'i', to: 'c', text: '4: recalcular firmas y compararlas con las almacenadas' },
  { from: 'c', to: 'i', text: '5: resultado por tabla', ret: true },
  { from: 'i', to: 'v', text: '6: ResultadoAuditoria', ret: true },
  { from: 'v', to: 'p', text: '7: ResultadoAuditoria?', ret: true },
  { alt: [
    { guard: 'sin problemas', steps: [{ from: 'p', to: 'u', text: '8: mostrar LoginForm', ret: true }] },
    { guard: 'integridad vulnerada', steps: [{ from: 'p', to: 'u', text: '9: mostrar IntegridadVulneradaForm (acceso bloqueado hasta la reparación)', ret: true }] },
  ] },
] }, 'Tecnicos/T02 - Secuencia integridad al iniciar');

seq({ title: 'T02 — Inicio de sesión con la sesión única (Singleton) y el gating de permisos', parts: [P('u', 'Usuario', true), P('f', ':LoginForm\n(UI)'), P('s', ':SeguridadService\n(Facade)'), P('k', ':SesionActual\n(singleton)'), P('d', ':OpenRIN_Services\n(DB)')], steps: [
  { from: 'u', to: 'f', text: '1: ingresar usuario y contraseña' },
  { from: 'f', to: 's', text: '2: Autenticar(usuario, contraseña)' },
  { from: 's', to: 'd', text: '3: consultar el usuario (ADO.NET)' },
  { from: 'd', to: 's', text: '4: hash PBKDF2, estado, intentos y bloqueo', ret: true },
  { from: 's', to: 's', text: '5: VerificarHash (PBKDF2-SHA256, tiempo constante)' },
  { alt: [
    { guard: 'credenciales inválidas o cuenta bloqueada', steps: [{ from: 's', to: 'f', text: '6: rechazo + contador de intentos (5 intentos: bloqueo 15 min)', ret: true }, { from: 'f', to: 'u', text: '7: mostrar el error', ret: true }] },
    { guard: 'credenciales válidas', steps: [
      { from: 's', to: 'k', text: '8: IniciarUsuarioAutenticado(usuario)' },
      { from: 'k', to: 'k', text: '9: instancia única protegida por lock' },
      { from: 'k', to: 's', text: '10: sesión iniciada', ret: true },
      { from: 's', to: 'f', text: '11: UsuarioAutenticado (perfil y permisos)', ret: true },
      { from: 'f', to: 'u', text: '12: abrir PrincipalForm (gating por permisos) y registrar en bitácora', ret: true },
    ] },
  ] },
] }, 'Tecnicos/T02 - Secuencia inicio de sesion');

// ---------- Capa de acceso a datos ----------
const E = 'style=dashed arrowhead=open';
await dot(`rankdir=LR; nodesep=0.45; ranksep=1.0;
${cls('ig', 'infra', 'interface', 'IRepositorioGenerico<T>', ['+ ObtenerPorId(id) : T?', '+ ObtenerTodos() : List<T>', '+ Agregar(entidad) : void', '+ Modificar(entidad) : void', '+ Eliminar(entidad) : void', '+ Existe(id) : bool'])}
${cls('rg', 'ui', '', 'RepositorioGenerico<T>', ['# contexto : NegocioDbContext', '+ ObtenerPorId(id) / ObtenerTodos()', '+ Agregar / Modificar / Eliminar / Existe'])}
${cls('ip', 'infra', 'interface', 'IPacienteRepositorio', ['+ BuscarActivos() : List<Paciente>', '+ BuscarPorDNI(dni) : Paciente?', '+ ExisteDNIActivo(dni, idExcluir) : bool'])}
${cls('pr', 'ui', 'DAL', 'PacienteRepositorio', ['+ BuscarActivos() / BuscarPorDNI(dni)', '+ ExisteDNIActivo(dni, idExcluir)'])}
${cls('ctx', 'infra', 'EF Core', 'NegocioDbContext', ['- Pacientes, ObrasSociales, Usuarios, Diagnosticos', '- HistoriasClinicas, MedicionesRIN, Alertas, Turnos', '- Seguimientos, EventosAdversos, ReportesEstadisticos', '- AuditoriaCambios, DigitosVerificadores (13 DbSet)', '# OnModelCreating(modelBuilder) : void', '+ SaveChanges() : int  (DVH/DVV en 3 fases + auditoría)'])}
${cls('au', 'infra', 'internal helper', 'AuditoriaDeCambios', ['+ Serializar(valores) : string?', '+ Aplicar(entrada, json) : void'])}
rg -> ig [arrowhead=empty style=dashed label="implementa"];
pr -> rg [arrowhead=empty label="hereda de RepositorioGenerico<Paciente>"];
pr -> ip [arrowhead=empty style=dashed label="implementa"];
rg -> ctx [${E} label="usa"];
ctx -> au [${E} label="captura cambios"];`, 'Arquitectura/G06 - Acceso a datos (clases)');
console.log('extra ok');
