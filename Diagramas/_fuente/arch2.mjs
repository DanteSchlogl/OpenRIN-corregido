import { dot, cls } from './lib.mjs';
const E = 'style=dashed arrowhead=open';
const L = (id, name, m) => cls(id, 'logic', 'BLL', name, m);
const cl = (n, label, body) => `subgraph cluster_${n} { label="${label}"; style=rounded; color="#4F8F5A"; fontname="Arial"; fontsize=12; margin=22; ${body} }`;

// ---------- Lógica de negocio (cuadrícula 2x2) ----------
await dot(`rankdir=TB; nodesep=0.5; ranksep=0.5; compound=true; newrank=true;
${cl('a', 'Pacientes e historia clínica', [
  L('pl', 'PacienteLogic', ['+ Buscar(filtro, incluirInactivos)', '+ BuscarPorDNI(dni) : Paciente?', '+ RegistrarPaciente(paciente, usuario) : CredencialesPortal', '+ ModificarPaciente(paciente, usuario)', '+ DarDeBajaPaciente(id, motivo, usuario)', '(accede vía IPacienteRepositorio)']),
  L('hl', 'HistoriaClinicaLogic', ['+ Configurar(configuracion, usuario) : HistoriaClinica', '+ ObtenerPorPaciente(idPaciente)', '+ ActualizarPeriodicidad(idPaciente, dias, usuario)']),
  L('el', 'EventoAdversoLogic', ['+ Registrar(evento, usuario) : EventoAdverso', '+ ObtenerPorPaciente(idPaciente)'])].join('\n'))}
${cl('b', 'Triaje y alertas', [
  L('ml', 'MedicionRINLogic', ['+ Reportar(medicion, usuario) : ResultadoReporteRIN', '+ CalcularCriticidad(valor, limInf, limSup) : NivelCriticidad', '+ ObtenerUltima(idPaciente) : MedicionRIN?', '+ ObtenerHistorial(idPaciente)']),
  L('al', 'AlertaLogic', ['+ ObtenerActivas() : List<Alerta>', '+ ObtenerPorPaciente(idPaciente)', '+ Resolver(idAlerta, accion, usuario)']),
  L('pm', 'PanelMonitoreoLogic', ['+ ObtenerPanel() : PanelMonitoreo'])].join('\n'))}
${cl('c', 'Agenda y seguimiento', [
  L('tl', 'TurnoLogic', ['+ ObtenerAgenda(desde, hasta)', '+ AgendarTurno(turno, usuario) : Turno', '+ GenerarSugerencias()', '+ AceptarSugerencia(sugerencia, usuario)', '+ RechazarSugerencia(sugerencia, motivo, escalar, usuario)']),
  L('sl', 'SeguimientoLogic', ['+ DetectarPlazosVencidos() : List<Alerta>', '+ RegistrarIntentoContacto(intento, usuario)', '+ ObtenerSeguimientos(idPaciente)'])].join('\n'))}
${cl('d', 'Reportes e integridad', [
  L('rl', 'ReporteLogic', ['+ GenerarReporteMensual(anio, mes, usuario)', '+ GenerarReportePorDiagnostico(anio, mes, idDiagnostico, usuario)', '+ ObtenerHistorialPaciente(idPaciente)', '+ ExportarHistorialPaciente(idPaciente, formato, usuario)', '+ Clasificar(porcentajeEnRango) : ClasificacionControl']),
  L('au', 'AuditoriaLogic', ['+ ObtenerHistorial(entidad, idRegistro, maximo)']),
  L('il', 'IntegridadLogic', ['+ Auditar() : ResultadoAuditoria', '+ RecalcularTodo() : ResultadoAuditoria'])].join('\n'))}
nota [shape=note style=filled fillcolor="#FFFBE6" color="#C9B24A" fontsize=11 label="Todas las clases de lógica usan NegocioDbContext\n(punto único de guardado: firma DVH/DVV y audita)\ny ReglasNegocio.Rechazar(errores, contexto, usuario)\npara los rechazos auditados."];
pl -> hl -> el [style=invis]; ml -> al -> pm [style=invis]; tl -> sl [style=invis]; rl -> au -> il [style=invis];
el -> tl [style=invis]; pm -> rl [style=invis]; pl -> ml [style=invis constraint=false]; { rank=same; pl; ml; } { rank=same; tl; rl; }
sl -> nota [style=invis];`, 'Arquitectura/G06 - Logica de negocio (clases)');

// ---------- Presentación ----------
const U = (id, name, m = []) => cls(id, 'ui', 'UserControl', name, m);
const F = (id, name) => cls(id, 'ui', 'Form', name, []);
await dot(`rankdir=LR; nodesep=0.22; ranksep=1.1;
${cls('prog', 'infra', 'static', 'Program', ['+ Main() : void', '(auditoría de integridad antes del login)'])}
${cls('va', 'infra', 'static', 'VerificacionArranque', ['+ Auditar() : ResultadoAuditoria?'])}
${F('iv', 'IntegridadVulneradaForm')} ${F('lg', 'LoginForm')} ${F('rc', 'RecuperacionForm')} ${F('pf', 'PrincipalForm')}
${U('m1', 'PanelTriajeControl', ['+ Pausar() / Reanudar() / RefrescarTextos()'])}
${U('m2', 'PacientesControl')} ${U('m3', 'RecepcionRINControl')} ${U('m4', 'AgendaControl')} ${U('m5', 'SeguimientoControl')} ${U('m6', 'EventosAdversosControl')} ${U('m7', 'ReportesControl')} ${U('m8', 'PortalControl')} ${U('m9', 'SistemaControl', ['(usuarios, bitácora, cambios, idiomas, perfiles)'])}
${F('d1', 'PacienteEditorForm')} ${F('d1b', 'CredencialesPortalForm')} ${F('d2', 'HistoriaClinicaForm')} ${F('d8a', 'TextoDialogoForm')} ${F('d4', 'AgendarTurnoForm')} ${F('d8b', 'TextoDialogoForm')} ${F('d5', 'EventoAdversoForm')} ${F('d6', 'UsuarioNuevoForm')} ${F('d7', 'IdiomaNuevoForm')}
${cls('ex', 'infra', 'static', 'Exportadores', ['+ EscribirHistorial(exportacion, paciente, formato)'])}
nota [shape=note style=filled fillcolor="#FFFBE6" color="#C9B24A" label="Los 9 módulos y los diálogos delegan la lógica\\nen Negocio.BLL y los servicios técnicos en Services.Facade"];
prog -> va [label="1.º audita" ${E}]; prog -> iv [label="si falla" ${E}]; prog -> lg [label="si está OK" ${E}]; lg -> rc [${E} label="recuperar contraseña"]; prog -> pf [${E} label="sesión iniciada"];
pf -> m1 [${E}]; pf -> m2 [${E}]; pf -> m3 [${E}]; pf -> m4 [${E}]; pf -> m5 [${E}]; pf -> m6 [${E}]; pf -> m7 [${E}]; pf -> m8 [${E}]; pf -> m9 [${E}];
m2 -> d1 [${E}]; m2 -> d2 [${E}]; m2 -> d8a [${E}]; d1 -> d1b [${E}];
m4 -> d4 [${E}]; m4 -> d8b [${E}]; m6 -> d5 [${E}]; m9 -> d6 [${E}]; m9 -> d7 [${E}]; m7 -> ex [${E}];
{ rank=same; nota; }`, 'Arquitectura/G06 - Presentacion (clases)');
console.log('arch2 ok');
