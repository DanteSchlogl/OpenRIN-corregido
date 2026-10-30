// OpenRIN - herramientas de línea de comandos (A01 Instalador y mantenimiento).
//   OpenRIN.Herramientas.exe sembrar [contraseña-demo]   crea los usuarios y los datos de demostración (idempotente)
//   OpenRIN.Herramientas.exe respaldo                    respaldo completo de ambas bases con verificación
//   OpenRIN.Herramientas.exe integridad                  auditoría de dígitos verificadores (DVH/DVV)
using Negocio.BLL;
using Negocio.DAL.Context;
using Negocio.DomainModel;
using Negocio.DomainModel.Enums;
using TipoBackup = Services.DomainModel.TipoBackup;
using Services.Facade;

string comando = args.Length > 0 ? args[0].ToLowerInvariant() : "sembrar";
if (comando == "respaldo")
{
    var archivos = BackupService.RealizarBackupDeLaPlataforma(TipoBackup.Completo);
    bool todosValidos = archivos.All(BackupService.VerificarIntegridadBackup);
    foreach (string a in archivos) Console.WriteLine("respaldo: " + a);
    return todosValidos ? 0 : 3;
}
if (comando == "integridad")
{
    using var c = new NegocioDbContext();
    var r = new IntegridadLogic(c).Auditar();
    Console.WriteLine(r.Problemas.Count == 0 ? "Integridad OK (" + r.TablasVerificadas.Count + " tablas)" : string.Join(Environment.NewLine, r.Problemas));
    return r.Problemas.Count == 0 ? 0 : 2;
}

string clave = args.Length > 1 ? args[1] : "Demo2026!";
const string Quien = "instalador";

Console.WriteLine("OpenRIN - datos de demostración");

// ---- usuarios del sistema (base de servicios) ----
(string usuario, string nombre, string perfil)[] usuarios =
{
    ("sysadmin", "Administrador del sistema", "sysadmin"),
    ("medico", "Dra. Virginia Cortés (demo)", "medico"),
    ("recepcion", "Recepción del consultorio (demo)", "administrativo"),
};
foreach (var (u, n, p) in usuarios)
{
    if (!SeguridadService.ExisteNombreUsuario(u))
    {
        SeguridadService.RegistrarUsuario(u, n, clave, p, u + "@openrin.local", "¿Nombre del consultorio?", "openrin");
        Console.WriteLine("  usuario creado: " + u + " (" + p + ")");
    }
    else
    {
        SeguridadService.RecuperarContrasena(u, "openrin", clave);
        Console.WriteLine("  usuario existente, contraseña restablecida: " + u);
    }
}

using var ctx = new NegocioDbContext { UsuarioAuditoria = Quien };

// El médico también existe en la base de negocio (turnos, seguimientos).
Usuario? medico = ctx.Usuarios.FirstOrDefault(x => x.NombreUsuario == "medico");
if (medico == null)
{
    medico = new Usuario { NombreUsuario = "medico", NombreCompleto = "Dra. Virginia Cortés (demo)", Perfil = "medico", Activo = true };
    ctx.Usuarios.Add(medico);
    ctx.SaveChanges();
}

if (ctx.Pacientes.Any())
{
    Console.WriteLine("  ya hay pacientes cargados: no se agregan datos de ejemplo.");
}
else
{
    var pac = new PacienteLogic(ctx);
    var hc = new HistoriaClinicaLogic(ctx);
    var med = new MedicionRINLogic(ctx);
    var tur = new TurnoLogic(ctx);
    var ev = new EventoAdversoLogic(ctx);
    var seg = new SeguimientoLogic(ctx);
    DateTime hoy = DateTime.Today;

    // (nombre, dni, tel, diagnóstico, límites, medicación, periodicidad, [(días atrás, RIN)])
    var datos = new (string n, string dni, string tel, int dx, decimal li, decimal ls, string far, string dosis, int per, (int d, decimal v)[] rin)[]
    {
        ("Elena Ruiz",      "31666777", "11-4455-1003", 1, 2.0m, 3.0m, "Acenocumarol", "4 mg",  30, new[] { (60, 2.4m), (45, 2.5m), (31, 2.4m) }),
        ("Jorge Pereyra",   "28444555", "11-4455-1002", 4, 2.5m, 3.5m, "Warfarina",    "5 mg",  15, new[] { (20, 3.0m), (10, 3.3m), (3, 4.1m) }),
        ("Ricardo Sosa",    "26999888", "11-4455-1004", 2, 2.0m, 3.0m, "Acenocumarol", "3 mg",  15, new[] { (14, 2.5m), (8, 2.9m), (4, 2.95m), (1, 3.05m) }),
        ("Graciela Núñez",  "25777111", "11-4455-1005", 3, 2.0m, 3.0m, "Warfarina",    "2,5 mg", 15, new[] { (12, 2.6m), (6, 2.8m), (2, 2.5m) }),
        ("Marta Gómez",     "30111222", "11-0000-0000", 5, 2.0m, 3.0m, "Acenocumarol", "4 mg",  30, new[] { (20, 2.4m), (9, 2.5m), (3, 2.5m) }),
    };

    foreach (var d in datos)
    {
        var p = new Paciente { NombreCompleto = d.n, DNI = d.dni, Telefono = d.tel, Email = d.n.Split(' ')[0].ToLower() + "@demo.local", IdObraSocial = 5 };
        pac.RegistrarPaciente(p, Quien);
        hc.Configurar(new HistoriaClinica
        {
            IdPaciente = p.Id, IdDiagnostico = d.dx, LimiteInferiorRIN = d.li, LimiteSuperiorRIN = d.ls,
            Medicamento = d.far, Dosis = d.dosis, PeriodicidadDias = d.per
        }, Quien);
        foreach (var (dias, valor) in d.rin)
        {
            med.Reportar(new MedicionRIN
            {
                IdPaciente = p.Id, ValorRIN = valor, FechaMedicion = hoy.AddDays(-dias),
                Canal = dias % 2 == 0 ? CanalReporteRIN.Digital : CanalReporteRIN.Telefonico
            }, Quien);
        }
        Console.WriteLine("  paciente: " + d.n);
    }

    Paciente graciela = ctx.Pacientes.First(x => x.DNI == "25777111");
    tur.AgendarTurno(new Turno { IdPaciente = graciela.Id, IdUsuarioMedico = medico.Id, FechaHora = hoy.AddDays(3).AddHours(10), Observaciones = "Control de rutina" }, Quien);
    Paciente jorge = ctx.Pacientes.First(x => x.DNI == "28444555");
    ev.Registrar(new EventoAdverso
    {
        IdPaciente = jorge.Id, Fecha = hoy.AddDays(-2), Tipo = TipoEventoAdverso.Hemorragia, Gravedad = GravedadEvento.Leve,
        Descripcion = "Epistaxis leve", AccionTomada = "Se ajustó la dosis según indicación médica"
    }, Quien);
    seg.DetectarPlazosVencidos();
}

new IntegridadLogic(ctx).RecalcularTodo();
var auditoria = new IntegridadLogic(new NegocioDbContext()).Auditar();
Console.WriteLine(auditoria.Problemas.Count == 0
    ? "  integridad DVH/DVV verificada: OK"
    : "  ATENCIÓN: la verificación de integridad informó problemas.");
Console.WriteLine("Listo.");
return auditoria.Problemas.Count == 0 ? 0 : 2;

