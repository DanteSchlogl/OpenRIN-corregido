using System;
using System.Windows.Forms;
using Services.DomainModel;

namespace Services.BLL.ExceptionManagement
{
    /// <summary>
    /// Núcleo de gestión de excepciones (REQ-ARQ-004): registra los errores en la bitácora,
    /// muestra al usuario un mensaje amigable y ofrece reintentos ante fallos transitorios.
    /// Nunca debe lanzar excepciones secundarias.
    /// </summary>
    internal static class ExceptionManagerLogic
    {
        public static void Manejar(Exception ex, string contexto = "", bool mostrarMensaje = true)
        {
            try
            {
                string mensaje = string.IsNullOrWhiteSpace(contexto)
                    ? "Excepción no controlada."
                    : $"Excepción no controlada en: {contexto}";
                BitacoraLogic.Registrar(LogLevel.Error, mensaje, ex, capa: "Services");
            }
            catch
            {
                // Nunca debe fallar la gestión por un problema al registrar.
            }

            if (!mostrarMensaje)
            {
                return;
            }

            try
            {
                MessageBox.Show(
                    "Ocurrió un error inesperado. La operación no pudo completarse.\n" +
                    "Si el problema persiste, contacte al administrador del sistema.",
                    "OpenRIN",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
                // Sin interfaz disponible: el error ya quedó en bitácora.
            }
        }

        public static T Reintentar<T>(Func<T> operacion, int maxReintentos = 3, int intervaloMs = 1000)
        {
            ArgumentNullException.ThrowIfNull(operacion);

            int intentos = Math.Max(1, maxReintentos);
            Exception? ultimoError = null;

            for (int intento = 1; intento <= intentos; intento++)
            {
                try
                {
                    return operacion();
                }
                catch (Exception ex)
                {
                    ultimoError = ex;
                    if (intento < intentos)
                    {
                        System.Threading.Thread.Sleep(intervaloMs);
                    }
                }
            }

            throw ultimoError!;
        }

        public static void RegistrarManejadoresGlobales()
        {
            Application.ThreadException += (sender, e) => Manejar(e.Exception);

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    Manejar(ex, mostrarMensaje: false);
                }
            };
        }
    }
}
