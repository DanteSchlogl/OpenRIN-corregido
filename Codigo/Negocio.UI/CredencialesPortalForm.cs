using Negocio.BLL;
using Services.Facade;

namespace Negocio.UI
{
    /// <summary>
    /// Muestra una única vez las credenciales del portal generadas al dar de alta un paciente
    /// (REQ-FUNC-001): usuario (DNI) y contraseña temporal, con copiado al portapapeles.
    /// </summary>
    public class CredencialesPortalForm : Form
    {
        public CredencialesPortalForm(CredencialesPortal credenciales)
        {
            Text = LocalizationService.ObtenerTexto("pacientes.cred.titulo");
            ClientSize = new Size(460, 300);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Color.White;

            var lblTitulo = new Label
            {
                Text = LocalizationService.ObtenerTexto("pacientes.cred.titulo"),
                Location = new Point(18, 14),
                Size = new Size(424, 26),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 66, 120)
            };

            var lblAviso = new Label
            {
                Text = LocalizationService.ObtenerTexto("pacientes.cred.aviso"),
                Location = new Point(18, 44),
                Size = new Size(424, 44),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(140, 80, 20)
            };

            var lblUsuario = new Label
            {
                Text = LocalizationService.ObtenerTexto("login.usuario"),
                Location = new Point(18, 96),
                Size = new Size(200, 18),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            var txtUsuario = new TextBox
            {
                Text = credenciales.Usuario,
                Location = new Point(18, 116),
                Size = new Size(424, 28),
                ReadOnly = true,
                Font = new Font("Consolas", 11F)
            };

            var lblClave = new Label
            {
                Text = LocalizationService.ObtenerTexto("login.contrasena"),
                Location = new Point(18, 152),
                Size = new Size(200, 18),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            var txtClave = new TextBox
            {
                Text = credenciales.PasswordTemporal,
                Location = new Point(18, 172),
                Size = new Size(424, 28),
                ReadOnly = true,
                Font = new Font("Consolas", 11F)
            };

            var btnCopiar = new Button
            {
                Text = LocalizationService.ObtenerTexto("pacientes.cred.copiar"),
                Location = new Point(18, 218),
                Size = new Size(160, 34),
                FlatStyle = FlatStyle.Flat
            };
            btnCopiar.Click += (s, e) =>
            {
                Clipboard.SetText(credenciales.PasswordTemporal);
                MessageBox.Show(this, LocalizationService.ObtenerTexto("pacientes.cred.copiado"), Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            var btnCerrar = new Button
            {
                Text = LocalizationService.ObtenerTexto("comun.cerrar"),
                Location = new Point(348, 218),
                Size = new Size(94, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 108, 223),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => { DialogResult = DialogResult.OK; };

            AcceptButton = btnCerrar;
            Controls.AddRange(new Control[] { lblTitulo, lblAviso, lblUsuario, txtUsuario, lblClave, txtClave, btnCopiar, btnCerrar });
        }
    }
}
