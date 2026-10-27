using System.Collections.Generic;
using Negocio.BLL;
using Services.Facade;

namespace Negocio.UI;

/// <summary>
/// Punto de entrada de la aplicación de escritorio OpenRIN: registra los manejadores
/// globales de excepciones (REQ-ARQ-004), inicializa el idioma por defecto (REQ-ARQ-001),
/// ejecuta la verificación de integridad ANTES de habilitar cualquier ventana (requisito
/// T08) y luego ejecuta el flujo Login → Panel Principal.
/// </summary>
static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        ExceptionManager.RegistrarManejadoresGlobales();
        LocalizationService.EstablecerIdioma("es");

        // T08: política de arranque — la integridad de la base (DVH/DVV) se verifica antes
        // de dar acceso a la ventana de log-in. Si se detectan alteraciones (o no pudo
        // verificarse), el ingreso queda bloqueado y se informa al administrador.
        ResultadoAuditoria? auditoria = VerificacionArranque.Auditar();
        if (auditoria == null || auditoria.Problemas.Count > 0)
        {
            var problemas = auditoria?.Problemas
                ?? new List<string> { LocalizationService.ObtenerTexto("integridad.vulnerada.sinConexion") };
            using var alerta = new IntegridadVulneradaForm(problemas);
            alerta.ShowDialog();
            return;
        }

        using var login = new LoginForm();
        if (login.ShowDialog() != DialogResult.OK || login.Sesion == null)
        {
            return;
        }

        Application.Run(new PrincipalForm(login.Sesion));
    }
}
