using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading;
using Services.DAL.Implementations;
using Services.DAL.Interfaces;
using Services.DomainModel;

namespace Services.BLL.Backups
{
    /// <summary>
    /// Lógica del módulo de respaldo (REQ-ARQ-005): resuelve carpeta y nombre de archivo,
    /// verifica espacio en disco, ejecuta FULL/DIFFERENTIAL, restaura, verifica integridad
    /// y administra la programación automática diaria (por defecto 3:00 AM).
    /// Cada operación queda registrada en la bitácora.
    /// </summary>
    internal static class BackupLogic
    {
        /// <summary>Bases de datos que componen la plataforma.</summary>
        public static readonly IReadOnlyList<string> BasesDeLaPlataforma =
            new List<string> { "OpenRIN_Negocio", "OpenRIN_Services" }.AsReadOnly();

        private static readonly IBackupRepository Repositorio = new SqlBackupRepository();
        private static readonly object Candado = new object();
        private static System.Threading.Timer? _temporizadorDiario;

        public static string RealizarBackup(TipoBackup tipo, string? nombreBaseDatos = null)
        {
            string nombreBase = ResolverNombreBase(nombreBaseDatos);
            string carpeta = ObtenerCarpetaRespaldos();
            Directory.CreateDirectory(carpeta);

            string sufijo = tipo == TipoBackup.Completo ? "FULL" : "DIFF";
            string ruta = Path.Combine(carpeta, $"{nombreBase}_{DateTime.Now:yyyyMMdd_HHmmss}_{sufijo}.bak");

            VerificarEspacioEnDisco(nombreBase, ruta);

            try
            {
                Repositorio.EjecutarBackup(nombreBase, ruta, tipo);
            }
            catch (Exception ex)
            {
                BitacoraLogic.Registrar(LogLevel.Error, $"Respaldo {sufijo} de '{nombreBase}' FALLÓ.", ex, capa: "Backup");
                throw;
            }

            BitacoraLogic.Registrar(LogLevel.Info, $"Respaldo {sufijo} de '{nombreBase}' generado: {ruta}", capa: "Backup");
            return ruta;
        }

        public static List<string> RealizarBackupDeLaPlataforma(TipoBackup tipo)
        {
            var rutas = new List<string>();
            foreach (string baseDatos in BasesDeLaPlataforma)
            {
                rutas.Add(RealizarBackup(tipo, baseDatos));
            }
            return rutas;
        }

        public static void RestaurarBackup(string rutaArchivo)
        {
            if (string.IsNullOrWhiteSpace(rutaArchivo) || !File.Exists(rutaArchivo))
            {
                throw new InvalidOperationException($"No existe el archivo de respaldo: '{rutaArchivo}'.");
            }

            string nombreBase = Repositorio.LeerNombreBaseDesdeArchivo(rutaArchivo);

            int sesionesEnUso = Repositorio.CantidadSesionesEnBase(nombreBase);
            if (sesionesEnUso > 0)
            {
                BitacoraLogic.Registrar(LogLevel.Warning,
                    $"La base '{nombreBase}' tiene {sesionesEnUso} sesión(es) activa(s); se cerrarán para restaurar.",
                    capa: "Backup");
            }

            try
            {
                Repositorio.EjecutarRestore(nombreBase, rutaArchivo);
            }
            catch (Exception ex)
            {
                BitacoraLogic.Registrar(LogLevel.Error, $"Restauración de '{nombreBase}' FALLÓ.", ex, capa: "Backup");
                throw;
            }

            BitacoraLogic.Registrar(LogLevel.Info, $"Base '{nombreBase}' restaurada desde: {rutaArchivo}", capa: "Backup");
        }

        public static bool VerificarIntegridadBackup(string rutaArchivo)
        {
            try
            {
                Repositorio.EjecutarVerifyOnly(rutaArchivo);
                BitacoraLogic.Registrar(LogLevel.Info, $"Verificación de integridad OK: {rutaArchivo}", capa: "Backup");
                return true;
            }
            catch (Exception ex)
            {
                BitacoraLogic.Registrar(LogLevel.Warning, $"Verificación de integridad FALLÓ: {rutaArchivo}", ex, capa: "Backup");
                return false;
            }
        }

        public static bool ProgramacionActiva => _temporizadorDiario != null;

        public static void ProgramarRespaldoDiario(int horaDelDia = 3)
        {
            if (horaDelDia < 0 || horaDelDia > 23)
            {
                throw new ArgumentOutOfRangeException(nameof(horaDelDia), "La hora debe estar entre 0 y 23.");
            }

            lock (Candado)
            {
                _temporizadorDiario?.Dispose();

                DateTime proximo = DateTime.Today.AddHours(horaDelDia);
                if (proximo <= DateTime.Now)
                {
                    proximo = proximo.AddDays(1);
                }

                _temporizadorDiario = new System.Threading.Timer(
                    _ => EjecutarRespaldoProgramado(), null, proximo - DateTime.Now, TimeSpan.FromDays(1));

                BitacoraLogic.Registrar(LogLevel.Info,
                    $"Respaldo automático diario programado a las {horaDelDia:00}:00 (próximo: {proximo:yyyy-MM-dd HH:mm}).",
                    capa: "Backup");
            }
        }

        public static void DetenerProgramacion()
        {
            lock (Candado)
            {
                _temporizadorDiario?.Dispose();
                _temporizadorDiario = null;
            }

            BitacoraLogic.Registrar(LogLevel.Info, "Programación de respaldo automático detenida.", capa: "Backup");
        }

        private static void EjecutarRespaldoProgramado()
        {
            try
            {
                RealizarBackupDeLaPlataforma(TipoBackup.Completo);
            }
            catch (Exception ex)
            {
                BitacoraLogic.Registrar(LogLevel.Fatal, "El respaldo automático diario FALLÓ.", ex, capa: "Backup");
            }
        }

        private static string ResolverNombreBase(string? nombreBaseDatos)
        {
            if (!string.IsNullOrWhiteSpace(nombreBaseDatos))
            {
                return nombreBaseDatos.Trim();
            }

            string? configurada = ConfigurationManager.AppSettings["BackupDatabase"];
            return string.IsNullOrWhiteSpace(configurada) ? "OpenRIN_Negocio" : configurada.Trim();
        }

        private static string ObtenerCarpetaRespaldos()
        {
            string? configurada = ConfigurationManager.AppSettings["BackupFolder"];
            if (!string.IsNullOrWhiteSpace(configurada))
            {
                return configurada.Trim();
            }

            string raiz = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
            return Path.Combine(raiz, "OpenRIN", "Backups");
        }

        private static void VerificarEspacioEnDisco(string nombreBase, string rutaDestino)
        {
            double tamanoBaseMb = Repositorio.ObtenerTamanoBaseMb(nombreBase);
            double requeridoMb = Math.Max(tamanoBaseMb * 2.0, 100.0); // margen de seguridad

            string raiz = Path.GetPathRoot(Path.GetFullPath(rutaDestino)) ?? "C:\\";
            var unidad = new DriveInfo(raiz);
            double libreMb = unidad.AvailableFreeSpace / 1024.0 / 1024.0;

            if (libreMb < requeridoMb)
            {
                throw new InvalidOperationException(
                    $"Espacio insuficiente en disco para el respaldo de '{nombreBase}'. " +
                    $"Requerido: ~{requeridoMb:0} MB, disponible: {libreMb:0} MB.");
            }
        }
    }
}
