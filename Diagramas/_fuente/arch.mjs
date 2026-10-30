import { dot, cls, esc, tbl } from './lib.mjs';

const E = 'style=dashed arrowhead=open';
const ent = (id, name, m) => cls(id, 'ent', 'entidad', name, m);

// ---------- Dominio ----------
await dot(`rankdir=LR; nodesep=0.45; ranksep=1.1; splines=true;
${ent('pac', 'Paciente', ['- Id «PK»', '- NombreCompleto : string', '- DNI : string «UQ»', '- Telefono, Email : string', '- IdObraSocial «FK»', '- NumeroAfiliado : string', '- IdUsuarioPortal «FK»', '- Estado : EstadoPaciente', '- MotivoBaja : string', '- FechaAlta, FechaBaja'])}
${ent('os', 'ObraSocial', ['- Id «PK»', '- Nombre : string', '- Activo : bool'])}
${ent('hc', 'HistoriaClinica', ['- Id «PK»', '- IdPaciente «FK, UQ»', '- IdDiagnostico «FK»', '- LimiteInferiorRIN : decimal', '- LimiteSuperiorRIN : decimal', '- Medicamento, Dosis : string', '- PeriodicidadDias : int', '- ProximaFechaControl : DateTime?'])}
${ent('dx', 'Diagnostico', ['- Id «PK»', '- Nombre, Descripcion : string', '- Activo : bool'])}
${ent('med', 'MedicionRIN', ['- Id «PK»', '- IdPaciente «FK»', '- ValorRIN : decimal', '- FechaMedicion : DateTime', '- Canal : CanalReporteRIN', '- IdUsuarioRegistro «FK»', '- NivelCriticidad : NivelCriticidad?', '- PorTendenciaPeligrosa : bool'])}
${ent('al', 'Alerta', ['- Id «PK»', '- IdPaciente, IdMedicion «FK»', '- Tipo : TipoAlerta', '- NivelCriticidad, Estado', '- NotificadaMedico / Administrativo : bool', '- EscaladaAMedico : bool', '- FechaEscalada, FechaResolucion', '- IdUsuarioResolucion «FK»'])}
${ent('tu', 'Turno', ['- Id «PK»', '- IdPaciente, IdUsuarioMedico «FK»', '- FechaHora : DateTime', '- Estado : EstadoTurno', '- Observaciones : string'])}
${ent('seg', 'Seguimiento', ['- Id «PK»', '- IdPaciente «FK»', '- TipoContacto, Resultado', '- DecisionClinica, DetalleDecision', '- IdUsuarioRegistro, IdUsuarioDecision «FK»'])}
${ent('ev', 'EventoAdverso', ['- Id «PK»', '- IdPaciente «FK»', '- Fecha : DateTime', '- Tipo : TipoEventoAdverso', '- Gravedad : GravedadEvento', '- Descripcion, AccionTomada', '- IdUsuarioRegistro «FK»'])}
${ent('us', 'Usuario', ['- Id «PK»', '- NombreUsuario, NombreCompleto', '- Perfil : PerfilUsuario', '- Email, Telefono, Activo'])}
subgraph cluster_solo { margin=22; label="Sin asociaciones directas"; style=dashed; color="#999999"; fontname="Arial"; fontsize=11;
${ent('rep', 'ReporteEstadistico', ['- Anio, Mes : int', '- IndicadorGlobalEficacia : decimal', '- CantidadOptimos / Suboptimos / Deficientes', '- DetalleJson : string'])}
${cls('dv', 'infra', 'static', 'DigitoVerificador', ['+ CalcularDVH(cadena) : string', '+ CalcularDVV(filas) : string'])} }
pac -> os [arrowhead=none taillabel="*" headlabel="0..1" label="cobertura"];
pac -> hc [arrowhead=none taillabel="1" headlabel="0..1" label="historia clínica"];
hc -> dx [arrowhead=none taillabel="*" headlabel="0..1" label="diagnóstico"];
pac -> med [arrowhead=none taillabel="1" headlabel="*" label="mediciones"];
med -> al [arrowhead=none taillabel="1" headlabel="*" label="genera"];
pac -> al [arrowhead=none taillabel="1" headlabel="*" label="alertas"];
pac -> tu [arrowhead=none taillabel="1" headlabel="*" label="turnos"];
pac -> seg [arrowhead=none taillabel="1" headlabel="*" label="seguimientos"];
pac -> ev [arrowhead=none taillabel="1" headlabel="*" label="eventos"];
us -> pac [arrowhead=none taillabel="0..1" headlabel="0..1" label="acceso al portal" style=dashed];
us -> tu [arrowhead=none taillabel="0..1" headlabel="*" label="médico asignado" style=dashed];
us -> seg [arrowhead=none taillabel="0..1" headlabel="*" label="registra / decide" style=dashed];
us -> ev [arrowhead=none taillabel="0..1" headlabel="*" label="registra" style=dashed];
us -> med [arrowhead=none taillabel="0..1" headlabel="*" label="carga" style=dashed];`, 'Arquitectura/G06 - Dominio (clases)', 'compound=true');

// ---------- Lógica de negocio ----------
const L = (id, name, m) => cls(id, 'logic', 'BLL', name, m);
await dot(`rankdir=TB; nodesep=0.4; ranksep=0.9; compound=true; newrank=true;
subgraph cluster_a { label="Pacientes e historia clínica"; style=rounded; color="#4F8F5A"; fontname="Arial"; fontsize=12;
${L('pl', 'PacienteLogic', ['+ Buscar(filtro, incluirInactivos)', '+ BuscarPorDNI(dni) : Paciente?', '+ RegistrarPaciente(paciente, usuario) : CredencialesPortal', '+ ModificarPaciente(paciente, usuario)', '+ DarDeBajaPaciente(id, motivo, usuario)'])}
${L('hl', 'HistoriaClinicaLogic', ['+ Configurar(configuracion, usuario) : HistoriaClinica', '+ ObtenerPorPaciente(idPaciente)', '+ ActualizarPeriodicidad(idPaciente, dias, usuario)'])}
${L('el', 'EventoAdversoLogic', ['+ Registrar(evento, usuario) : EventoAdverso', '+ ObtenerPorPaciente(idPaciente)'])} }
subgraph cluster_b { label="Triaje y alertas"; style=rounded; color="#4F8F5A"; fontname="Arial"; fontsize=12;
${L('ml', 'MedicionRINLogic', ['+ Reportar(medicion, usuario) : ResultadoReporteRIN', '+ CalcularCriticidad(valor, limInf, limSup) : NivelCriticidad', '+ ObtenerUltima(idPaciente) : MedicionRIN?', '+ ObtenerHistorial(idPaciente)'])}
${L('al', 'AlertaLogic', ['+ ObtenerActivas() : List<Alerta>', '+ ObtenerPorPaciente(idPaciente)', '+ Resolver(idAlerta, accion, usuario)'])}
${L('pm', 'PanelMonitoreoLogic', ['+ ObtenerPanel() : PanelMonitoreo'])} }
subgraph cluster_c { label="Agenda y seguimiento"; style=rounded; color="#4F8F5A"; fontname="Arial"; fontsize=12;
${L('tl', 'TurnoLogic', ['+ ObtenerAgenda(desde, hasta)', '+ AgendarTurno(turno, usuario) : Turno', '+ GenerarSugerencias()', '+ AceptarSugerencia(sugerencia, usuario)', '+ RechazarSugerencia(sugerencia, motivo, escalar, usuario)'])}
${L('sl', 'SeguimientoLogic', ['+ DetectarPlazosVencidos() : List<Alerta>', '+ RegistrarIntentoContacto(intento, usuario)', '+ ObtenerSeguimientos(idPaciente)'])} }
subgraph cluster_d { label="Reportes e integridad"; style=rounded; color="#4F8F5A"; fontname="Arial"; fontsize=12;
${L('rl', 'ReporteLogic', ['+ GenerarReporteMensual(anio, mes, usuario)', '+ GenerarReportePorDiagnostico(anio, mes, idDiagnostico, usuario)', '+ ObtenerHistorialPaciente(idPaciente)', '+ ExportarHistorialPaciente(idPaciente, formato, usuario)', '+ Clasificar(porcentajeEnRango) : ClasificacionControl'])}
${L('au', 'AuditoriaLogic', ['+ ObtenerHistorial(entidad, idRegistro, maximo)'])}
${L('il', 'IntegridadLogic', ['+ Auditar() : ResultadoAuditoria', '+ RecalcularTodo() : ResultadoAuditoria'])} }
${cls('ctx', 'infra', 'EF Core', 'NegocioDbContext', ['(punto único de guardado)'])}
${cls('irepo', 'infra', 'interface', 'IPacienteRepositorio', [])}
${cls('rn', 'infra', 'static', 'ReglasNegocio', ['+ Rechazar(errores, contexto, usuario)'])}
pl -> irepo [${E} label="accede vía"];
hl -> ctx [${E} ltail=cluster_a label="persiste"]; ml -> ctx [${E} ltail=cluster_b]; tl -> ctx [${E} ltail=cluster_c]; rl -> ctx [${E} ltail=cluster_d];
ctx -> rn [style=invis];
hl -> rn [${E} label="rechaza" constraint=false];`, 'Arquitectura/G06 - Logica de negocio (clases)');

// ---------- Presentación ----------
const U = (id, name, m = []) => cls(id, 'ui', 'UserControl', name, m);
const F = (id, name) => cls(id, 'ui', 'Form', name, []);
await dot(`rankdir=LR; nodesep=0.3; ranksep=1.0; compound=true; newrank=true;
${cls('prog', 'infra', 'static', 'Program', ['+ Main() : void  (auditoría de integridad pre-login)'])}
${cls('va', 'infra', 'static', 'VerificacionArranque', ['+ Auditar() : ResultadoAuditoria?'])}
${F('iv', 'IntegridadVulneradaForm')} ${F('lg', 'LoginForm')} ${F('rc', 'RecuperacionForm')} ${F('pf', 'PrincipalForm')}
subgraph cluster_mod { label="Módulos del shell (UserControl)"; style=rounded; color="#4A78C2"; fontname="Arial"; fontsize=12;
${U('m1', 'PanelTriajeControl', ['+ Pausar() / Reanudar() / RefrescarTextos()'])} ${U('m2', 'PacientesControl')} ${U('m3', 'RecepcionRINControl')} ${U('m4', 'AgendaControl')} ${U('m5', 'SeguimientoControl')} ${U('m6', 'EventosAdversosControl')} ${U('m7', 'ReportesControl')} ${U('m8', 'PortalControl')} ${U('m9', 'SistemaControl', ['(usuarios, bitácora, cambios, idiomas, perfiles)'])} }
subgraph cluster_dlg { label="Diálogos"; style=rounded; color="#4A78C2"; fontname="Arial"; fontsize=12;
${F('d1', 'PacienteEditorForm')} ${F('d2', 'HistoriaClinicaForm')} ${F('d3', 'CredencialesPortalForm')} ${F('d4', 'AgendarTurnoForm')} ${F('d5', 'EventoAdversoForm')} ${F('d6', 'UsuarioNuevoForm')} ${F('d7', 'IdiomaNuevoForm')} ${F('d8', 'TextoDialogoForm')} }
${cls('ex', 'infra', 'static', 'Exportadores', ['+ EscribirHistorial(exportacion, paciente, formato)'])}
${cls('bll', 'logic', 'capa', 'Negocio.BLL', ['(lógica de negocio)'])}
${cls('fac', 'svc', 'capa', 'Services.Facade', ['(seguridad, idiomas, bitácora, respaldos)'])}
prog -> va [label="1.º audita" ${E}]; prog -> iv [label="si falla" ${E}]; prog -> lg [label="si está OK" ${E}]; lg -> rc [${E} label="recuperar"]; prog -> pf [${E} label="tras el login"];
pf -> m1 [lhead=cluster_mod ${E} label="aloja"];
m2 -> d1 [${E}]; m2 -> d2 [${E}]; m2 -> d8 [${E}]; d1 -> d3 [${E}]; m4 -> d4 [${E}]; m4 -> d8 [${E}]; m6 -> d5 [${E}]; m9 -> d6 [${E}]; m9 -> d7 [${E}]; m7 -> ex [${E}];
m5 -> bll [ltail=cluster_mod ${E} label="delegan"]; m9 -> fac [ltail=cluster_mod ${E}];`, 'Arquitectura/G06 - Presentacion (clases)');

// ---------- Servicios: seguridad y localización ----------
const S = (id, k, st, n, m) => cls(id, k, st, n, m);
await dot(`rankdir=LR; nodesep=0.35; ranksep=0.9;
${S('ses', 'svc', 'singleton', 'SesionActual', ['- _instancia : SesionActual', '- _candado : object  (lock)', '+ IniciarUsuarioAutenticado(usuario)', '+ Cerrar() / TienePermiso(permiso)'])}
${S('sf', 'svc', 'Facade static', 'SeguridadService', ['+ Autenticar(usuario, contraseña)', '+ TienePermiso(idUsuario, permiso)', '+ CambiarEstadoUsuario(...)', '+ RecuperarContraseña(...)', '+ GuardarPermisosDePerfil(perfil, codigos)'])}
${S('sl', 'logic', 'static', 'SeguridadLogic', ['+ Autenticar(usuario, contraseña)', '+ ObtenerPermisos(...) : PermisoCompuesto', '+ HasheoPassword(texto)'])}
${S('cs', 'svc', 'Facade static', 'CryptographyService', ['+ Encriptar / Desencriptar', '+ Hashear / VerificarHash'])}
${S('cl', 'logic', 'static', 'CryptographyLogic', ['(AES-CBC 256 bits · PBKDF2-SHA256)'])}
${S('ck', 'infra', 'static', 'CryptoKeyProvider', ['+ ObtenerClave() : byte[]  (DPAPI)'])}
${S('pm', 'ent', 'abstract', 'Permiso', ['# Codigo, Nombre : string', '# Tipo : char', '+ Incluye(otro) : bool'])}
${S('pa', 'ent', 'hoja', 'PermisoAtomico', ['+ Incluye(otro) : bool'])}
${S('pc', 'ent', 'compuesto', 'PermisoCompuesto', ['- Hijos : List<Permiso>', '+ Incluye(otro) : bool'])}
${S('us', 'ent', 'dominio', 'Usuario / UsuarioAutenticado / UsuarioListado', ['(identidad, perfil, estado)'])}
${S('lf', 'svc', 'Facade static (observer)', 'LocalizationService', ['+ ObtenerTexto(clave)', '+ SuscribirCambioIdioma(accion) / Desuscribir(accion)', '+ RegistrarIdioma / EliminarIdioma(codigo)'])}
${S('ll', 'logic', 'static', 'LocalizationLogic', ['(diccionario del idioma activo en memoria)'])}
${S('id', 'ent', 'dominio', 'Idioma / TextoLocalizacion', ['Codigo, Nombre, Activo · Clave, Valor'])}
${S('r1', 'infra', 'ADO.NET', 'SqlUsuarioRepository', [])} ${S('r2', 'infra', 'ADO.NET', 'SqlPermisoRepository', [])} ${S('r3', 'infra', 'ADO.NET', 'SqlLocalizacionRepository', [])}
sf -> sl [${E} label="delega"]; ses -> sf [${E} label="consulta permisos" dir=back]; sl -> cl [${E} label="hash"]; cs -> cl [${E}]; cl -> ck [${E} label="clave"];
sl -> r1 [${E}]; sl -> r2 [${E}]; sl -> pm [${E} label="árbol"]; sl -> us [${E}];
pm -> pa [arrowhead=none arrowtail=empty dir=back]; pm -> pc [arrowhead=none arrowtail=empty dir=back]; pc -> pm [arrowhead=odiamond taillabel="*" label="hijos" constraint=false];
lf -> ll [${E} label="delega"]; ll -> r3 [${E}]; ll -> id [${E}];`, 'Arquitectura/G06 - Servicios seguridad y localizacion (clases)');

// ---------- DER hoja B ----------
const T = (id, name, rows) => tbl(id, name, rows);
await dot(`rankdir=LR; nodesep=0.5; ranksep=1.3; splines=true;
${T('p', 'Pacientes', ['PK Id : int', 'NombreCompleto : nvarchar', 'DNI : nvarchar (UQ activos)', 'FK IdObraSocial : int', 'FK IdUsuarioPortal : int', 'Estado, MotivoBaja, FechaAlta, FechaBaja', 'DVH : nvarchar'])}
${T('u', 'Usuarios', ['PK Id : int', 'NombreUsuario, NombreCompleto', 'Perfil, Email, Telefono', 'Activo : bit', 'FechaAlta : datetime2', 'DVH : nvarchar'])}
${T('t', 'Turnos', ['PK Id : int', 'FK IdPaciente : int', 'FK IdUsuarioMedico : int', 'FechaHora : datetime2', 'Estado, Observaciones, FechaCreacion', 'DVH : nvarchar'])}
${T('s', 'Seguimientos', ['PK Id : int', 'FK IdPaciente : int', 'TipoContacto, Resultado, Observaciones', 'FK IdUsuarioRegistro : int', 'DecisionClinica, DetalleDecision', 'FK IdUsuarioDecision : int', 'FechaRegistro, FechaDecision', 'DVH : nvarchar'])}
${T('e', 'EventosAdversos', ['PK Id : int', 'FK IdPaciente : int', 'Fecha, Tipo, Gravedad', 'Descripcion, AccionTomada', 'FK IdUsuarioRegistro : int', 'FechaRegistro : datetime2', 'DVH : nvarchar'])}
subgraph cluster_ctl { margin=22; label="Tablas de control y reportes (sin FK)"; style=dashed; color="#999999"; fontname="Arial"; fontsize=11;
${T('r', 'ReportesEstadisticos', ['PK Id : int', 'Anio, Mes : int', 'FechaGeneracion : datetime2', 'IndicadorGlobalEficacia : decimal', 'CantidadOptimos / Suboptimos / Deficientes', 'DetalleJson : nvarchar', 'DVH : nvarchar'])}
${T('dv', 'DigitosVerificadores', ['PK Id : int', 'NombreTabla : nvarchar', 'DVV : nvarchar', 'FechaCalculo : datetime2'])}
${T('a', 'AuditoriaCambios', ['PK Id : int', 'Entidad, IdRegistro', 'FechaCambio : datetime2', 'Usuario, TipoCambio', 'DatosAnteriores / DatosNuevos : JSON'])} }
p -> t [dir=both arrowtail=teetee arrowhead=crowodot label="agenda"];
p -> s [dir=both arrowtail=teetee arrowhead=crowodot label="sigue"];
p -> e [dir=both arrowtail=teetee arrowhead=crowodot label="sufre"];
u -> p [dir=both arrowtail=teeodot arrowhead=teeodot label="portal"];
u -> t [dir=both arrowtail=teeodot arrowhead=crowodot label="médico"];
u -> s [dir=both arrowtail=teeodot arrowhead=crowodot label="contacta / decide"];
u -> e [dir=both arrowtail=teeodot arrowhead=crowodot label="registra"];`, 'Arquitectura/G07 - DER hoja B (operacion y sistema)');
console.log('arch ok');
