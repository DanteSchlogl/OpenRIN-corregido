using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Services.DAL.Factory;
using Services.DAL.Interfaces;
using Services.DomainModel;
using Services.DomainModel.Exceptions;

namespace Services.BLL.Backups
{
    /// <summary>
    /// Lógica del módulo de respaldo (REQ-ARQ-005): resuelve carpeta y nombre de archivo, verifica espacio en
    /// disco, ejecuta FULL/DIFFERENTIAL con CHECKSUM (el completo COPY_ONLY), verifica cada archivo con
    /// RESTORE VERIFYONLY, lleva el registro en la tabla Respaldos, restaura con copia previa de seguridad y
    /// administra el respaldo automático diario (solo con datos sanos, conservando los últimos 10).
    /// Cada operación queda registrada en la bitácora.
    /// </summary>
    internal static class BackupLogic
    {
        /// <summary>Cantidad de respaldos automáticos que se conservan por base.</summary>
        public const int AutomaticosConservados = 10;

        /// <summary>Bases de datos que componen la plataforma.</summary>
        public static readonly IReadOnlyList<string> BasesDeLaPlataforma =
            new List<string> { "OpenRIN_Negocio", "OpenRIN_Services" }.AsReadOnly();

        private static readonly IBackupRepository Repositorio = RepositorioFactory.Crear<IBackupRepository>();
        private static readonly IRespaldoRegistroRepository Registro = RepositorioFactory.Crear<IRespaldoRegistroRepository>();
        private static readonly object Candado = new object();
        private static System.Threading.Timer? _temporizadorDiario;
        private static Func<bool>? _verificadorDeSalud;

        /// <summary>Define cómo saber si los datos están sanos (la aplicación entrega la verificación de DVH/DVV).</summary>
        public static void EstablecerVerificadorDeSalud(Func<bool>? verificador) => _verificadorDeSalud = verificador;

        public static string RealizarBackup(
            TipoBackup tipo, string? nombreBaseDatos = null, string? carpetaDestino = null,
            string origen = "Manual", string usuario = "")
        {
            string nombreBase = ResolverNombreBase(nombreBaseDatos);
            string carpeta = ObtenerCarpetaRespaldos(carpetaDestino);
            Directory.CreateDirectory(carpeta);

            string sufijo = tipo == TipoBackup.Completo ? "FULL" : "DIFF";
            // Los milisegundos evitan que dos respaldos seguidos se pisen.
            string nombreArchivo = $"{nombreBase}_{DateTime.Now:yyyyMMdd_HHmmssfff}_{sufijo}.bak";
            string ruta = Path.Combine(carpeta, nombreArchivo);
            string rutaFinal = ruta;
            string rutaAVerificar = ruta;
            string observaciones = "";
            string? copiaDelServidor = null;

            VerificarEspacioEnDisco(nombreBase, ruta);

            try
            {
                try
                {
                    Repositorio.EjecutarBackup(nombreBase, ruta, tipo);
                }
                catch (Exception ex) when (Repositorio.EsErrorDeCarpetaSinPermiso(ex))
                {
                    // SQL Server no puede escribir en la carpeta elegida: respalda en la suya y la aplicación copia el archivo.
                    string rutaServidor = Path.Combine(Repositorio.ObtenerCarpetaDelServidor(), nombreArchivo);
                    Repositorio.EjecutarBackup(nombreBase, rutaServidor, tipo);
                    rutaAVerificar = rutaServidor;
                    try
                    {
                        File.Copy(rutaServidor, ruta, overwrite: false);
                        observaciones = "SQL Server no podía escribir en la carpeta elegida: el archivo se copió desde la carpeta del servidor.";
                        copiaDelServidor = rutaServidor;
                    }
                    catch (Exception copia)
                    {
                        rutaFinal = rutaServidor;
                        observaciones = $"No se pudo copiar a la carpeta elegida ({copia.Message}); el archivo quedó en la carpeta del servidor.";
                    }
                }
            }
            catch (Exception ex)
            {
                BitacoraLogic.Registrar(LogLevel.Error, $"Respaldo {sufijo} de '{nombreBase}' FALLÓ.", ex, usuario, capa: "Backup");
                throw new RespaldoException($"No se pudo generar el respaldo de '{nombreBase}'. {ex.Message}", ex);
            }

            bool sano = true;
            try
            {
                Repositorio.EjecutarVerifyOnly(rutaAVerificar);
            }
            catch (Exception ex)
            {
                sano = false;
                observaciones = (observaciones + " No pasó RESTORE VERIFYONLY: " + ex.Message).Trim();
            }

            if (copiaDelServidor != null && sano)
            {
                TryBorrar(copiaDelServidor); // ya verificada y copiada: la copia del servidor sobra
            }

            RegistrarEnTabla(new RespaldoRegistro
            {
                Fecha = DateTime.Now,
                BaseDatos = nombreBase,
                Archivo = rutaFinal,
                TamanoBytes = File.Exists(rutaFinal) ? new FileInfo(rutaFinal).Length : 0,
                Origen = origen,
                Sano = sano,
                Usuario = usuario,
                Observaciones = observaciones.Length > 500 ? observaciones[..500] : observaciones
            });

            if (!sano)
            {
                BitacoraLogic.Registrar(LogLevel.Error, $"Respaldo {sufijo} de '{nombreBase}' generado pero NO sano: {rutaFinal}", usuario: usuario, capa: "Backup");
                throw new RespaldoException($"El respaldo de '{nombreBase}' se generó pero no pasó la verificación; no se puede usar: {rutaFinal}");
            }

            BitacoraLogic.Registrar(LogLevel.Info, $"Respaldo {sufijo} ({origen}) de '{nombreBase}' generado: {rutaFinal}", usuario: usuario, capa: "Backup");

            if (origen == "Automatico")
            {
                AplicarRetencion(nombreBase);
            }

            return rutaFinal;
        }

        public static List<string> RealizarBackupDeLaPlataforma(
            TipoBackup tipo, string? carpetaDestino = null, string origen = "Manual", string usuario = "")
        {
            var rutas = new List<string>();
            foreach (string baseDatos in BasesDeLaPlataforma)
            {
                rutas.Add(RealizarBackup(tipo, baseDatos, carpetaDestino, origen, usuario));
            }
            return rutas;
        }

        /// <summary>
        /// Respaldo automático de una vez por día: solo si los datos están sanos (no se pisa un buen respaldo con
        /// datos alterados) y si hoy todavía no hay uno. Devuelve los archivos generados (vacío si no correspondía).
        /// </summary>
        public static List<string> RespaldoDiarioSiCorresponde(Func<bool>? datosSanos, string usuario = "sistema", string? carpetaDestino = null)
        {
            Func<bool>? verificador = datosSanos ?? _verificadorDeSalud;
            if (verificador != null && !verificador())
            {
                BitacoraLogic.Registrar(LogLevel.Warning,
                    "Respaldo automático omitido: los datos no están sanos (dígitos verificadores).", usuario: usuario, capa: "Backup");
                return new List<string>();
            }

            bool yaHecho = BasesDeLaPlataforma.All(b =>
            {
                RespaldoRegistro? ultimo = Registro.ListarPorOrigen(b, "Automatico").FirstOrDefault();
                return ultimo != null && ultimo.Fecha.Date == DateTime.Today && ultimo.Sano && File.Exists(ultimo.Archivo);
            });
            if (yaHecho)
            {
                return new List<string>();
            }

            return RealizarBackupDeLaPlataforma(TipoBackup.Completo, carpetaDestino, "Automatico", usuario);
        }

        /// <summary>Lista los últimos respaldos registrados e indica si cada archivo sigue en disco.</summary>
        public static List<RespaldoRegistro> ListarRespaldos(int cantidad = 200)
        {
            List<RespaldoRegistro> lista = Registro.ListarUltimos(cantidad);
            foreach (RespaldoRegistro r in lista)
            {
                r.ArchivoExiste = File.Exists(r.Archivo);
            }
            return lista;
        }

        public static void RestaurarBackup(string rutaArchivo, string usuario = "")
        {
            if (string.IsNullOrWhiteSpace(rutaArchivo) || !File.Exists(rutaArchivo))
            {
                throw new RespaldoException($"No existe el archivo de respaldo: '{rutaArchivo}'.");
            }

            string? temporal = null;
            try
            {
                string legible = PrepararLectura(rutaArchivo, out temporal);
                RestaurarDesde(legible, rutaArchivo, usuario);
            }
            finally
            {
                if (temporal != null)
                {
                    TryBorrar(temporal);
                }
            }
        }

        private static void RestaurarDesde(string rutaArchivo, string rutaOriginal, string usuario)
        {
            if (!VerificarSinPreparar(rutaArchivo))
            {
                throw new RespaldoException("El archivo de respaldo no pasó la verificación (RESTORE VERIFYONLY con CHECKSUM); no se restaura.");
            }

            string nombreBase = Repositorio.LeerNombreBaseDesdeArchivo(rutaArchivo);

            // Copia previa de seguridad: si la restauración sale mal, el estado anterior no se pierde.
            if (Repositorio.ExisteBase(nombreBase))
            {
                try
                {
                    string previo = RealizarBackup(TipoBackup.Completo, nombreBase, null, "Previo", usuario);
                    BitacoraLogic.Registrar(LogLevel.Info, $"Copia previa de seguridad de '{nombreBase}': {previo}", usuario: usuario, capa: "Backup");
                }
                catch (Exception ex)
                {
                    throw new RespaldoException($"No se pudo hacer la copia previa de seguridad de '{nombreBase}'; no se restaura. {ex.Message}", ex);
                }
            }

            int sesionesEnUso = Repositorio.CantidadSesionesEnBase(nombreBase);
            if (sesionesEnUso > 0)
            {
                BitacoraLogic.Registrar(LogLevel.Warning,
                    $"La base '{nombreBase}' tiene {sesionesEnUso} sesión(es) activa(s); se cerrarán para restaurar.",
                    usuario: usuario, capa: "Backup");
            }

            try
            {
                Repositorio.EjecutarRestore(nombreBase, rutaArchivo);
            }
            catch (Exception ex)
            {
                BitacoraLogic.Registrar(LogLevel.Error, $"Restauración de '{nombreBase}' FALLÓ.", ex, usuario, capa: "Backup");
                throw new RespaldoException($"No se pudo restaurar '{nombreBase}'. {ex.Message}", ex);
            }

            BitacoraLogic.Registrar(LogLevel.Info, $"Base '{nombreBase}' restaurada desde: {rutaOriginal}", usuario: usuario, capa: "Backup");
        }

        /// <summary>Nombre de la base de datos que contiene el archivo .bak (lee su encabezado).</summary>
        public static string ObtenerBaseDelArchivo(string rutaArchivo)
        {
            string? temporal = null;
            try
            {
                return Repositorio.LeerNombreBaseDesdeArchivo(PrepararLectura(rutaArchivo, out temporal));
            }
            finally
            {
                if (temporal != null)
                {
                    TryBorrar(temporal);
                }
            }
        }

        public static bool VerificarIntegridadBackup(string rutaArchivo)
        {
            string? temporal = null;
            try
            {
                return VerificarSinPreparar(PrepararLectura(rutaArchivo, out temporal), rutaArchivo);
            }
            catch (RespaldoException ex)
            {
                BitacoraLogic.Registrar(LogLevel.Warning, $"Verificación de integridad FALLÓ: {rutaArchivo}", ex, capa: "Backup");
                return false;
            }
            finally
            {
                if (temporal != null)
                {
                    TryBorrar(temporal);
                }
            }
        }

        /// <summary>
        /// El servicio de SQL Server lee los respaldos con su propia cuenta: si no puede abrir el archivo (por ejemplo,
        /// está en la carpeta personal del usuario), se usa una copia temporal en la carpeta del servidor.
        /// </summary>
        private static string PrepararLectura(string ruta, out string? copiaTemporal)
        {
            copiaTemporal = null;
            try
            {
                Repositorio.LeerNombreBaseDesdeArchivo(ruta);
                return ruta;
            }
            catch (Exception ex) when (Repositorio.EsErrorDeCarpetaSinPermiso(ex))
            {
                string copia = Path.Combine(Repositorio.ObtenerCarpetaDelServidor(), "openrin_tmp_" + Guid.NewGuid().ToString("N")[..8] + ".bak");
                try
                {
                    File.Copy(ruta, copia, overwrite: false);
                }
                catch (Exception copiaEx)
                {
                    throw new RespaldoException(
                        $"SQL Server no tiene permiso para leer '{ruta}'. Copie el archivo a la carpeta de respaldos del servidor ({Path.GetDirectoryName(copia)}) o a la carpeta C:/OpenRIN/Backups. {copiaEx.Message}", copiaEx);
                }
                copiaTemporal = copia;
                return copia;
            }
        }

        private static bool VerificarSinPreparar(string rutaArchivo, string? rutaParaMensaje = null)
        {
            try
            {
                Repositorio.EjecutarVerifyOnly(rutaArchivo);
                BitacoraLogic.Registrar(LogLevel.Info, $"Verificación de integridad OK: {rutaParaMensaje ?? rutaArchivo}", capa: "Backup");
                return true;
            }
            catch (Exception ex)
            {
                BitacoraLogic.Registrar(LogLevel.Warning, $"Verificación de integridad FALLÓ: {rutaParaMensaje ?? rutaArchivo}", ex, capa: "Backup");
                return false;
            }
        }

        /// <summary>Registra en la tabla Respaldos un archivo .bak externo que el usuario eligió para restaurar.</summary>
        public static void RegistrarImportado(string rutaArchivo, string usuario)
        {
            string nombreBase = ObtenerBaseDelArchivo(rutaArchivo);
            RegistrarEnTabla(new RespaldoRegistro
            {
                Fecha = DateTime.Now,
                BaseDatos = nombreBase,
                Archivo = rutaArchivo,
                TamanoBytes = new FileInfo(rutaArchivo).Length,
                Origen = "Importado",
                Sano = true,
                Usuario = usuario,
                Observaciones = "Archivo elegido por el usuario para restaurar."
            });
        }

        public static bool ProgramacionActiva => _temporizadorDiario != null;

        public static void ProgramarRespaldoDiario(int horaDelDia = 3)
        {
            if (horaDelDia < 0 || horaDelDia > 23)
            {
                throw new DatoInvalidoException("La hora debe estar entre 0 y 23.", nameof(horaDelDia));
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
                RespaldoDiarioSiCorresponde(null);
            }
            catch (Exception ex)
            {
                BitacoraLogic.Registrar(LogLevel.Fatal, "El respaldo automático diario FALLÓ.", ex, capa: "Backup");
            }
        }

        private static void AplicarRetencion(string nombreBase)
        {
            try
            {
                foreach (RespaldoRegistro viejo in Registro.ListarPorOrigen(nombreBase, "Automatico").Skip(AutomaticosConservados))
                {
                    TryBorrar(viejo.Archivo);
                    Registro.Eliminar(viejo.Id);
                }
            }
            catch (Exception ex)
            {
                BitacoraLogic.Registrar(LogLevel.Warning, $"No se pudo aplicar la retención de respaldos de '{nombreBase}'.", ex, capa: "Backup");
            }
        }

        private static void RegistrarEnTabla(RespaldoRegistro registro)
        {
            try
            {
                Registro.Registrar(registro);
            }
            catch (Exception ex)
            {
                BitacoraLogic.Registrar(LogLevel.Warning, "No se pudo registrar el respaldo en la tabla Respaldos.", ex, capa: "Backup");
            }
        }

        private static void TryBorrar(string ruta)
        {
            try
            {
                if (File.Exists(ruta))
                {
                    File.Delete(ruta);
                }
            }
            catch (Exception)
            {
                // Sin permiso para borrar: el archivo queda y el registro se conserva o se elimina igual.
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

        private static string ObtenerCarpetaRespaldos(string? carpetaElegida)
        {
            if (!string.IsNullOrWhiteSpace(carpetaElegida))
            {
                return carpetaElegida.Trim();
            }

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
                throw new RespaldoException(
                    $"Espacio insuficiente en disco para el respaldo de '{nombreBase}'. " +
                    $"Requerido: ~{requeridoMb:0} MB, disponible: {libreMb:0} MB.");
            }
        }
    }
}
