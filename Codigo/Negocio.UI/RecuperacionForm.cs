using Services.DomainModel.Exceptions;
using Services.Facade;

namespace Negocio.UI
{
    /// <summary>
    /// Diálogo de recuperación de contraseña (REQ-NF-001): valida la identidad mediante la
    /// pregunta de seguridad configurada (hash) y permite establecer una contraseña nueva.
    /// </summary>
    public class RecuperacionForm : Form
    {
        private readonly TextBox _txtUsuario = new();
        private readonly Button _btnVerPregunta = new();
        private readonly Label _lblPregunta = new();
        private readonly Label _lblRespuesta = new();
        private readonly TextBox _txtRespuesta = new();
        private readonly Label _lblNueva = new();
        private readonly TextBox _txtNuevaContrasena = new();
        private readonly Label _lblAviso = new();
        private readonly Button _btnCambiar = new();

        public RecuperacionForm(string usuarioInicial)
        {
            Text = $"OpenRIN — {Localizacion("rec.titulo")}";
            ClientSize = new Size(460, 344);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Color.White;

            ConstruirInterfaz();
            if (usuarioInicial.Length > 0)
            {
                _txtUsuario.Text = usuarioInicial;
            }
        }

        private static string Localizacion(string clave) => LocalizationService.ObtenerTexto(clave);

        private static Label Etiqueta(string clave, int x, int y, int ancho)
            => new()
            {
                Text = Localizacion(clave),
                Location = new Point(x, y),
                Size = new Size(ancho, 18),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 85, 115)
            };

        private void ConstruirInterfaz()
        {
            Controls.Add(Etiqueta("login.usuario", 18, 14, 240));
            _txtUsuario.Name = "txtUsuarioRecuperacion";
            _txtUsuario.Location = new Point(18, 32);
            _txtUsuario.Size = new Size(240, 28);
            _txtUsuario.Font = new Font("Segoe UI", 10F);
            _txtUsuario.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; VerPregunta(); } };

            _btnVerPregunta.Name = "btnVerPregunta";
            _btnVerPregunta.Location = new Point(270, 31);
            _btnVerPregunta.Size = new Size(172, 30);
            _btnVerPregunta.FlatStyle = FlatStyle.Flat;
            _btnVerPregunta.BackColor = Color.FromArgb(45, 108, 223);
            _btnVerPregunta.ForeColor = Color.White;
            _btnVerPregunta.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _btnVerPregunta.FlatAppearance.BorderSize = 0;
            _btnVerPregunta.Cursor = Cursors.Hand;
            _btnVerPregunta.Click += (s, e) => VerPregunta();
            _btnVerPregunta.Text = Localizacion("rec.verPregunta");

            _lblPregunta.Location = new Point(18, 74);
            _lblPregunta.Size = new Size(424, 40);
            _lblPregunta.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _lblPregunta.ForeColor = Color.FromArgb(30, 66, 120);

            Controls.Add(Etiqueta("rec.respuesta", 18, 118, 240));
            _txtRespuesta.Name = "txtRespuestaSeguridad";
            _txtRespuesta.Location = new Point(18, 136);
            _txtRespuesta.Size = new Size(280, 28);
            _txtRespuesta.Font = new Font("Segoe UI", 10F);

            Controls.Add(Etiqueta("rec.nuevaContrasena", 18, 174, 240));
            _txtNuevaContrasena.Name = "txtNuevaContrasena";
            _txtNuevaContrasena.Location = new Point(18, 192);
            _txtNuevaContrasena.Size = new Size(280, 28);
            _txtNuevaContrasena.Font = new Font("Segoe UI", 10F);
            _txtNuevaContrasena.UseSystemPasswordChar = true;

            _lblAviso.Location = new Point(18, 226);
            _lblAviso.Size = new Size(424, 40);
            _lblAviso.Font = new Font("Segoe UI", 9F);
            _lblAviso.ForeColor = Color.FromArgb(180, 40, 40);

            _btnCambiar.Name = "btnCambiar";
            _btnCambiar.Location = new Point(248, 292);
            _btnCambiar.Size = new Size(194, 36);
            _btnCambiar.FlatStyle = FlatStyle.Flat;
            _btnCambiar.BackColor = Color.FromArgb(45, 108, 223);
            _btnCambiar.ForeColor = Color.White;
            _btnCambiar.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _btnCambiar.FlatAppearance.BorderSize = 0;
            _btnCambiar.Cursor = Cursors.Hand;
            _btnCambiar.Click += (s, e) => CambiarContrasena();
            _btnCambiar.Text = Localizacion("rec.cambiar");

            var btnCancelar = new Button
            {
                Text = Localizacion("comun.cancelar"),
                Location = new Point(18, 292),
                Size = new Size(90, 36),
                FlatStyle = FlatStyle.Flat
            };
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; };

            CancelButton = btnCancelar;

            Controls.Add(_txtUsuario);
            Controls.Add(_btnVerPregunta);
            Controls.Add(_lblPregunta);
            Controls.Add(_txtRespuesta);
            Controls.Add(_txtNuevaContrasena);
            Controls.AddRange(new Control[] { _lblAviso, _btnCambiar, btnCancelar });
        }

        private void VerPregunta()
        {
            _lblAviso.Text = string.Empty;
            string usuario = _txtUsuario.Text.Trim();
            if (usuario.Length == 0)
            {
                _lblPregunta.Text = string.Empty;
                _lblAviso.Text = Localizacion("login.campoObligatorio");
                return;
            }

            try
            {
                string? pregunta = SeguridadService.ObtenerPreguntaSeguridad(usuario);
                _lblPregunta.Text = pregunta == null
                    ? Localizacion("rec.sinPregunta")
                    : $"{Localizacion("rec.pregunta")} {pregunta}";
                _lblPregunta.ForeColor = pregunta == null ? Color.FromArgb(180, 40, 40) : Color.FromArgb(30, 66, 120);
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "RecuperacionForm");
            }
        }

        private void CambiarContrasena()
        {
            try
            {
                SeguridadService.RecuperarContrasena(
                    _txtUsuario.Text.Trim(),
                    _txtRespuesta.Text.Trim(),
                    _txtNuevaContrasena.Text);

                MessageBox.Show(this, Localizacion("rec.ok"), Localizacion("rec.cambiar"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (CredencialesInvalidasException ex)
            {
                _lblAviso.Text = ex.Message;
            }
            catch (ArgumentException ex)
            {
                _lblAviso.Text = ex.Message;
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "RecuperacionForm");
            }
        }
    }
}
