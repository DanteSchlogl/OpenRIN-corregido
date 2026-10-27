import { ucg, seq, dot, cls } from './lib.mjs';

const P = (id, label, actor) => ({ id, label, actor });
const CTX = P('c', ':NegocioDbContext\n(DAL)');
const ctxSave = (from, text = 'SaveChanges()') => [
  { from, to: 'c', text },
  { from: 'c', to: 'c', text: 'firmar DVH/DVV + auditar cambio' },
  { from: 'c', to: from, text: 'ok', ret: true },
];
const E = (txt) => ({ from: 'l', to: 'f', text: txt, ret: true });

// Estilo común de las clases afectadas
const cu = async (n, name, body, extra = '') =>
  dot(`rankdir=LR; nodesep=0.5; ranksep=0.9; ${body}`, `UC-${n}/UC-${n} - Clases afectadas`, extra);
const file = (n, kind) => `UC-${n}/UC-${n} - ${kind}`;

const ent = (id, name, m) => cls(id, 'ent', 'entidad', name, m);
const ctxNode = cls('ctx', 'infra', 'EF Core', 'NegocioDbContext', ['+ SaveChanges() : int', '  (firma DVH/DVV y audita)']);
const ctxRead = cls('ctx', 'infra', 'EF Core', 'NegocioDbContext', ['(consultas de lectura)']);
const rejNode = cls('rej', 'infra', 'static', 'ReglasNegocio', ['+ Rechazar(errores, contexto, usuario)']);

// ============ UC-01 ============
ucg({ title: 'UC-01 — Registrar, Modificar y Dar de Baja Paciente: gráfico', left: ['Administrativo'], right: ['Médico'], main: 'UC-01 Registrar, Modificar y Dar de Baja Paciente', rel: [{ text: 'Generar credenciales del portal', rel: 'extend', cond: '(en el alta)' }] }, file('01', 'Gráfico'));
await cu('01', '', [
  cls('form', 'ui', 'Form', 'PacienteEditorForm', ['(alta / edición / baja)']),
  cls('logic', 'logic', 'BLL', 'PacienteLogic', ['+ Buscar(filtro, incluirInactivos) : List<Paciente>', '+ BuscarPorDNI(dni) : Paciente?', '+ RegistrarPaciente(paciente, usuario) : CredencialesPortal', '+ ModificarPaciente(paciente, usuario) : void', '+ DarDeBajaPaciente(id, motivo, usuario) : void']),
  cls('irepo', 'infra', 'interface', 'IPacienteRepositorio', ['+ BuscarActivos() : List<Paciente>', '+ BuscarPorDNI(dni) : Paciente?', '+ ExisteDNIActivo(dni, idExcluir) : bool']),
  cls('repo', 'infra', 'DAL', 'PacienteRepositorio', ['(implementa la interfaz)']),
  ctxNode, rejNode,
  ent('pac', 'Paciente', ['- NombreCompleto, DNI «UQ»', '- Estado, MotivoBaja', '- FechaAlta, FechaBaja']),
  ent('os', 'ObraSocial', ['- Nombre, Activo']),
  'form -> logic [label="invoca" style=dashed arrowhead=open]; logic -> irepo [label="valida DNI / busca" style=dashed arrowhead=open]; repo -> irepo [arrowhead=empty style=dashed label="implementa"]; repo -> ctx [style=dashed arrowhead=open label="persiste"]; logic -> rej [style=dashed arrowhead=open label="rechaza"]; logic -> pac [style=dashed arrowhead=open label="crea/modifica/baja"]; pac -> os [arrowhead=none taillabel="*" headlabel="0..1"];',
].join('\n'));
seq({ title: 'UC-01 — Registrar Paciente: diagrama de secuencia (alta con validación de DNI)', parts: [P('a', 'Administrativo', true), P('f', ':PacienteEditorForm\n(UI)'), P('l', ':PacienteLogic\n(BLL)'), P('r', ':PacienteRepositorio\n(DAL)'), CTX], steps: [
  { from: 'a', to: 'f', text: '1: ingresar datos, DNI, obra social y contacto' },
  { from: 'f', to: 'l', text: '2: RegistrarPaciente(paciente, usuario)' },
  { from: 'l', to: 'r', text: '3: ExisteDNIActivo(dni)' },
  { from: 'r', to: 'l', text: '4: bool', ret: true },
  { alt: [
    { guard: 'DNI de otro paciente activo', steps: [E('5: ValidacionNegocioException (detalle)'), { from: 'f', to: 'a', text: '6: mostrar conflicto', ret: true }] },
    { guard: 'DNI nuevo', steps: [
      { from: 'l', to: 'r', text: '7: Agregar(paciente)' },
      { from: 'r', to: 'c', text: '8: SaveChanges()' },
      { from: 'c', to: 'c', text: '9: firmar DVH/DVV + auditar alta' },
      { from: 'c', to: 'r', text: '10: ok', ret: true },
      { from: 'l', to: 'l', text: '11: generar credenciales del portal' },
      E('12: CredencialesPortal(usuario, contraseña temporal)'),
      { from: 'f', to: 'a', text: '13: confirmación + credenciales', ret: true },
    ] },
  ] },
] }, file('01', 'Secuencia'));

// ============ UC-02 ============
ucg({ title: 'UC-02 — Configurar Historia Clínica Hematológica: gráfico', left: ['Médico hematólogo'], main: 'UC-02 Configurar Historia Clínica Hematológica', rel: [{ text: 'Actualizar periodicidad y próximo control', rel: 'extend' }] }, file('02', 'Gráfico'));
await cu('02', '', [
  cls('form', 'ui', 'Form', 'HistoriaClinicaForm', ['(configuración clínica)']),
  cls('logic', 'logic', 'BLL', 'HistoriaClinicaLogic', ['+ Configurar(configuracion, usuario) : HistoriaClinica', '+ ObtenerPorPaciente(idPaciente) : HistoriaClinica?', '+ ActualizarPeriodicidad(idPaciente, dias, usuario) : void']),
  ctxNode, rejNode,
  ent('hc', 'HistoriaClinica', ['- IdPaciente «UQ», IdDiagnostico', '- LimiteInferiorRIN, LimiteSuperiorRIN', '- Medicamento, Dosis', '- PeriodicidadDias, ProximaFechaControl']),
  ent('dx', 'Diagnostico', ['- Nombre, Descripcion, Activo']),
  'form -> logic [label="invoca" style=dashed arrowhead=open]; logic -> ctx [label="persiste" style=dashed arrowhead=open]; logic -> rej [label="rechaza" style=dashed arrowhead=open]; logic -> hc [label="configura" style=dashed arrowhead=open]; hc -> dx [arrowhead=none taillabel="*" headlabel="0..1" label="clasifica"];',
].join('\n'));
seq({ title: 'UC-02 — Configurar Historia Clínica: diagrama de secuencia', parts: [P('a', 'Médico', true), P('f', ':HistoriaClinicaForm\n(UI)'), P('l', ':HistoriaClinicaLogic\n(BLL)'), CTX], steps: [
  { from: 'a', to: 'f', text: '1: ingresar diagnóstico, medicación, dosis, límites y periodicidad' },
  { from: 'f', to: 'l', text: '2: Configurar(configuracion, usuario)' },
  { from: 'l', to: 'c', text: '3: leer paciente y su historia clínica' },
  { from: 'c', to: 'l', text: '4: datos', ret: true },
  { from: 'l', to: 'l', text: '5: validar paciente activo, límites (inf < sup) y próximo control' },
  { alt: [
    { guard: 'datos inválidos', steps: [E('6: ValidacionNegocioException (detalle)'), { from: 'f', to: 'a', text: '7: mostrar errores', ret: true }] },
    { guard: 'datos válidos', steps: [
      { from: 'l', to: 'c', text: '8: Add/Update(historia) + SaveChanges()' },
      { from: 'c', to: 'c', text: '9: firmar DVH/DVV + auditar' },
      { from: 'c', to: 'l', text: '10: ok', ret: true },
      E('11: HistoriaClinica configurada (próximo control)'),
      { from: 'f', to: 'a', text: '12: confirmación + próxima fecha de control', ret: true },
    ] },
  ] },
] }, file('02', 'Secuencia'));

// ============ UC-03 ============
ucg({ title: 'UC-03 — Registrar Eventos Adversos en la Historia Clínica: gráfico', left: ['Médico hematólogo'], main: 'UC-03 Registrar Eventos Adversos en la Historia Clínica', rel: [{ text: 'Destacar evento Moderado o Grave', rel: 'extend', cond: '(gravedad ≥ Moderada)' }] }, file('03', 'Gráfico'));
await cu('03', '', [
  cls('form', 'ui', 'Form', 'EventoAdversoForm', ['(registro de eventos)']),
  cls('ctl', 'ui', 'UserControl', 'EventosAdversosControl', ['(listado; colorea por gravedad)']),
  cls('logic', 'logic', 'BLL', 'EventoAdversoLogic', ['+ Registrar(evento, usuario) : EventoAdverso', '+ ObtenerPorPaciente(idPaciente) : List<EventoAdverso>']),
  ctxNode, rejNode,
  ent('ev', 'EventoAdverso', ['- Fecha, Tipo, Descripcion', '- Gravedad (Leve / Moderada / Grave)', '- AccionTomada']),
  ent('pac', 'Paciente', ['- NombreCompleto, Estado']),
  'form -> logic [label="invoca" style=dashed arrowhead=open]; ctl -> form [label="abre" style=dashed arrowhead=open]; logic -> ctx [label="persiste" style=dashed arrowhead=open]; logic -> rej [label="rechaza" style=dashed arrowhead=open]; logic -> ev [label="crea" style=dashed arrowhead=open]; ev -> pac [arrowhead=none taillabel="*" headlabel="1" label="de"];',
].join('\n'));
seq({ title: 'UC-03 — Registrar Evento Adverso: diagrama de secuencia', parts: [P('a', 'Médico', true), P('f', ':EventoAdversoForm\n(UI)'), P('l', ':EventoAdversoLogic\n(BLL)'), CTX], steps: [
  { from: 'a', to: 'f', text: '1: elegir paciente e ingresar fecha, tipo, gravedad y acción' },
  { from: 'f', to: 'l', text: '2: Registrar(evento, usuario)' },
  { from: 'l', to: 'c', text: '3: leer paciente e historia clínica' },
  { from: 'c', to: 'l', text: '4: datos', ret: true },
  { from: 'l', to: 'l', text: '5: validar paciente activo, historia configurada y fecha no futura' },
  { alt: [
    { guard: 'validación falla', steps: [E('6: ValidacionNegocioException'), { from: 'f', to: 'a', text: '7: mostrar errores', ret: true }] },
    { guard: 'válido', steps: [
      { from: 'l', to: 'c', text: '8: Add(evento) + SaveChanges()' },
      { from: 'c', to: 'c', text: '9: firmar DVH/DVV + auditar' },
      { from: 'c', to: 'l', text: '10: ok', ret: true },
      E('11: EventoAdverso registrado'),
      { alt: [
        { guard: 'gravedad Moderada o Grave', steps: [{ from: 'f', to: 'f', text: '12: destacar con color en la historia' }] },
      ] },
      { from: 'f', to: 'a', text: '13: confirmación del registro', ret: true },
    ] },
  ] },
] }, file('03', 'Secuencia'));

// ============ UC-04 ============
ucg({ title: 'UC-04 — Reportar Valor de RIN: gráfico', left: ['Administrativo'], right: ['Paciente / Familiar autorizado'], main: 'UC-04 Reportar Valor de RIN', rel: [
  { text: 'UC-05 Calcular Criticidad', rel: 'include', fill: '#E8F0FE' },
  { text: 'UC-07 Notificar Alerta Urgente', rel: 'extend', cond: '(criticidad Alta)', fill: '#E8F0FE' },
  { text: 'UC-08 Tendencia Peligrosa', rel: 'extend', cond: '(en el borde)', fill: '#E8F0FE' },
] }, file('04', 'Gráfico'));
await cu('04', '', [
  cls('ui1', 'ui', 'UserControl', 'RecepcionRINControl', ['(recepción)']),
  cls('ui2', 'ui', 'UserControl', 'PortalControl', ['(portal del paciente)']),
  cls('logic', 'logic', 'BLL', 'MedicionRINLogic', ['+ Reportar(medicion, usuario) : ResultadoReporteRIN', '+ CalcularCriticidad(valor, limInf, limSup) : NivelCriticidad', '+ ObtenerUltima(idPaciente) : MedicionRIN?', '+ ObtenerHistorial(idPaciente) : List<MedicionRIN>']),
  ctxNode, rejNode,
  ent('med', 'MedicionRIN', ['- ValorRIN (0,1 – 20,0), FechaMedicion', '- Canal, NivelCriticidad', '- PorTendenciaPeligrosa']),
  ent('hc', 'HistoriaClinica', ['- LimiteInferiorRIN, LimiteSuperiorRIN']),
  ent('al', 'Alerta', ['- Tipo, NivelCriticidad, Estado', '- NotificadaMedico / Administrativo']),
  'ui1 -> logic [label="invoca" style=dashed arrowhead=open]; ui2 -> logic [style=dashed arrowhead=open]; logic -> ctx [label="lee HC y persiste" style=dashed arrowhead=open]; logic -> rej [label="rechaza" style=dashed arrowhead=open]; logic -> med [label="crea/clasifica" style=dashed arrowhead=open]; logic -> al [label="genera si es Alta" style=dashed arrowhead=open]; logic -> hc [label="consulta rango" style=dashed arrowhead=open]; al -> med [arrowhead=none taillabel="*" headlabel="1" label="origen"];',
].join('\n'));
seq({ title: 'UC-04 — Reportar Valor de RIN: diagrama de secuencia', parts: [P('a', 'Administrativo /\nPaciente', true), P('f', ':RecepcionRINControl\n(UI)'), P('l', ':MedicionRINLogic\n(BLL)'), CTX], steps: [
  { from: 'a', to: 'f', text: '1: ingresar valor de RIN, fecha y canal' },
  { from: 'f', to: 'l', text: '2: Reportar(medicion, usuario)' },
  { from: 'l', to: 'c', text: '3: leer paciente, historia clínica y medición anterior' },
  { from: 'c', to: 'l', text: '4: datos', ret: true },
  { from: 'l', to: 'l', text: '5: validar (rango 0,1–20,0; fecha no futura)' },
  { alt: [
    { guard: 'validación falla', steps: [E('6: ValidacionNegocioException'), { from: 'f', to: 'a', text: '7: mostrar errores', ret: true }] },
    { guard: 'válida', steps: [
      { from: 'l', to: 'l', text: '8: CalcularCriticidad(valor, límites) → Baja/Media/Alta  (UC-05)' },
      { from: 'l', to: 'l', text: '9: evaluar tendencia con la medición anterior  (UC-08)' },
      { from: 'l', to: 'c', text: '10: Add(medicion) + SaveChanges()' },
      { from: 'c', to: 'c', text: '11: firmar DVH/DVV + auditar' },
      { from: 'c', to: 'l', text: '12: ok', ret: true },
      { alt: [{ guard: 'criticidad Alta', steps: [{ from: 'l', to: 'c', text: '13: Add(alerta, notificada) + SaveChanges()  (UC-07)' }, { from: 'c', to: 'l', text: '14: ok', ret: true }] }] },
      E('15: ResultadoReporteRIN(medición, alerta)'),
      { from: 'f', to: 'a', text: '16: confirmación + clasificación', ret: true },
    ] },
  ] },
] }, file('04', 'Secuencia'));

// ============ UC-05 ============
ucg({ title: 'UC-05 — Calcular Criticidad del Reporte: gráfico', left: ['Sistema (automático)'], main: 'UC-05 Calcular Criticidad del Reporte', rel: [{ text: 'UC-04 Reportar Valor de RIN', rel: 'include', fill: '#E8F0FE' }] }, file('05', 'Gráfico'));
await dot(`rankdir=LR; nodesep=0.5; ranksep=0.9;
${cls('logic', 'logic', 'BLL', 'MedicionRINLogic', ['+ CalcularCriticidad(valor, limInf, limSup) : NivelCriticidad', '+ Reportar(medicion, usuario) : ResultadoReporteRIN', '# EnBordeDelRango(valor, hc) : bool', 'DistanciaAlta = 0,5   MargenBorde = 0,1'])}
${cls('enum', 'rec', 'enum', 'NivelCriticidad', ['Baja  (dentro del rango)', 'Media (fuera por menos de 0,5)', 'Alta  (fuera por 0,5 o más, o por tendencia)'])}
${ent('hc', 'HistoriaClinica', ['- LimiteInferiorRIN', '- LimiteSuperiorRIN'])}
${ent('med', 'MedicionRIN', ['- ValorRIN, NivelCriticidad', '- PorTendenciaPeligrosa'])}
${ctxNode}
logic -> hc [label="lee límites" style=dashed arrowhead=open]; logic -> med [label="clasifica y marca" style=dashed arrowhead=open]; logic -> enum [label="devuelve" style=dashed arrowhead=open]; logic -> ctx [label="persiste" style=dashed arrowhead=open];`, 'UC-05/UC-05 - Clases afectadas');
seq({ title: 'UC-05 — Calcular Criticidad: diagrama de secuencia', parts: [P('s', 'Reportar()\n(UC-04)'), P('l', ':MedicionRINLogic\n(BLL)'), CTX], steps: [
  { from: 's', to: 'l', text: '1: nueva medición validada' },
  { from: 'l', to: 'c', text: '2: leer límites de la historia clínica y la medición anterior' },
  { from: 'c', to: 'l', text: '3: límites + anterior', ret: true },
  { alt: [
    { guard: 'valor dentro del rango', steps: [{ from: 'l', to: 'l', text: '4: criticidad = Baja' }] },
    { guard: 'fuera por menos de 0,5', steps: [{ from: 'l', to: 'l', text: '5: criticidad = Media' }] },
    { guard: 'fuera por 0,5 o más', steps: [{ from: 'l', to: 'l', text: '6: criticidad = Alta' }] },
  ] },
  { alt: [{ guard: 'ambas mediciones a ±0,1 de un límite  (UC-08)', steps: [{ from: 'l', to: 'l', text: '7: criticidad = Alta; PorTendenciaPeligrosa = true' }] }] },
  { from: 'l', to: 's', text: '8: NivelCriticidad + PorTendenciaPeligrosa', ret: true },
] }, 'UC-05/UC-05 - Secuencia');

// ============ UC-06 ============
ucg({ title: 'UC-06 — Generar Panel de Alertas: gráfico', left: ['Médico'], right: ['Administrativo'], main: 'UC-06 Generar Panel de Alertas', rel: [{ text: 'UC-10 Gestionar seguimiento', rel: 'extend', cond: '(acción de contacto)', fill: '#E8F0FE' }] }, file('06', 'Gráfico'));
await cu('06', '', [
  cls('ui', 'ui', 'UserControl', 'PanelTriajeControl', ['+ Pausar() : void', '+ Reanudar() : void', '+ RefrescarTextos() : void']),
  cls('logic', 'logic', 'BLL', 'PanelMonitoreoLogic', ['+ ObtenerPanel() : PanelMonitoreo']),
  cls('al', 'logic', 'BLL', 'AlertaLogic', ['+ ObtenerActivas() : List<Alerta>', '+ ObtenerPorPaciente(idPaciente) : List<Alerta>', '+ Resolver(idAlerta, accion, usuario) : void']),
  ctxRead,
  ent('pac', 'Paciente', ['- NombreCompleto, Estado']),
  ent('med', 'MedicionRIN', ['- ValorRIN, NivelCriticidad']),
  ent('alr', 'Alerta', ['- Tipo, Estado, NivelCriticidad']),
  'ui -> logic [label="invoca" style=dashed arrowhead=open]; ui -> al [style=dashed arrowhead=open]; logic -> ctx [label="consulta" style=dashed arrowhead=open]; logic -> pac [label="activos" style=dashed arrowhead=open]; logic -> med [label="última medición" style=dashed arrowhead=open]; logic -> alr [label="alertas activas" style=dashed arrowhead=open];',
].join('\n'));
seq({ title: 'UC-06 — Generar Panel de Alertas: diagrama de secuencia', parts: [P('a', 'Médico /\nAdministrativo', true), P('f', ':PanelTriajeControl\n(UI)'), P('l', ':PanelMonitoreoLogic\n(BLL)'), CTX], steps: [
  { from: 'a', to: 'f', text: '1: abrir el panel (o dispara el refresco automático)' },
  { from: 'f', to: 'l', text: '2: ObtenerPanel()' },
  { from: 'l', to: 'c', text: '3: consultar pacientes activos, mediciones y alertas' },
  { from: 'c', to: 'l', text: '4: datos', ret: true },
  { from: 'l', to: 'l', text: '5: clasificar y priorizar (triaje)' },
  { from: 'l', to: 'l', text: '6: armar tarjetas por severidad con alertas activas' },
  { alt: [
    { guard: 'hay pacientes', steps: [{ from: 'l', to: 'f', text: '7: PanelMonitoreo (triaje ordenado)', ret: true }, { from: 'f', to: 'a', text: '8: mostrar panel priorizado', ret: true }] },
    { guard: 'sin datos', steps: [{ from: 'l', to: 'f', text: '9: panel vacío', ret: true }, { from: 'f', to: 'a', text: '10: estado vacío informativo', ret: true }] },
  ] },
] }, file('06', 'Secuencia'));

// ============ UC-07 ============
ucg({ title: 'UC-07 — Notificar Alerta Urgente: gráfico', left: ['Sistema (automático)'], right: ['Médico'], main: 'UC-07 Notificar Alerta Urgente', rel: [{ text: 'Escalar al médico', rel: 'extend', cond: '(3 intentos fallidos)' }, { text: 'UC-04 Reportar Valor de RIN', rel: 'extend', cond: '', fill: '#E8F0FE' }].slice(0, 1) }, file('07', 'Gráfico'));
await cu('07', '', [
  cls('ml', 'logic', 'BLL', 'MedicionRINLogic', ['+ Reportar(medicion, usuario)', '  genera la alerta con', '  NotificadaMedico = true', '  NotificadaAdministrativo = true']),
  cls('sl', 'logic', 'BLL', 'SeguimientoLogic', ['+ RegistrarIntentoContacto(intento, ...)', 'UmbralIntentosFallidos = 3', '  → EscaladaAMedico = true']),
  cls('al', 'logic', 'BLL', 'AlertaLogic', ['+ ObtenerActivas() : List<Alerta>', '+ Resolver(idAlerta, accion, usuario)']),
  cls('ui', 'ui', 'UserControl', 'PanelTriajeControl', ['(muestra el aviso urgente)']),
  ctxNode,
  ent('alr', 'Alerta', ['- NotificadaMedico, NotificadaAdministrativo', '- EscaladaAMedico, FechaEscalada', '- Estado, FechaResolucion']),
  'ml -> alr [label="crea y notifica" style=dashed arrowhead=open]; sl -> alr [label="escala" style=dashed arrowhead=open]; al -> alr [label="consulta/resuelve" style=dashed arrowhead=open]; ml -> ctx [style=dashed arrowhead=open label="persiste"]; sl -> ctx [style=dashed arrowhead=open]; ui -> al [label="consulta" style=dashed arrowhead=open];',
].join('\n'));
seq({ title: 'UC-07 — Notificar Alerta Urgente: diagrama de secuencia', parts: [P('s', 'Reportar()\n(UC-04)'), P('l', ':MedicionRINLogic\n(BLL)'), CTX, P('k', ':SeguimientoLogic\n(BLL)'), P('u', ':PanelTriajeControl\n(UI)')], steps: [
  { from: 's', to: 'l', text: '1: criticidad Alta detectada' },
  { from: 'l', to: 'c', text: '2: Add(alerta: NotificadaMedico/Administrativo = true) + SaveChanges()' },
  { from: 'c', to: 'c', text: '3: firmar DVH/DVV + auditar' },
  { from: 'c', to: 'l', text: '4: ok', ret: true },
  { from: 'u', to: 'c', text: '5: refresco: leer alertas activas' },
  { from: 'c', to: 'u', text: '6: alertas (urgentes destacadas)', ret: true },
  { alt: [
    { guard: 'se resuelve (UC-06 / UC-10)', steps: [{ from: 'u', to: 'c', text: '7: Resolver(alerta) → Estado = Resuelta' }] },
    { guard: '3 intentos de contacto fallidos', steps: [{ from: 'k', to: 'c', text: '8: EscaladaAMedico = true, FechaEscalada = ahora' }, { from: 'c', to: 'u', text: '9: alerta escalada al médico', ret: true }] },
  ] },
] }, file('07', 'Secuencia'));

// ============ UC-08 ============
ucg({ title: 'UC-08 — Reclasificar por Tendencia Peligrosa: gráfico', left: ['Sistema (automático)'], main: 'UC-08 Reclasificar por Tendencia Peligrosa', rel: [{ text: 'UC-04 Reportar Valor de RIN', rel: 'extend', cond: '', fill: '#E8F0FE' }] }, file('08', 'Gráfico'));
await dot(`rankdir=LR; nodesep=0.5; ranksep=0.9;
${cls('logic', 'logic', 'BLL', 'MedicionRINLogic', ['+ Reportar(medicion, usuario) : ResultadoReporteRIN', '# EnBordeDelRango(valor, hc) : bool', 'MargenBorde = ±0,1'])}
${ent('med', 'MedicionRIN', ['- ValorRIN, NivelCriticidad', '- PorTendenciaPeligrosa : bool'])}
${ent('al', 'Alerta', ['- Tipo = TendenciaPeligrosa', '- NivelCriticidad = Alta'])}
${ctxNode}
logic -> med [label="marca y sube a Alta" style=dashed arrowhead=open]; logic -> al [label="genera" style=dashed arrowhead=open]; logic -> ctx [label="lee anterior y persiste" style=dashed arrowhead=open];`, 'UC-08/UC-08 - Clases afectadas');
seq({ title: 'UC-08 — Reclasificar por Tendencia Peligrosa: diagrama de secuencia', parts: [P('s', 'Reportar()\n(UC-04)'), P('l', ':MedicionRINLogic\n(BLL)'), CTX], steps: [
  { from: 's', to: 'l', text: '1: medición nueva (valor y límites del paciente)' },
  { from: 'l', to: 'c', text: '2: leer la medición anterior del paciente' },
  { from: 'c', to: 'l', text: '3: medición anterior', ret: true },
  { alt: [
    { guard: 'no hay medición anterior', steps: [{ from: 'l', to: 's', text: '4: sin evaluación de tendencia', ret: true }] },
    { guard: 'ambas a ±0,1 de un límite', steps: [
      { from: 'l', to: 'l', text: '5: PorTendenciaPeligrosa = true; NivelCriticidad = Alta' },
      { from: 'l', to: 'c', text: '6: Add(medicion) + Add(alerta TendenciaPeligrosa) + SaveChanges()' },
      { from: 'c', to: 'l', text: '7: ok', ret: true },
      { from: 'l', to: 's', text: '8: medición reclasificada a Alta + alerta', ret: true },
    ] },
    { guard: 'la anterior no está en el borde', steps: [{ from: 'l', to: 's', text: '9: criticidad según el valor (sin tendencia)', ret: true }] },
  ] },
] }, 'UC-08/UC-08 - Secuencia');

// ============ UC-09 ============
ucg({ title: 'UC-09 — Gestionar Agenda y Reasignación Inteligente: gráfico', left: ['Administrativo'], right: ['Médico'], main: 'UC-09 Gestionar Agenda y Reasignación Inteligente', rel: [{ text: 'Escalar rechazo al médico', rel: 'extend', cond: '(si se rechaza)' }] }, file('09', 'Gráfico'));
await cu('09', '', [
  cls('ui', 'ui', 'UserControl', 'AgendaControl', ['(agenda y sugerencias)']),
  cls('logic', 'logic', 'BLL', 'TurnoLogic', ['+ ObtenerAgenda(desde, hasta) : List<Turno>', '+ AgendarTurno(turno, usuario) : Turno', '+ GenerarSugerencias() : List<SugerenciaReasignacion>', '+ AceptarSugerencia(sugerencia, usuario) : ResultadoReasignacion', '+ RechazarSugerencia(sugerencia, motivo, escalar, usuario) : Alerta?']),
  cls('sug', 'rec', 'record', 'SugerenciaReasignacion', ['TurnoLiberado, PacienteCandidato, Motivo']),
  ctxNode,
  ent('tu', 'Turno', ['- FechaHora, Estado, Observaciones', '- IdPaciente, IdUsuarioMedico']),
  ent('al', 'Alerta', ['- Tipo, Estado (escalada al médico)']),
  'ui -> logic [label="invoca" style=dashed arrowhead=open]; logic -> sug [label="genera" style=dashed arrowhead=open]; logic -> tu [label="reasigna" style=dashed arrowhead=open]; logic -> al [label="crea si escala" style=dashed arrowhead=open]; logic -> ctx [label="persiste" style=dashed arrowhead=open];',
].join('\n'));
seq({ title: 'UC-09 — Gestionar Agenda y Reasignación: diagrama de secuencia', parts: [P('a', 'Administrativo', true), P('f', ':AgendaControl\n(UI)'), P('l', ':TurnoLogic\n(BLL)'), CTX], steps: [
  { from: 'a', to: 'f', text: '1: consultar la agenda del período' },
  { from: 'f', to: 'l', text: '2: ObtenerAgenda(desde, hasta)' },
  { from: 'l', to: 'c', text: '3: consultar turnos del período' },
  { from: 'c', to: 'l', text: '4: turnos', ret: true },
  { from: 'l', to: 'f', text: '5: agenda del período', ret: true },
  { from: 'f', to: 'l', text: '6: GenerarSugerencias()' },
  { from: 'l', to: 'l', text: '7: cruzar turnos liberados con pacientes críticos sin turno próximo' },
  { from: 'l', to: 'f', text: '8: sugerencias de reasignación', ret: true },
  { alt: [
    { guard: 'sin sugerencias', steps: [{ from: 'f', to: 'a', text: '9: no hay reasignaciones pendientes', ret: true }] },
    { guard: 'el administrativo acepta', steps: [
      { from: 'f', to: 'l', text: '10: AceptarSugerencia(sugerencia, usuario)' },
      { from: 'l', to: 'c', text: '11: reasignar turno + SaveChanges()' },
      { from: 'c', to: 'c', text: '12: firmar DVH/DVV + auditar' },
      { from: 'c', to: 'l', text: '13: ok', ret: true },
      { from: 'l', to: 'f', text: '14: ResultadoReasignacion(turno, notificaciones)', ret: true },
    ] },
    { guard: 'el administrativo rechaza', steps: [
      { from: 'f', to: 'l', text: '15: RechazarSugerencia(sugerencia, motivo, escalar, usuario)' },
      { alt: [{ guard: 'escalar al médico', steps: [{ from: 'l', to: 'c', text: '16: Add(alerta de escalada) + SaveChanges()' }, { from: 'c', to: 'l', text: '17: ok', ret: true }] }] },
      { from: 'l', to: 'f', text: '18: rechazo registrado (con motivo)', ret: true },
    ] },
  ] },
] }, file('09', 'Secuencia'));

// ============ UC-10 ============
ucg({ title: 'UC-10 — Monitorear Plazos y Gestionar Seguimiento: gráfico', left: ['Administrativo'], right: ['Médico'], main: 'UC-10 Monitorear Plazos y Gestionar Seguimiento', rel: [{ text: 'Registrar decisión clínica y escalada', rel: 'extend', cond: '(si el caso escala)' }] }, file('10', 'Gráfico'));
await cu('10', '', [
  cls('ui', 'ui', 'UserControl', 'SeguimientoControl', ['(lista de contactos y registro)']),
  cls('logic', 'logic', 'BLL', 'SeguimientoLogic', ['+ DetectarPlazosVencidos() : List<Alerta>', '+ RegistrarIntentoContacto(intento, usuario) : ResultadoIntentoContacto', '+ ObtenerSeguimientos(idPaciente) : List<Seguimiento>', 'UmbralIntentosFallidos = 3']),
  ctxNode,
  ent('seg', 'Seguimiento', ['- TipoContacto, Resultado, Observaciones', '- DecisionClinica, DetalleDecision']),
  ent('al', 'Alerta', ['- Tipo (ReporteAusente), Estado', '- EscaladaAMedico, FechaEscalada']),
  ent('hc', 'HistoriaClinica', ['- PeriodicidadDias, ProximaFechaControl']),
  'ui -> logic [label="invoca" style=dashed arrowhead=open]; logic -> seg [label="registra" style=dashed arrowhead=open]; logic -> al [label="cierra / escala" style=dashed arrowhead=open]; logic -> hc [label="detecta vencidos" style=dashed arrowhead=open]; logic -> ctx [label="persiste" style=dashed arrowhead=open];',
].join('\n'));
seq({ title: 'UC-10 — Monitorear Plazos y Gestionar Seguimiento: diagrama de secuencia', parts: [P('a', 'Administrativo /\nMédico', true), P('f', ':SeguimientoControl\n(UI)'), P('l', ':SeguimientoLogic\n(BLL)'), CTX], steps: [
  { from: 'a', to: 'f', text: '1: abrir el módulo de seguimiento' },
  { from: 'f', to: 'l', text: '2: DetectarPlazosVencidos()' },
  { from: 'l', to: 'c', text: '3: leer historias con control vencido + alertas activas' },
  { from: 'c', to: 'l', text: '4: vencimientos y alertas', ret: true },
  { from: 'l', to: 'f', text: '5: lista de contactos del día', ret: true },
  { from: 'a', to: 'f', text: '6: registrar intento de contacto y resultado' },
  { from: 'f', to: 'l', text: '7: RegistrarIntentoContacto(intento, usuario)' },
  { from: 'l', to: 'c', text: '8: Add(seguimiento) + SaveChanges()' },
  { from: 'c', to: 'c', text: '9: firmar DVH/DVV + auditar' },
  { from: 'c', to: 'l', text: '10: ok', ret: true },
  { alt: [
    { guard: 'contacto exitoso', steps: [{ from: 'l', to: 'c', text: '11: cerrar la alerta asociada' }, { from: 'c', to: 'l', text: '12: ok', ret: true }] },
    { guard: 'requiere decisión del médico', steps: [{ from: 'l', to: 'c', text: '13: registrar decisión clínica; EscaladaAMedico = true' }, { from: 'c', to: 'l', text: '14: ok', ret: true }] },
    { guard: 'sin contacto', steps: [{ from: 'l', to: 'c', text: '15: acumular intento fallido (al 3.º se escala)' }, { from: 'c', to: 'l', text: '16: ok', ret: true }] },
  ] },
  { from: 'l', to: 'f', text: '17: ResultadoIntentoContacto(intento, cerróAlerta, escaló)', ret: true },
  { from: 'f', to: 'a', text: '18: confirmación del seguimiento', ret: true },
] }, file('10', 'Secuencia'));

// ============ UC-11 ============
ucg({ title: 'UC-11 — Generar Reporte Mensual de Eficacia: gráfico', left: ['Médico'], right: ['Administrador del sistema'], main: 'UC-11 Generar Reporte Mensual de Eficacia', rel: [{ text: 'UC-13 Exportar reporte', rel: 'extend', fill: '#E8F0FE' }] }, file('11', 'Gráfico'));
await cu('11', '', [
  cls('ui', 'ui', 'UserControl', 'ReportesControl', ['(generación y visualización)']),
  cls('logic', 'logic', 'BLL', 'ReporteLogic', ['+ GenerarReporteMensual(anio, mes, usuario) : ReporteEstadistico', '+ Clasificar(porcentajeEnRango) : ClasificacionControl', 'UmbralOptimo = 80 %   UmbralSuboptimo = 50 %']),
  cls('enum', 'rec', 'enum', 'ClasificacionControl', ['Optimo (≥ 80 %)', 'Suboptimo (≥ 50 % y < 80 %)', 'Deficiente (< 50 %)']),
  ctxNode,
  ent('rep', 'ReporteEstadistico', ['- Anio, Mes', '- IndicadorGlobalEficacia', '- CantidadOptimos / Suboptimos / Deficientes', '- DetalleJson']),
  ent('med', 'MedicionRIN', ['- ValorRIN, FechaMedicion']),
  'ui -> logic [label="invoca" style=dashed arrowhead=open]; logic -> med [label="consolida" style=dashed arrowhead=open]; logic -> enum [label="clasifica" style=dashed arrowhead=open]; logic -> rep [label="genera / regenera" style=dashed arrowhead=open]; logic -> ctx [label="persiste" style=dashed arrowhead=open];',
].join('\n'));
seq({ title: 'UC-11 — Generar Reporte Mensual de Eficacia: diagrama de secuencia', parts: [P('a', 'Médico /\nAdministrador', true), P('f', ':ReportesControl\n(UI)'), P('l', ':ReporteLogic\n(BLL)'), CTX], steps: [
  { from: 'a', to: 'f', text: '1: elegir período (año y mes)' },
  { from: 'f', to: 'l', text: '2: GenerarReporteMensual(anio, mes, usuario)' },
  { from: 'l', to: 'c', text: '3: obtener mediciones del período' },
  { from: 'c', to: 'l', text: '4: mediciones', ret: true },
  { alt: [
    { guard: 'período sin mediciones', steps: [{ from: 'l', to: 'f', text: '5: sin datos (no se genera)', ret: true }, { from: 'f', to: 'a', text: '6: informar ausencia de datos', ret: true }] },
    { guard: 'hay mediciones', steps: [
      { from: 'l', to: 'l', text: '7: Clasificar(% en rango) por paciente; indicador global' },
      { alt: [{ guard: 'reporte ya existente', steps: [{ from: 'l', to: 'c', text: '8: actualizar el reporte del período' }] }, { guard: 'no existía', steps: [{ from: 'l', to: 'c', text: '9: Add(reporte)' }] }] },
      { from: 'l', to: 'c', text: '10: SaveChanges()' },
      { from: 'c', to: 'c', text: '11: firmar DVH/DVV + auditar' },
      { from: 'c', to: 'l', text: '12: ok', ret: true },
      { from: 'l', to: 'f', text: '13: ReporteEstadistico del período', ret: true },
      { from: 'f', to: 'a', text: '14: mostrar reporte y ofrecer exportación', ret: true },
    ] },
  ] },
] }, file('11', 'Secuencia'));

// ============ UC-12 ============
ucg({ title: 'UC-12 — Generar Reporte por Diagnóstico: gráfico', left: ['Médico'], right: ['Administrador del sistema'], main: 'UC-12 Generar Reporte por Diagnóstico', rel: [{ text: 'UC-13 Exportar reporte', rel: 'extend', fill: '#E8F0FE' }] }, file('12', 'Gráfico'));
await cu('12', '', [
  cls('ui', 'ui', 'UserControl', 'ReportesControl', ['(reporte por diagnóstico)']),
  cls('logic', 'logic', 'BLL', 'ReporteLogic', ['+ GenerarReportePorDiagnostico(anio, mes, idDiagnostico?, usuario) : List<FilaReporteDiagnostico>']),
  cls('fila', 'rec', 'record', 'FilaReporteDiagnostico', ['Diagnostico, Indicador', 'Cantidades (óptimos / subóptimos / deficientes)']),
  ctxNode,
  ent('dx', 'Diagnostico', ['- Nombre, Descripcion, Activo']),
  ent('hc', 'HistoriaClinica', ['- IdDiagnostico']),
  ent('med', 'MedicionRIN', ['- ValorRIN, FechaMedicion']),
  'ui -> logic [label="invoca" style=dashed arrowhead=open]; logic -> ctx [label="consultas agrupadas" style=dashed arrowhead=open]; logic -> fila [label="devuelve" style=dashed arrowhead=open]; logic -> med [label="agrupa" style=dashed arrowhead=open]; hc -> dx [arrowhead=none taillabel="*" headlabel="0..1" label="indica"]; logic -> hc [label="usa el diagnóstico de" style=dashed arrowhead=open];',
].join('\n'));
seq({ title: 'UC-12 — Generar Reporte por Diagnóstico: diagrama de secuencia', parts: [P('a', 'Médico /\nAdministrador', true), P('f', ':ReportesControl\n(UI)'), P('l', ':ReporteLogic\n(BLL)'), CTX], steps: [
  { from: 'a', to: 'f', text: '1: elegir período y diagnóstico (o todos)' },
  { from: 'f', to: 'l', text: '2: GenerarReportePorDiagnostico(anio, mes, idDiagnostico, usuario)' },
  { from: 'l', to: 'c', text: '3: obtener mediciones agrupadas por diagnóstico' },
  { from: 'c', to: 'l', text: '4: mediciones por grupo', ret: true },
  { from: 'l', to: 'l', text: '5: consolidar indicadores por diagnóstico' },
  { alt: [
    { guard: 'diagnóstico sin datos en el período', steps: [{ from: 'l', to: 'l', text: '6: fila informada vacía' }] },
    { guard: 'todos los diagnósticos', steps: [{ from: 'l', to: 'l', text: '7: consolidar todos los grupos activos' }] },
  ] },
  { from: 'l', to: 'f', text: '8: List<FilaReporteDiagnostico>', ret: true },
  { from: 'f', to: 'a', text: '9: mostrar tabla por diagnóstico (exportable)', ret: true },
] }, file('12', 'Secuencia'));

// ============ UC-13 ============
ucg({ title: 'UC-13 — Consultar Historial y Exportar Reportes: gráfico', left: ['Médico'], right: ['Administrativo'], main: 'UC-13 Consultar Historial y Exportar Reportes', rel: [{ text: 'Exportar el historial (PDF / Excel / JSON)', rel: 'extend' }] }, file('13', 'Gráfico'));
await cu('13', '', [
  cls('ui', 'ui', 'UserControl', 'ReportesControl', ['(historial y exportación)']),
  cls('logic', 'logic', 'BLL', 'ReporteLogic', ['+ ObtenerHistorialPaciente(idPaciente) : List<ItemHistorial>', '+ ExportarHistorialPaciente(idPaciente, formato, usuario) : ExportacionHistorial']),
  cls('exp', 'infra', 'static', 'Exportadores', ['+ CarpetaExportacion() : string', '+ EscribirHistorial(exportacion, paciente, formato) : string']),
  cls('it', 'rec', 'record', 'ItemHistorial', ['Fecha, Tipo, Descripcion']),
  cls('eh', 'rec', 'record', 'ExportacionHistorial', ['NombreArchivoSugerido, Items']),
  ctxRead,
  'ui -> logic [label="invoca" style=dashed arrowhead=open]; ui -> exp [label="escribe el archivo" style=dashed arrowhead=open]; logic -> ctx [label="consulta" style=dashed arrowhead=open]; logic -> it [label="compone" style=dashed arrowhead=open]; logic -> eh [label="devuelve" style=dashed arrowhead=open]; exp -> eh [label="usa" style=dashed arrowhead=open];',
].join('\n'));
seq({ title: 'UC-13 — Consultar Historial y Exportar Reportes: diagrama de secuencia', parts: [P('a', 'Médico /\nAdministrativo', true), P('f', ':ReportesControl\n(UI)'), P('l', ':ReporteLogic\n(BLL)'), CTX, P('x', ':Exportadores\n(static)')], steps: [
  { from: 'a', to: 'f', text: '1: elegir paciente' },
  { from: 'f', to: 'l', text: '2: ObtenerHistorialPaciente(idPaciente)' },
  { from: 'l', to: 'c', text: '3: consultar mediciones, alertas y seguimientos' },
  { from: 'c', to: 'l', text: '4: datos', ret: true },
  { from: 'l', to: 'f', text: '5: List<ItemHistorial>', ret: true },
  { alt: [
    { guard: 'historial sin ítems', steps: [{ from: 'f', to: 'a', text: '6: historial vacío informativo', ret: true }] },
    { guard: 'con ítems: el usuario elige PDF, Excel o JSON', steps: [
      { from: 'f', to: 'l', text: '7: ExportarHistorialPaciente(idPaciente, formato, usuario)' },
      { from: 'l', to: 'l', text: '8: construir ExportacionHistorial' },
      { from: 'l', to: 'l', text: '9: registrar la exportación en auditoría / bitácora' },
      { from: 'l', to: 'f', text: '10: ExportacionHistorial', ret: true },
      { from: 'f', to: 'x', text: '11: EscribirHistorial(exportacion, paciente, formato)' },
      { from: 'x', to: 'x', text: '12: generar el archivo en la carpeta de exportación' },
      { from: 'x', to: 'f', text: '13: ruta del archivo', ret: true },
      { from: 'f', to: 'a', text: '14: «Historial exportado» + ubicación', ret: true },
    ] },
  ] },
] }, file('13', 'Secuencia'));
console.log('uc ok');
