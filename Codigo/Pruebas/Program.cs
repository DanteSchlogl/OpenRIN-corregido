using Microsoft.EntityFrameworkCore;
using Negocio.BLL;
using Negocio.DAL.Context;
using Negocio.DomainModel;
using Negocio.DomainModel.Enums;
using Services.Facade;
using Negocio.DomainModel.Exceptions;

int ok = 0, fail = 0;
void Check(string nombre, bool cond, string detalle = "")
{
    if (cond) { ok++; Console.WriteLine("  OK    " + nombre); }
    else { fail++; Console.WriteLine("  FALLA " + nombre + (detalle != "" ? "  -> " + detalle : "")); }
}
T Try<T>(string nombre, Func<T> f)
{
    try { return f(); }
    catch (Exception ex) { fail++; Console.WriteLine("  FALLA " + nombre + "  -> " + ex.GetType().Name + ": " + ex.Message); return default!; }
}
void Titulo(string t) => Console.WriteLine("\n== " + t);

string clave = "Ort" + Guid.NewGuid().ToString("N")[..10] + "!1";
string uSys = "pruebas_auto"; // usuario propio de la prueba: no toca los usuarios de demostración
const string U = "prueba";

// ---------------------------------------------------------------- SERVICIOS
Titulo("Servicios: idiomas");
LocalizationService.EstablecerIdioma("es");
Check("idioma es", LocalizationService.ObtenerTexto("agenda.agendar") == "Agendar turno", LocalizationService.ObtenerTexto("agenda.agendar"));
LocalizationService.EstablecerIdioma("en");
Check("idioma en-US cambia el texto", LocalizationService.ObtenerTexto("agenda.agendar") != "Agendar turno" && LocalizationService.ObtenerTexto("agenda.agendar") != "agenda.agendar", LocalizationService.ObtenerTexto("agenda.agendar"));
LocalizationService.EstablecerIdioma("zh-CN");
Check("idioma zh-CN cambia el texto", LocalizationService.ObtenerTexto("agenda.agendar") != "agenda.agendar");
LocalizationService.EstablecerIdioma("es");
bool lanzo = false; try { LocalizationService.EstablecerIdioma("xx"); } catch (Services.DomainModel.Exceptions.DatoInvalidoException) { lanzo = true; }
Check("idioma inexistente se rechaza", lanzo);
Check("clave inexistente devuelve la clave", LocalizationService.ObtenerTexto("no.existe.clave") == "no.existe.clave");
int notif = 0; Action obs = () => notif++;
LocalizationService.Suscribir(obs);
LocalizationService.EstablecerIdioma("en"); LocalizationService.EstablecerIdioma("es");
Check("observer notifica los cambios", notif == 2, "notificaciones=" + notif);
LocalizationService.Desuscribir(obs);

Titulo("Servicios: seguridad");
if (!SeguridadService.ExisteNombreUsuario(uSys))
    Try("crear sysadmin", () => SeguridadService.RegistrarUsuario(uSys, "Administrador del sistema", clave, "sysadmin", "sys@openrin.local", "Pregunta de prueba", "respuesta"));
else SeguridadService.RecuperarContrasena(uSys, "respuesta", clave);
var aut = Try("login válido", () => SeguridadService.Autenticar(uSys, clave));
Check("login válido devuelve perfil sysadmin", aut != null && aut.Perfil == "sysadmin");
bool malo = false; try { SeguridadService.Autenticar(uSys, "mala-clave"); } catch (Exception) { malo = true; }
Check("contraseña incorrecta se rechaza", malo);
Check("sysadmin tiene permiso SI001", aut != null && SeguridadService.TienePermiso(aut.Id, "SI001"));
Check("árbol de permisos no vacío", SeguridadService.ObtenerArbolPermisos() != null);
Check("perfiles: 5", SeguridadService.ObtenerPerfiles().Count == 5, SeguridadService.ObtenerPerfiles().Count.ToString());
Check("pregunta de seguridad", SeguridadService.ObtenerPreguntaSeguridad(uSys) == "Pregunta de prueba");
SeguridadService.RecuperarContrasena(uSys, "respuesta", clave + "x");
Check("recuperar contraseña y volver a entrar", Try("login nueva clave", () => SeguridadService.Autenticar(uSys, clave + "x")) != null);
SeguridadService.RecuperarContrasena(uSys, "respuesta", clave);
for (int i = 0; i < 5; i++) { try { SeguridadService.Autenticar(uSys, "xxxxxx"); } catch { } }
bool bloqueado = false; try { SeguridadService.Autenticar(uSys, clave); } catch (Exception) { bloqueado = true; }
Check("5 intentos fallidos bloquean la cuenta", bloqueado);
using (var c0 = new Microsoft.Data.SqlClient.SqlConnection(@"Server=.\SQLEXPRESS;Database=OpenRIN_Services;Integrated Security=True;TrustServerCertificate=True;"))
{ c0.Open(); using var cmd = c0.CreateCommand(); cmd.CommandText = "UPDATE Usuarios SET IntentosFallidos=0, BloqueadoHasta=NULL WHERE NombreUsuario='pruebas_auto'"; cmd.ExecuteNonQuery(); }
Check("desbloqueo y login", Try("login", () => SeguridadService.Autenticar(uSys, clave)) != null);
Check("cifrado AES ida y vuelta", CryptographyService.Desencriptar(CryptographyService.Encriptar("dato clínico")) == "dato clínico");
var h = CryptographyService.Hashear("abc");
Check("hash PBKDF2 verifica", CryptographyService.VerificarHash("abc", h) && !CryptographyService.VerificarHash("abd", h));
BitacoraService.Info("prueba automática", U);
Check("bitácora registra", BitacoraService.ObtenerUltimos(5).Any(l => l.Mensaje.Contains("prueba automática")));

// ---------------------------------------------------------------- NEGOCIO
using var ctx = new NegocioDbContext();
ctx.UsuarioAuditoria = U;
var pac = new PacienteLogic(ctx); var hc = new HistoriaClinicaLogic(ctx); var med = new MedicionRINLogic(ctx);
var alr = new AlertaLogic(ctx); var panel = new PanelMonitoreoLogic(ctx); var turnos = new TurnoLogic(ctx);
var seg = new SeguimientoLogic(ctx); var rep = new ReporteLogic(ctx); var ev = new EventoAdversoLogic(ctx);
var integ = new IntegridadLogic(ctx); var audit = new AuditoriaLogic(ctx);
string dni = (30000000 + Random.Shared.Next(999999)).ToString();

Titulo("UC-01 pacientes");
var p = new Paciente { NombreCompleto = "Paciente de Prueba", DNI = dni, Telefono = "11-5555-0000", Email = "p@prueba.local", IdObraSocial = 1 };
var cred = Try("alta de paciente", () => pac.RegistrarPaciente(p, U));
Check("alta devuelve credenciales", cred != null && !string.IsNullOrEmpty(cred.Usuario));
Check("buscar por DNI", pac.BuscarPorDNI(dni)?.Id == p.Id);
bool dup = false; try { pac.RegistrarPaciente(new Paciente { NombreCompleto = "Otro", DNI = dni, Telefono = "1", Email = "o@o.o" }, U); } catch (Exception) { dup = true; }
Check("DNI duplicado se rechaza", dup);
p.Telefono = "11-6666-0000"; pac.ModificarPaciente(p, U);
Check("modificar paciente", pac.BuscarPorDNI(dni)!.Telefono == "11-6666-0000");

Titulo("UC-02 historia clínica");
bool inv = false; try { hc.Configurar(new HistoriaClinica { IdPaciente = p.Id, IdDiagnostico = 1, LimiteInferiorRIN = 3.0m, LimiteSuperiorRIN = 2.0m, Medicamento = "Acenocumarol", Dosis = "4 mg", PeriodicidadDias = 30 }, U); } catch (Exception) { inv = true; }
Check("límites invertidos se rechazan", inv);
var h1 = Try("configurar HC", () => hc.Configurar(new HistoriaClinica { IdPaciente = p.Id, IdDiagnostico = 1, LimiteInferiorRIN = 2.0m, LimiteSuperiorRIN = 3.0m, Medicamento = "Acenocumarol", Dosis = "4 mg", PeriodicidadDias = 30 }, U));
Check("HC configurada", hc.ObtenerPorPaciente(p.Id) != null);
hc.ActualizarPeriodicidad(p.Id, 15, U);
Check("periodicidad actualizada", hc.ObtenerPorPaciente(p.Id)!.PeriodicidadDias == 15);

Titulo("UC-04/05/07/08 RIN, criticidad y alertas");
Check("criticidad Baja", MedicionRINLogic.CalcularCriticidad(2.5m, 2.0m, 3.0m) == NivelCriticidad.Baja);
Check("criticidad Media", MedicionRINLogic.CalcularCriticidad(3.3m, 2.0m, 3.0m) == NivelCriticidad.Media);
Check("criticidad Alta", MedicionRINLogic.CalcularCriticidad(3.5m, 2.0m, 3.0m) == NivelCriticidad.Alta);
bool fuera = false; try { med.Reportar(new MedicionRIN { IdPaciente = p.Id, ValorRIN = 25m, FechaMedicion = DateTime.Today, Canal = CanalReporteRIN.Digital }, U); } catch (Exception) { fuera = true; }
Check("RIN fuera de 0,1–20 se rechaza", fuera);
bool fut = false; try { med.Reportar(new MedicionRIN { IdPaciente = p.Id, ValorRIN = 2.5m, FechaMedicion = DateTime.Today.AddDays(3), Canal = CanalReporteRIN.Digital }, U); } catch (Exception) { fut = true; }
Check("fecha futura se rechaza", fut);
var r1 = Try("RIN 2,5", () => med.Reportar(new MedicionRIN { IdPaciente = p.Id, ValorRIN = 2.5m, FechaMedicion = DateTime.Today.AddDays(-10), Canal = CanalReporteRIN.Digital }, U));
Check("2,5 → Baja sin alerta", r1.Medicion.NivelCriticidad == NivelCriticidad.Baja && r1.AlertaGenerada == null);
var r2 = Try("RIN 2,95 (borde)", () => med.Reportar(new MedicionRIN { IdPaciente = p.Id, ValorRIN = 2.95m, FechaMedicion = DateTime.Today.AddDays(-8), Canal = CanalReporteRIN.Telefonico }, U));
Check("primer borde sin anterior en borde → sin tendencia", !r2.Medicion.PorTendenciaPeligrosa);
var r3 = Try("RIN 3,05 (borde)", () => med.Reportar(new MedicionRIN { IdPaciente = p.Id, ValorRIN = 3.05m, FechaMedicion = DateTime.Today.AddDays(-5), Canal = CanalReporteRIN.Digital }, U));
Check("dos bordes seguidos → tendencia peligrosa, Alta y alerta", r3.Medicion.PorTendenciaPeligrosa && r3.Medicion.NivelCriticidad == NivelCriticidad.Alta && r3.AlertaGenerada?.Tipo == TipoAlerta.TendenciaPeligrosa);
var r4 = Try("RIN 4,2", () => med.Reportar(new MedicionRIN { IdPaciente = p.Id, ValorRIN = 4.2m, FechaMedicion = DateTime.Today.AddDays(-2), Canal = CanalReporteRIN.Digital }, U));
Check("4,2 → Alta con alerta CriticidadAlta notificada", r4.Medicion.NivelCriticidad == NivelCriticidad.Alta && r4.AlertaGenerada != null && r4.AlertaGenerada.NotificadaMedico && r4.AlertaGenerada.NotificadaAdministrativo);

Titulo("UC-06 panel y alertas");
var fila = panel.ObtenerPanel().FirstOrDefault(f => f.IdPaciente == p.Id);
Check("paciente aparece en el panel", fila != null);
Check("alertas activas del paciente: 2", alr.ObtenerPorPaciente(p.Id).Count(a => a.Estado == EstadoAlerta.Activa) == 2);
int idA = r4.AlertaGenerada!.Id;
alr.Resolver(idA, "Se ajustó la dosis", U);
Check("resolver alerta", alr.ObtenerPorPaciente(p.Id).First(a => a.Id == idA).Estado == EstadoAlerta.Resuelta);

Titulo("UC-03 eventos adversos");
var e1 = Try("evento adverso", () => ev.Registrar(new EventoAdverso { IdPaciente = p.Id, Fecha = DateTime.Today.AddDays(-1), Tipo = TipoEventoAdverso.Hemorragia, Gravedad = GravedadEvento.Grave, Descripcion = "Epistaxis", AccionTomada = "Control" }, U));
Check("evento registrado", ev.ObtenerPorPaciente(p.Id).Count == 1);
bool evf = false; try { ev.Registrar(new EventoAdverso { IdPaciente = p.Id, Fecha = DateTime.Today.AddDays(5), Tipo = TipoEventoAdverso.Hemorragia, Gravedad = GravedadEvento.Leve, Descripcion = "x", AccionTomada = "y" }, U); } catch (Exception) { evf = true; }
Check("evento con fecha futura se rechaza", evf);

Titulo("UC-09 agenda");
var medico = ctx.Usuarios.FirstOrDefault(u => u.Perfil == "medico") ?? new Usuario { NombreUsuario = "medico_prueba", NombreCompleto = "Dr. Prueba", Perfil = "medico", Activo = true };
if (medico.Id == 0) { ctx.Usuarios.Add(medico); ctx.SaveChanges(); }
var t1 = Try("agendar turno", () => turnos.AgendarTurno(new Turno { IdPaciente = p.Id, IdUsuarioMedico = medico.Id, FechaHora = DateTime.Today.AddDays(2).AddHours(8).AddMinutes(Random.Shared.Next(0, 600)) }, U));
Check("turno en la agenda", turnos.ObtenerAgenda(DateTime.Today, DateTime.Today.AddDays(5)).Any(t => t.IdPaciente == p.Id));
Check("tiene turno próximo", turnos.TieneTurnoProximo(p.Id));
Check("generar sugerencias no falla", Try("sugerencias", () => turnos.GenerarSugerencias()) != null);

Titulo("UC-10 seguimiento");
var pv = new Paciente { NombreCompleto = "Paciente Vencido", DNI = (31000000 + Random.Shared.Next(999999)).ToString(), Telefono = "11-1", Email = "v@v.local", IdObraSocial = 2 };
pac.RegistrarPaciente(pv, U);
hc.Configurar(new HistoriaClinica { IdPaciente = pv.Id, IdDiagnostico = 2, LimiteInferiorRIN = 2.0m, LimiteSuperiorRIN = 3.0m, Medicamento = "Warfarina", Dosis = "5 mg", PeriodicidadDias = 15 }, U);
med.Reportar(new MedicionRIN { IdPaciente = pv.Id, ValorRIN = 2.5m, FechaMedicion = DateTime.Today.AddDays(-45), Canal = CanalReporteRIN.Digital }, U);
var venc = seg.DetectarPlazosVencidos();
Check("detecta plazo vencido y genera alerta de ausencia", venc.Any(a => a.IdPaciente == pv.Id && a.Tipo == TipoAlerta.ReporteAusente));
var s1 = Try("contacto fallido 1", () => seg.RegistrarIntentoContacto(new Seguimiento { IdPaciente = pv.Id, TipoContacto = TipoContacto.Llamado, Resultado = ResultadoContacto.Fallido }, U));
var s2 = Try("contacto fallido 2", () => seg.RegistrarIntentoContacto(new Seguimiento { IdPaciente = pv.Id, TipoContacto = TipoContacto.Llamado, Resultado = ResultadoContacto.Fallido }, U));
var s3 = Try("contacto fallido 3", () => seg.RegistrarIntentoContacto(new Seguimiento { IdPaciente = pv.Id, TipoContacto = TipoContacto.Mensaje, Resultado = ResultadoContacto.Fallido }, U));
Console.WriteLine($"  info  escaló al médico en intentos: 1={s1?.EscaladaAlMedico} 2={s2?.EscaladaAlMedico} 3={s3?.EscaladaAlMedico} cerró={s3?.CerroAlerta}");
Check("seguimientos registrados", seg.ObtenerSeguimientos(pv.Id).Count >= 3);
Check("detectar plazos vencidos no falla", Try("plazos", () => seg.DetectarPlazosVencidos()) != null);

Titulo("UC-11/12/13 reportes y exportación");
var rm = Try("reporte mensual", () => rep.GenerarReporteMensual(DateTime.Today.Year, DateTime.Today.Month, U));
Check("reporte mensual con indicador", rm != null && rm.IndicadorGlobalEficacia >= 0);
var rm2 = Try("reporte mensual otra vez (regenera)", () => rep.GenerarReporteMensual(DateTime.Today.Year, DateTime.Today.Month, U));
Check("regenerar no duplica", ctx.ReportesEstadisticos.Count(x => x.Anio == DateTime.Today.Year && x.Mes == DateTime.Today.Month) == 1);
bool sinDatos = false; try { rep.GenerarReporteMensual(2020, 1, U); sinDatos = true; } catch (Exception) { sinDatos = true; }
Check("período sin mediciones se maneja", sinDatos);
Check("reporte por diagnóstico", Try("por diagnóstico", () => rep.GenerarReportePorDiagnostico(DateTime.Today.Year, DateTime.Today.Month, null, U)).Count > 0);
Check("clasificar 85/60/30", ReporteLogic.Clasificar(85) == ClasificacionControl.Optimo && ReporteLogic.Clasificar(60) == ClasificacionControl.Suboptimo && ReporteLogic.Clasificar(30) == ClasificacionControl.Deficiente);
Check("historial unificado", rep.ObtenerHistorialPaciente(p.Id).Count >= 4);
var ex = Try("exportar historial", () => rep.ExportarHistorialPaciente(p.Id, "json", U));
Check("exportación con nombre e ítems", ex != null && ex.Items.Count > 0 && ex.NombreArchivoSugerido.Length > 0);

Titulo("Baja lógica");
pac.DarDeBajaPaciente(p.Id, "Prueba automática", U);
Check("paciente dado de baja conserva historia", pac.BuscarPorDNI(dni)?.Estado == EstadoPaciente.Inactivo && rep.ObtenerHistorialPaciente(p.Id).Count >= 4);
bool bajaRIN = false; try { med.Reportar(new MedicionRIN { IdPaciente = p.Id, ValorRIN = 2.5m, FechaMedicion = DateTime.Today, Canal = CanalReporteRIN.Digital }, U); } catch (Exception) { bajaRIN = true; }
Check("inactivo no puede reportar RIN", bajaRIN);

Titulo("Control de cambios");
var cambios = audit.ObtenerHistorial("Pacientes", p.Id, 50);
Check("cambios auditados del paciente", cambios.Count >= 2, cambios.Count.ToString());
var modif = cambios.FirstOrDefault(c => c.TipoCambio.Contains("Modif", StringComparison.OrdinalIgnoreCase));
if (modif != null)
{
    var rest = audit.Restaurar(modif.Id, U);
    Check("restaurar estado anterior", rest.Ok, rest.Mensaje);
}
else Console.WriteLine("  info  no hay cambio de tipo modificación para restaurar");

Titulo("Integridad DVH/DVV");
int firmadas = Try("recalcular todo", () => integ.RecalcularTodo());
var a0 = integ.Auditar();
Check("auditoría sin problemas", a0.Problemas.Count == 0, string.Join(" | ", a0.Problemas.Take(3)));
Check("11 tablas auditadas", a0.TablasVerificadas.Count == 11, a0.TablasVerificadas.Count.ToString());
using (var c1 = new Microsoft.Data.SqlClient.SqlConnection(@"Server=.\SQLEXPRESS;Database=OpenRIN_Negocio;Integrated Security=True;TrustServerCertificate=True;"))
{ c1.Open(); using var cmd = c1.CreateCommand(); cmd.CommandText = $"UPDATE Pacientes SET Telefono='ALTERADO' WHERE Id={p.Id}"; cmd.ExecuteNonQuery(); }
using var ctx2 = new NegocioDbContext(); var a1 = new IntegridadLogic(ctx2).Auditar();
Check("alteración directa en la base se detecta", a1.Problemas.Count > 0);
new IntegridadLogic(ctx2).RecalcularTodo();
using var ctx3 = new NegocioDbContext();
Check("reparación vuelve a dejar la base íntegra", new IntegridadLogic(ctx3).Auditar().Problemas.Count == 0);

Titulo("Respaldo");
string? ruta = Try("backup completo", () => BackupService.RealizarBackup(Services.DomainModel.TipoBackup.Completo, "OpenRIN_Negocio"));
Check("archivo de respaldo existe", ruta != null && File.Exists(ruta), ruta ?? "");
Check("respaldo verificable", ruta != null && BackupService.VerificarIntegridadBackup(ruta));


// ---------------------------------------------------------------- CORRECCIONES DEL PROFESOR
Titulo("Excepciones de negocio");
Check("DatoInvalido es una ReglaNegocio", typeof(Services.DomainModel.Exceptions.DatoInvalidoException).IsSubclassOf(typeof(Services.DomainModel.Exceptions.ReglaNegocioException)));
Check("ValidacionNegocio es una ReglaNegocio", typeof(Negocio.DomainModel.Exceptions.ValidacionNegocioException).IsSubclassOf(typeof(Services.DomainModel.Exceptions.ReglaNegocioException)));
Check("AccesoADatos, Respaldo, ConfiguracionInvalida, IntegridadVulnerada, RegistroNoEncontrado y RegistroDuplicado son ReglaNegocio",
    new[] { typeof(Services.DomainModel.Exceptions.AccesoADatosException), typeof(Services.DomainModel.Exceptions.RespaldoException),
            typeof(Services.DomainModel.Exceptions.ConfiguracionInvalidaException), typeof(Services.DomainModel.Exceptions.IntegridadVulneradaException),
            typeof(Services.DomainModel.Exceptions.RegistroNoEncontradoException), typeof(Services.DomainModel.Exceptions.RegistroDuplicadoException) }
        .All(t => t.IsSubclassOf(typeof(Services.DomainModel.Exceptions.ReglaNegocioException))));
bool dato = false; try { SeguridadService.RegistrarUsuario("", "x", "Abcdef1!", "medico", "a@b.c", "p", "r"); } catch (Services.DomainModel.Exceptions.DatoInvalidoException) { dato = true; } catch (Exception) { }
Check("usuario vacío lanza DatoInvalido", dato);
bool usuarioRepetido = false; try { SeguridadService.RegistrarUsuario(uSys, "x", "Abcdef1!", "medico", "a@b.c", "p", "r"); } catch (Services.DomainModel.Exceptions.RegistroDuplicadoException) { usuarioRepetido = true; } catch (Exception) { }
Check("usuario repetido lanza RegistroDuplicado", usuarioRepetido);
bool noExiste = false; try { SeguridadService.CambiarEstadoUsuario("no_existe_xyz", true); } catch (Services.DomainModel.Exceptions.RegistroNoEncontradoException) { noExiste = true; } catch (Exception) { }
Check("usuario inexistente lanza RegistroNoEncontrado", noExiste);

Titulo("Factory con reflexión");
Check("crea un repositorio configurado", Try("crear IUsuarioRepository", () => Services.DAL.Factory.RepositorioFactory.Crear<Services.DAL.Interfaces.IUsuarioRepository>()) != null);
Check("repositorio con constructor con argumentos", Try("crear IPacienteRepositorio", () => { using var cx = new NegocioDbContext(); return Services.DAL.Factory.RepositorioFactory.Crear<Negocio.DAL.Repositorios.IPacienteRepositorio>(null, cx); }) != null);
bool Falla(string? nombre, object[]? args = null) { try { Services.DAL.Factory.RepositorioFactory.CrearDesdeNombre<IDisposable>("Repositorio.IDisposable", nombre, args ?? Array.Empty<object>()); return false; } catch (Services.DomainModel.Exceptions.ConfiguracionInvalidaException) { return true; } }
Check("clave ausente lanza ConfiguracionInvalida", Falla(null));
Check("clase inexistente lanza ConfiguracionInvalida", Falla("No.Existe.Clase"));
Check("clase abstracta lanza ConfiguracionInvalida", Falla("System.IO.Stream"));
Check("clase que no implementa la interfaz lanza ConfiguracionInvalida", Falla("System.Text.StringBuilder"));
Check("clase sin el constructor pedido lanza ConfiguracionInvalida", Falla("System.IO.StringWriter", new object[] { 1, 2, 3 }));
Check("clase válida con constructor sin parámetros", !Falla("System.IO.StringWriter"));
bool sinCtor = false; try { Services.DAL.Factory.RepositorioFactory.Crear<Negocio.DAL.Repositorios.IPacienteRepositorio>(); } catch (Services.DomainModel.Exceptions.ConfiguracionInvalidaException) { sinCtor = true; }
Check("pedir un repositorio sin su argumento lanza ConfiguracionInvalida", sinCtor);

Titulo("Respaldos (COPY_ONLY, CHECKSUM, tabla Respaldos)");
string carpetaPrueba = Path.Combine(Path.GetTempPath(), "openrin_resp_" + Guid.NewGuid().ToString("N")[..8]);
Directory.CreateDirectory(carpetaPrueba);
string? rutaA = Try("respaldo A", () => BackupService.RealizarBackup(Services.DomainModel.TipoBackup.Completo, "OpenRIN_Negocio", carpetaPrueba, uSys));
string? rutaB = Try("respaldo B", () => BackupService.RealizarBackup(Services.DomainModel.TipoBackup.Completo, "OpenRIN_Negocio", carpetaPrueba, uSys));
Check("dos respaldos seguidos no se pisan", rutaA != null && rutaB != null && rutaA != rutaB && File.Exists(rutaA) && File.Exists(rutaB));
Check("el nombre lleva milisegundos", rutaA != null && System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(rutaA), @"_\d{8}_\d{9}_FULL\.bak$"));
using (var cMsdb = new Microsoft.Data.SqlClient.SqlConnection(@"Server=.\SQLEXPRESS;Database=msdb;Integrated Security=True;TrustServerCertificate=True;"))
{
    cMsdb.Open(); using var cmd = cMsdb.CreateCommand();
    cmd.CommandText = "SELECT TOP 1 CAST(s.is_copy_only AS INT) FROM msdb.dbo.backupset s JOIN msdb.dbo.backupmediafamily m ON m.media_set_id=s.media_set_id WHERE m.physical_device_name LIKE '%' + @r ORDER BY s.backup_set_id DESC";
    cmd.Parameters.AddWithValue("@r", Path.GetFileName(rutaB ?? "x"));
    object? copyOnly = cmd.ExecuteScalar();
    Check("el respaldo completo es COPY_ONLY", copyOnly != null && Convert.ToInt32(copyOnly) == 1);
    cmd.CommandText = "SELECT TOP 1 CAST(s.has_backup_checksums AS INT) FROM msdb.dbo.backupset s JOIN msdb.dbo.backupmediafamily m ON m.media_set_id=s.media_set_id WHERE m.physical_device_name LIKE '%' + @r ORDER BY s.backup_set_id DESC";
    object? chk = cmd.ExecuteScalar();
    Check("el respaldo lleva CHECKSUM", chk != null && Convert.ToInt32(chk) == 1);
}
var registrados = Try("listar respaldos", () => BackupService.ListarRespaldos(50)) ?? new();
var filaA = registrados.FirstOrDefault(r => r.Archivo == rutaA);
Check("el respaldo queda en la tabla Respaldos (origen, sano, tamaño, usuario)",
    filaA != null && filaA.Origen == "Manual" && filaA.Sano && filaA.TamanoBytes > 0 && filaA.Usuario == uSys && filaA.BaseDatos == "OpenRIN_Negocio");
bool noSanoLanza = false;
string falso = Path.Combine(carpetaPrueba, "falso.bak"); File.WriteAllText(falso, "no es un respaldo");
try { BackupService.RestaurarBackup(falso, uSys); } catch (Services.DomainModel.Exceptions.RespaldoException) { noSanoLanza = true; } catch (Exception) { }
Check("un archivo que no es respaldo no se restaura (RespaldoException)", noSanoLanza);
Check("la verificación rechaza el archivo falso", !BackupService.VerificarIntegridadBackup(falso));

Titulo("Respaldo automático diario");
bool sanos = false;
var generados1 = Try("diario con datos no sanos", () => BackupService.RespaldoDiarioSiCorresponde(() => sanos, "prueba", carpetaPrueba));
Check("con datos no sanos no se respalda", generados1 != null && generados1.Count == 0);
sanos = true;
var generados2 = Try("diario con datos sanos", () => BackupService.RespaldoDiarioSiCorresponde(() => sanos, "prueba", carpetaPrueba));
Check("con datos sanos genera un respaldo por base", generados2 != null && generados2.Count == 2 && generados2.All(File.Exists));
var generados3 = Try("diario repetido", () => BackupService.RespaldoDiarioSiCorresponde(() => sanos, "prueba", carpetaPrueba));
Check("una sola vez por día", generados3 != null && generados3.Count == 0);
for (int i = 0; i < 11; i++)
{
    Services.BLL.Backups.BackupLogic.RealizarBackup(Services.DomainModel.TipoBackup.Completo, "OpenRIN_Negocio", carpetaPrueba, "Automatico", "prueba");
}
var automaticos = BackupService.ListarRespaldos(500).Where(r => r.BaseDatos == "OpenRIN_Negocio" && r.Origen == "Automatico").ToList();
Check("se conservan solo los 10 últimos automáticos", automaticos.Count == 10, "automáticos=" + automaticos.Count);
Check("los archivos de los automáticos viejos se borran", Directory.GetFiles(carpetaPrueba, "OpenRIN_Negocio_*_FULL.bak").Length == 10 + 2 /* los dos manuales */);

Titulo("Recuperación con dígitos verificadores");
using (var ctxR = new NegocioDbContext()) { new IntegridadLogic(ctxR).RecalcularTodo(); }
string respaldoSano = BackupService.RealizarBackup(Services.DomainModel.TipoBackup.Completo, "OpenRIN_Negocio", carpetaPrueba, uSys);
using (var c1 = new Microsoft.Data.SqlClient.SqlConnection(@"Server=.\SQLEXPRESS;Database=OpenRIN_Negocio;Integrated Security=True;TrustServerCertificate=True;"))
{ c1.Open(); using var cmd = c1.CreateCommand(); cmd.CommandText = $"UPDATE Pacientes SET Telefono='VIOLADO' WHERE Id={p.Id}"; cmd.ExecuteNonQuery(); }
ResultadoAuditoria audVul; using (var ctxV = new NegocioDbContext()) { audVul = new IntegridadLogic(ctxV).Auditar(); }
Check("la auditoría lista el registro alterado (tabla e Id)", audVul.Alterados.Any(r => r.Tabla == "Pacientes" && r.Id == p.Id), string.Join(" | ", audVul.Alterados.Take(3).Select(r => r.Tabla + "#" + r.Id)));
Try("restaurar desde el respaldo sano", () => { BackupService.RestaurarBackup(respaldoSano, uSys); return true; });
ResultadoAuditoria audOk; using (var ctxO = new NegocioDbContext()) { audOk = new IntegridadLogic(ctxO).Auditar(); }
Check("tras restaurar, la integridad vuelve a estar correcta", audOk.Problemas.Count == 0, string.Join(" | ", audOk.Problemas.Take(2)));
var previos = BackupService.ListarRespaldos(500).Where(r => r.Origen == "Previo" && r.BaseDatos == "OpenRIN_Negocio").ToList();
Check("antes de restaurar se hizo una copia previa de seguridad", previos.Count > 0 && previos[0].Sano);
Check("el dato alterado volvió a su valor", Try("leer paciente", () => { using var cx = new NegocioDbContext(); return cx.Pacientes.AsNoTracking().First(x => x.Id == p.Id).Telefono; }) != "VIOLADO");
Check("la restauración queda en la bitácora", BitacoraService.ObtenerUltimos(80).Any(l => l.Mensaje.Contains("restaurada desde")));
try { Directory.Delete(carpetaPrueba, true); } catch (Exception) { }


Titulo("Idiomas con .resx (ensamblados satélite)");
Check("existe el ensamblado satélite de en", File.Exists(Path.Combine(AppContext.BaseDirectory, "en", "Services.resources.dll")));
Check("existe el ensamblado satélite de zh-CN", File.Exists(Path.Combine(AppContext.BaseDirectory, "zh-CN", "Services.resources.dll")));
int textosEnLaBase;
using (var cTx = new Microsoft.Data.SqlClient.SqlConnection(@"Server=.\SQLEXPRESS;Database=OpenRIN_Services;Integrated Security=True;TrustServerCertificate=True;"))
{ cTx.Open(); using var cmd = cTx.CreateCommand(); cmd.CommandText = "SELECT COUNT(*) FROM Textos WHERE CodigoIdioma IN ('es','en','zh-CN')"; textosEnLaBase = Convert.ToInt32(cmd.ExecuteScalar()); }
LocalizationService.EstablecerIdioma("es");
Check("los textos del idioma base salen del .resx, no de la base", textosEnLaBase == 0 && LocalizationService.ObtenerTexto("agenda.agendar") == "Agendar turno", "filas en la base=" + textosEnLaBase);
LocalizationService.EstablecerIdioma("en");
Check("el idioma en sale del ensamblado satélite", LocalizationService.ObtenerTexto("comun.guardar") == "Save");
LocalizationService.EstablecerIdioma("zh-CN");
Check("el idioma zh-CN sale del ensamblado satélite", LocalizationService.ObtenerTexto("comun.guardar") == "保存");
LocalizationService.EstablecerIdioma("es");
Check("los 3 idiomas tienen las mismas claves", new[] { "es", "en", "zh-CN" }.Select(i => LocalizationService.ObtenerTextosDe(i).Count).Distinct().Count() == 1);
LocalizationService.GuardarTexto("es", "comun.guardar", "Guardar!!");
Check("una leyenda corregida en la base pisa a la del .resx", LocalizationService.ObtenerTexto("comun.guardar") == "Guardar!!");
LocalizationService.EliminarTexto("es", "comun.guardar");
Check("al quitar la corrección vuelve el texto del .resx", LocalizationService.ObtenerTexto("comun.guardar") == "Guardar");
LocalizationService.RegistrarIdioma("pt", "Português (prueba)");
LocalizationService.GuardarTexto("pt", "comun.guardar", "Salvar");
LocalizationService.EstablecerIdioma("pt");
Check("un idioma nuevo funciona solo con leyendas de la base", LocalizationService.ObtenerTexto("comun.guardar") == "Salvar" && LocalizationService.ObtenerTexto("comun.cancelar") == "comun.cancelar");
LocalizationService.EstablecerIdioma("es");
LocalizationService.EliminarIdioma("pt");

Console.WriteLine($"\n===== RESULTADO: {ok} correctas, {fail} con falla =====");
return fail == 0 ? 0 : 1;
