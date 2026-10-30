import path from 'path';
import { Mx, save, esc } from './lib.mjs';

// ---------- Diagrama de actividad con carriles (ruteo explícito, sin cruces) ----------
const LW = 300, X0 = 20, TOP = 52, STEP = 96;
const cxOf = (lane) => X0 + lane * LW + LW / 2;
const cyOf = (row) => TOP + 28 + 50 + row * STEP;
const tok = (t) => (t[0] === 'c' ? cxOf(+t.slice(1)) : t[0] === 'r' ? cyOf(+t.slice(1)) : X0 + (+t.slice(1) + 1) * LW - 14);
const PORT = { t: 'X=0.5;Y=0', b: 'X=0.5;Y=1', l: 'X=0;Y=0.5', r: 'X=1;Y=0.5' };
const port = (kind, p) => { const [x, y] = PORT[p].split(';'); return `${kind}${x};${kind}${y};`; };

function act(spec, relName) {
  const { title, lanes, nodes, edges } = spec;
  const mx = new Mx();
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
    const cx = cxOf(n.lane), cy = cyOf(n.row);
    const [bg, bd] = (n.color || colors[n.lane % 3]).split('|');
    if (n.type === 'start') ids[n.id] = mx.vertex('', 'ellipse;html=1;fillColor=#222222;strokeColor=#222222;', cx - 12, cy - 12, 24, 24);
    else if (n.type === 'end') ids[n.id] = mx.vertex('', 'ellipse;html=1;shape=endState;fillColor=#222222;strokeColor=#222222;', cx - 15, cy - 15, 30, 30);
    else if (n.type === 'merge') ids[n.id] = mx.vertex('', 'rhombus;html=1;fillColor=#FFFFFF;strokeColor=#555555;', cx - 14, cy - 14, 28, 28);
    else if (n.type === 'decision') ids[n.id] = mx.vertex(esc(n.text), 'rhombus;whiteSpace=wrap;html=1;fillColor=#FFF6E5;strokeColor=#C9922E;fontFamily=Arial;fontSize=11;', cx - 95, cy - 38, 190, 76);
    else ids[n.id] = mx.vertex(esc(n.text), `rounded=1;whiteSpace=wrap;html=1;fillColor=${n.fill || bg};strokeColor=${n.stroke || bd};fontFamily=Arial;fontSize=11;arcSize=20;`, cx - 105, cy - 28, 210, 56);
  }
  for (const e of edges) {
    const st = 'edgeStyle=orthogonalEdgeStyle;rounded=0;html=1;endArrow=block;endFill=1;strokeColor=#333333;fontFamily=Arial;fontSize=11;labelBackgroundColor=#FFFFFF;' + port('exit', e.ex || 'b') + port('entry', e.en || 't');
    const pts = (e.via || []).map(([x, y]) => [tok(x), tok(y)]);
    mx.edge(esc(e.label || ''), st, { source: ids[e.from], target: ids[e.to], points: pts });
  }
  mx.rotulo(Math.max(20, lanes.length * LW - 660), H + 6, path.basename(relName), 680);
  save(relName, mx, title);
}

const A = 'Actividad';
const N = (id, lane, row, text, o = {}) => ({ id, lane, row, text, ...o });
const S = (id, lane, row) => ({ id, lane, row, type: 'start' });
const Z = (id, lane, row) => ({ id, lane, row, type: 'end' });
const M = (id, lane, row) => ({ id, lane, row, type: 'merge' });
const D = (id, lane, row, text) => ({ id, lane, row, text, type: 'decision' });
const R = { fill: '#FDE8E8', stroke: '#C94C4C' };
const G = { color: '#E6F2E8|#4F8F5A' };
const E = (from, to, label, ex, en, via) => ({ from, to, label, ex, en, via });

// P01
act({ title: 'P01 — Realizar Triaje y Evaluación de Riesgo Hematológico: diagrama de actividad', lanes: ['Sistema', 'Administrativo', 'Médico hematólogo'], nodes: [
  S('s', 0, 0), N('a', 0, 1, 'Recibir la medición de RIN (reportada o recibida)'), N('b', 0, 2, 'Clasificar la criticidad según rango y tendencia'),
  D('d', 0, 3, '¿Criticidad Alta o tendencia peligrosa?'),
  N('c', 0, 4, 'Generar la alerta y notificar al equipo', R), N('e', 1, 4, 'Registrar el control como estable', G),
  M('m', 0, 5), N('f', 0, 6, 'Priorizar y publicar el panel de triaje'), N('g', 1, 7, 'Revisar el panel priorizado'), N('h', 2, 8, 'Atender primero los casos críticos'), Z('z', 2, 9),
], edges: [
  E('s', 'a'), E('a', 'b'), E('b', 'd'), E('d', 'c', 'sí'), E('d', 'e', 'no', 'r', 't'),
  E('c', 'm'), E('e', 'm', '', 'b', 'r', [['c1', 'r5']]), E('m', 'f'), E('f', 'g', '', 'r', 't'), E('g', 'h', '', 'r', 't'), E('h', 'z'),
] }, `Procesos/P01 - ${A}`);

// P02
act({ title: 'P02 — Configurar Historia Clínica y Parámetros del Paciente: diagrama de actividad', lanes: ['Administrativo', 'Sistema', 'Médico hematólogo'], nodes: [
  S('s', 0, 0), N('a', 0, 1, 'Registrar al paciente (DNI, contacto, obra social)'), N('b', 1, 2, 'Validar la unicidad del DNI'), D('d', 1, 3, '¿DNI de otro paciente activo?'),
  N('c', 0, 4, 'Rechazar con el detalle del conflicto', R), N('e', 1, 4, 'Persistir al paciente y generar las credenciales del portal'),
  N('f', 2, 5, 'Configurar el tratamiento: medicación, dosis, límites de RIN y periodicidad'), N('g', 1, 6, 'Calcular y guardar el próximo control según la periodicidad'),
  M('m', 1, 7), Z('z', 1, 8),
], edges: [
  E('s', 'a'), E('a', 'b', '', 'r', 't'), E('b', 'd'), E('d', 'c', 'sí', 'l', 't'), E('d', 'e', 'no'),
  E('e', 'f', '', 'r', 't'), E('f', 'g', '', 'b', 'r', [['c2', 'r6']]), E('g', 'm'), E('c', 'm', '', 'b', 'l', [['c0', 'r7']]), E('m', 'z'),
] }, `Procesos/P02 - ${A}`);

// P03
act({ title: 'P03 — Optimizar Agenda Médica: diagrama de actividad', lanes: ['Administrativo', 'Sistema', 'Médico hematólogo'], nodes: [
  S('s', 0, 0), N('a', 0, 1, 'Consultar la agenda del período'), N('b', 1, 2, 'Cruzar turnos liberados con pacientes críticos sin turno próximo'),
  D('d1', 1, 3, '¿Hay sugerencias?'), N('c', 1, 4, 'Mostrar las sugerencias al administrativo'), D('d2', 0, 5, '¿Acepta la sugerencia?'),
  N('e', 1, 6, 'Reasignar el turno, persistir y notificar'), N('f', 0, 6, 'Registrar el motivo del rechazo', R), D('d3', 0, 7, '¿Escalar al médico?'),
  N('g', 2, 8, 'Atender el caso escalado'), M('m', 1, 9), Z('z', 1, 10),
], edges: [
  E('s', 'a'), E('a', 'b', '', 'r', 't'), E('b', 'd1'), E('d1', 'c', 'sí'), E('d1', 'm', 'no', 'r', 'r', [['m1', 'r3'], ['m1', 'r9']]),
  E('c', 'd2', '', 'l', 't'), E('d2', 'e', 'sí', 'r', 't'), E('d2', 'f', 'no'), E('f', 'd3'),
  E('d3', 'g', 'sí', 'r', 't'), E('d3', 'm', 'no', 'b', 'l', [['c0', 'r9']]), E('e', 'm'), E('g', 'm', '', 'b', 'r', [['c2', 'r9']]), E('m', 'z'),
] }, `Procesos/P03 - ${A}`);

// P04
act({ title: 'P04 — Realizar Seguimiento Clínico y Atención Preventiva: diagrama de actividad', lanes: ['Sistema', 'Administrativo', 'Médico hematólogo'], nodes: [
  S('s', 0, 0), N('a', 0, 1, 'Detectar controles vencidos y alertas activas'), N('b', 0, 2, 'Armar la lista de contactos priorizada'),
  N('c', 1, 3, 'Registrar el intento de contacto y su resultado'), D('d1', 1, 4, '¿Contacto exitoso?'),
  N('e', 0, 5, 'Cerrar la alerta asociada'), D('d2', 1, 5, '¿Requiere decisión médica?'),
  N('f', 2, 6, 'Registrar la decisión clínica (adelantar el control o derivar)'), N('g', 1, 6, 'Reintentar en el próximo ciclo (al 3.º intento fallido se escala al médico)'),
  M('m', 1, 7), Z('z', 1, 8),
], edges: [
  E('s', 'a'), E('a', 'b'), E('b', 'c', '', 'r', 't'), E('c', 'd1'), E('d1', 'e', 'sí', 'l', 't'), E('d1', 'd2', 'no'),
  E('d2', 'f', 'sí', 'r', 't'), E('d2', 'g', 'no'), E('g', 'm'), E('e', 'm', '', 'b', 'l', [['c0', 'r7']]), E('f', 'm', '', 'b', 'r', [['c2', 'r7']]), E('m', 'z'),
] }, `Procesos/P04 - ${A}`);

// P05
act({ title: 'P05 — Generar Reportes Estadísticos de Eficacia: diagrama de actividad', lanes: ['Usuario (médico / administrador)', 'Sistema'], nodes: [
  S('s', 0, 0), N('a', 0, 1, 'Elegir el período y solicitar el reporte'), N('b', 1, 2, 'Consolidar las mediciones del período'),
  N('c', 1, 3, 'Calcular el indicador global y las cantidades (óptimos / subóptimos / deficientes)'), N('d', 1, 4, 'Guardar el reporte (firma DVH/DVV + auditoría)'),
  D('e', 0, 5, '¿Exportar el reporte?'), N('f', 1, 6, 'Generar el archivo (PDF / Excel / JSON) y auditar'), M('m', 0, 7), Z('z', 0, 8),
], edges: [
  E('s', 'a'), E('a', 'b', '', 'r', 't'), E('b', 'c'), E('c', 'd'), E('d', 'e', '', 'l', 't'),
  E('e', 'f', 'sí', 'r', 't'), E('e', 'm', 'no'), E('f', 'm', '', 'b', 'r', [['c1', 'r7']]), E('m', 'z'),
] }, `Procesos/P05 - ${A}`);
console.log('proc ok');
