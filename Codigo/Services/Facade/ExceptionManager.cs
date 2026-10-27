using System;
using Services.BLL.ExceptionManagement;

namespace Services.Facade
{
    /// <summary>
    /// Fachada pública de gestión de excepciones (REQ-ARQ-004).
    /// Único punto de entrada para manejar errores no controlados y reintentos.
    /// </summary>
    public static class ExceptionManager
    {
        /// <summary>Registra la excepción en bitácora y muestra (opcionalmente) un mensaje amigable al usuario.</summary>
        public static void ManejarExcepcion(Exception ex, string contexto = "", bool mostrarMensaje = true)
            => ExceptionManagerLogic.Manejar(ex, contexto, mostrarMensaje);

        /// <summary>Ejecuta una operación con reintentos ante fallos transitorios.</summary>
        public static T Reintentar<T>(Func<T> operacion, int maxReintentos = 3, int intervaloMs = 1000)
            => ExceptionManagerLogic.Reintentar(operacion, maxReintentos, intervaloMs);

        /// <summary>
        /// Suscribe los manejadores globales de excepciones (WinForms + AppDomain).
        /// Debe invocarse una vez al iniciar la aplicación de escritorio (Program.cs de la UI).
        /// </summary>
        public static void RegistrarManejadoresGlobales() => ExceptionManagerLogic.RegistrarManejadoresGlobales();
    }
}
