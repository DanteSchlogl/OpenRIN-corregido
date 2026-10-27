using Negocio.BLL;
using Negocio.DAL.Context;

namespace Negocio.UI
{
    /// <summary>
    /// Verificación de integridad (DVH/DVV) ejecutada en el arranque de la aplicación,
    /// antes de mostrar cualquier ventana (requisito T08). Si detecta alteraciones,
    /// el flujo principal muestra la pantalla de alerta y no habilita el ingreso.
    /// </summary>
    internal static class VerificacionArranque
    {
        /// <summary>Audita la integridad de la base. Devuelve null si la auditoría no pudo ejecutarse.</summary>
        public static ResultadoAuditoria? Auditar()
        {
            try
            {
                using var contexto = new NegocioDbContext();
                return new IntegridadLogic(contexto).Auditar();
            }
            catch (Exception ex)
            {
                Services.Facade.ExceptionManager.ManejarExcepcion(ex, "Verificación de integridad (arranque)", mostrarMensaje: false);
                return null;
            }
        }
    }
}
