import { dot, cls } from './lib.mjs';
const E = 'style=dashed arrowhead=open';

// ---------- Excepciones de negocio ----------
const X = (id, name, m = [], st = '') => cls(id, 'ent', st, name, m);
await dot(`rankdir=RL; nodesep=0.25; ranksep=0.9;
${cls('ex', 'infra', 'System', 'Exception', [])}
${X('rn', 'ReglaNegocioException', ['+ Message : string', '+ InnerException : Exception?'], 'base de las excepciones de negocio')}
${X('di', 'DatoInvalidoException', ['+ Campo : string'])}
${X('rnf', 'RegistroNoEncontradoException', [])}
${X('rd', 'RegistroDuplicadoException', [])}
${X('re', 'RespaldoException', [])}
${X('ci', 'ConfiguracionInvalidaException', [])}
${X('iv', 'IntegridadVulneradaException', ['+ Detalles : IReadOnlyList<string>'])}
${X('vn', 'ValidacionNegocioException', ['+ Errores : IReadOnlyList<string>'], 'Negocio.DomainModel')}
${cls('ad', 'infra', 'técnica', 'AccesoADatosException', ['(fallas de acceso a datos: no se muestran al usuario)'])}
${cls('em', 'logic', 'static', 'ExceptionManagerLogic', ['+ Manejar(excepcion, contexto) : string', '  ReglaNegocioException → muestra Message', '  cualquier otra → texto genérico + bitácora'])}
rn -> ex [arrowhead=empty]; ad -> ex [arrowhead=empty];
di -> rn [arrowhead=empty]; rnf -> rn [arrowhead=empty]; rd -> rn [arrowhead=empty]; re -> rn [arrowhead=empty]; ci -> rn [arrowhead=empty]; iv -> rn [arrowhead=empty]; vn -> rn [arrowhead=empty];
em -> rn [constraint=false ${E} label="distingue"]; em -> ad [${E} constraint=false];`, 'Arquitectura/G06 - Excepciones de negocio (clases)');

// ---------- Fábrica con reflexión ----------
await dot(`rankdir=LR; nodesep=0.4; ranksep=1.0;
${cls('cfg', 'infra', 'App.config', 'appSettings', ['Repositorio.IBackupRepository', '  = ...SqlBackupRepository', 'Repositorio.IRespaldoRegistroRepository', '  = ...SqlRespaldoRegistroRepository', 'Repositorio.ILoggerRepository.Archivo', '  = ...FileLoggerRepository', 'Repositorio.IPacienteRepositorio', '  = Negocio.DAL...PacienteRepositorio'])}
${cls('rf', 'infra', 'public static', 'RepositorioFactory', ['+ Crear<TInterfaz>(alias?, argumentos) : TInterfaz', '  lee la clave Repositorio.<Interfaz>[.alias]', '  Type.GetType + Activator.CreateInstance', '  valida: existe, es concreta,', '  implementa la interfaz y tiene el constructor'])}
${cls('ci', 'ent', '', 'ConfiguracionInvalidaException', ['(clave faltante o clase inválida)'])}
${cls('lg', 'logic', 'static', 'Clases de lógica', ['BackupLogic, BitacoraLogic, SeguridadLogic,', 'LocalizationLogic, PacienteLogic', 'piden sus repositorios a la fábrica', 'y no conocen las clases concretas'])}
${cls('ii', 'infra', 'interface', 'Interfaces de repositorio', ['IBackupRepository, IUsuarioRepository,', 'ILoggerRepository, IPacienteRepositorio ...'])}
${cls('sq', 'ui', 'internal', 'Repositorios concretos', ['SqlBackupRepository, SqlUsuarioRepository,', 'SqlLoggerRepository, PacienteRepositorio ...', '(clases internas: solo la fábrica y', 'las pruebas las ven)'])}
lg -> rf [${E} label="Crear<T>()"]; rf -> cfg [${E} label="lee la clase"]; rf -> ci [${E} label="si falla"];
rf -> ii [${E} label="devuelve"]; sq -> ii [arrowhead=empty style=dashed label="implementa"]; rf -> sq [${E} label="instancia por reflexión" constraint=false];`, 'Arquitectura/G06 - Fabrica con reflexion (clases)');

// ---------- Respaldos ----------
await dot(`rankdir=LR; nodesep=0.35; ranksep=0.9;
${cls('iu', 'ui', 'Form', 'IntegridadVulneradaForm', ['lista registros alterados', 'inicia sesión (permiso SI004)', 'restaura un respaldo sano', 'y vuelve a auditar'])}
${cls('rp', 'ui', 'UserControl', 'RespaldosPanel', ['pestaña Respaldos de', 'Sistema y Bitácora'])}
${cls('bs', 'svc', 'Facade static', 'BackupService', ['+ GenerarBackup / RestaurarBackup', '+ RespaldoDiarioSiCorresponde', '+ ListarRespaldos'])}
${cls('bl', 'logic', 'static', 'BackupLogic', ['+ GenerarBackup(carpeta, usuario, tipo)', '+ RespaldoDiarioSiCorresponde(...)', '+ RestaurarBackup(archivo, usuario)', '  (hace antes una copia "Previo")', '+ ListarRespaldos() / RegistrarImportado()'])}
${cls('re', 'ent', '', 'RespaldoException', ['(respaldo no sano o restauración fallida)'])}
${cls('ib', 'infra', 'interface', 'IBackupRepository', ['+ EjecutarBackup / VerifyOnly', '+ EjecutarRestore / ExisteBase', '+ ObtenerCarpetaDelServidor'])}
${cls('sb', 'ui', 'internal', 'SqlBackupRepository', ['BACKUP ... COPY_ONLY, CHECKSUM', 'RESTORE VERIFYONLY ... CHECKSUM', 'SINGLE_USER / REPLACE'])}
${cls('ir', 'infra', 'interface', 'IRespaldoRegistroRepository', ['+ Registrar(respaldo) / Listar()', '+ EliminarAutomaticosViejos(n)'])}
${cls('sr', 'ui', 'internal', 'SqlRespaldoRegistroRepository', ['tabla Respaldos (OpenRIN_Services)'])}
${cls('rr', 'ent', 'dominio', 'RespaldoRegistro', ['Id, Fecha, BaseDatos, Archivo', 'Origen: Manual | Automatico |', '  Previo | Importado', 'Sano, Usuario, Observaciones'])}
iu -> bs [${E}]; rp -> bs [${E}]; bs -> bl [${E} label="delega"]; bl -> re [${E}];
bl -> ib [${E} label="fábrica"]; bl -> ir [${E} label="fábrica"];
sb -> ib [arrowhead=empty style=dashed label="implementa"]; sr -> ir [arrowhead=empty style=dashed label="implementa"]; ir -> rr [${E}];`, 'Arquitectura/G06 - Respaldos (clases)');
console.log('nuevas ok');
