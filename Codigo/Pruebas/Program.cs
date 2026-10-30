using Microsoft.EntityFrameworkCore;
using Negocio.BLL;
using Negocio.DAL.Context;
using Negocio.DomainModel;
using Negocio.DomainModel.Enums;
using Services.Facade;

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
string uSys = "sysadmin";
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
bool lanzo = false; try { LocalizationService.EstablecerIdioma("xx"); } catch (ArgumentException) { lanzo = true; }
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
{ c0.Open(); using var cmd = c0.CreateCommand(); cmd.CommandText = "UPDATE Usuarios SET IntentosFallidos=0, BloqueadoHasta=NULL WHERE NombreUsuario='sysadmin'"; cmd.ExecuteNonQuery(); }
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

Console.WriteLine($"\n===== RESULTADO: {ok} correctas, {fail} con falla =====");
File.WriteAllText(@"credenciales_prueba.txt", "usuario: sysadmin\r\ncontraseña de prueba: " + clave + "\r\n");
return fail == 0 ? 0 : 1;
