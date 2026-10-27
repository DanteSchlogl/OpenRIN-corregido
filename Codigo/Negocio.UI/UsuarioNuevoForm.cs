using Services.Facade;

namespace Negocio.UI
{
    /// <summary>
    /// Diálogo de alta de usuario del sistema (pantalla Sistema): registra la cuenta a través
    /// de la API real de seguridad (hash PBKDF2 + validaciones de contraseña), con pregunta y
    /// respuesta de seguridad opcionales para el flujo de recuperación de contraseña.
    /// </summary>
    public class UsuarioNuevoForm : Form
    {
        private readonly TextBox _txtUsuario = new();
        private readonly ComboBox _cmbPerfil = new();
        private readonly TextBox _txtNombre = new();
        private readonly TextBox _txtEmail = new();
        private readonly TextBox _txtContrasena = new();
        private readonly ComboBox _cmbPregunta = new();
        private readonly TextBox _txtRespuesta = new();
        private readonly Label _lblAviso = new();

        public UsuarioNuevoForm()
        {
            Text = $"OpenRIN — {Localizacion("sistema.nuevoUsuario")}";
            ClientSize = new Size(500, 414);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Color.White;

            ConstruirInterfaz();
        }

        private static string Localizacion(string clave) => LocalizationService.ObtenerTexto(clave);

        private static Label Etiqueta(string texto, int x, int y, int ancho)
            => new()
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(ancho, 18),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 85, 115)
            };

        private void ConstruirInterfaz()
        {
            Controls.Add(Etiqueta(Localizacion("sistema.columna.usuario"), 18, 12, 220));
            _txtUsuario.Name = "txtNuevoUsuario";
            _txtUsuario.Location = new Point(18, 30);
            _txtUsuario.Size = new Size(220, 28);
            _txtUsuario.Font = new Font("Segoe UI", 10F);

            Controls.Add(Etiqueta(Localizacion("sistema.columna.perfil"), 250, 12, 230));
            _cmbPerfil.Location = new Point(250, 30);
            _cmbPerfil.Size = new Size(230, 28);
            _cmbPerfil.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbPerfil.Font = new Font("Segoe UI", 10F);
            _cmbPerfil.Items.Add("administrativo");
            _cmbPerfil.Items.Add("medico");
            _cmbPerfil.Items.Add("paciente");
            _cmbPerfil.Items.Add("familiar_autorizado");
            _cmbPerfil.Items.Add("sysadmin");
            _cmbPerfil.SelectedIndex = 0;

            Controls.Add(Etiqueta(Localizacion("sistema.columna.nombre"), 18, 68, 462));
            _txtNombre.Name = "txtNuevoNombre";
            _txtNombre.Location = new Point(18, 86);
            _txtNombre.Size = new Size(462, 28);
            _txtNombre.Font = new Font("Segoe UI", 10F);

            Controls.Add(Etiqueta(Localizacion("pacientes.email"), 18, 124, 462));
            _txtEmail.Name = "txtNuevoEmail";
            _txtEmail.Location = new Point(18, 142);
            _txtEmail.Size = new Size(462, 28);
            _txtEmail.Font = new Font("Segoe UI", 10F);

            Controls.Add(Etiqueta(Localizacion("login.contrasena"), 18, 180, 220));
            _txtContrasena.Name = "txtNuevaClave";
            _txtContrasena.Location = new Point(18, 198);
            _txtContrasena.Size = new Size(220, 28);
            _txtContrasena.Font = new Font("Segoe UI", 10F);
            _txtContrasena.UseSystemPasswordChar = true;

            Controls.Add(Etiqueta(Localizacion("rec.pregunta"), 260, 180, 220));
            _cmbPregunta.Location = new Point(260, 198);
            _cmbPregunta.Size = new Size(220, 28);
            _cmbPregunta.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbPregunta.Font = new Font("Segoe UI", 10F);
            _cmbPregunta.Items.Add(Localizacion("rec.pregunta1"));
            _cmbPregunta.Items.Add(Localizacion("rec.pregunta2"));
            _cmbPregunta.Items.Add(Localizacion("rec.pregunta3"));

            Controls.Add(Etiqueta(Localizacion("rec.respuesta"), 18, 238, 220));
            _txtRespuesta.Name = "txtNuevaRespuesta";
            _txtRespuesta.Location = new Point(18, 256);
            _txtRespuesta.Size = new Size(220, 28);
            _txtRespuesta.Font = new Font("Segoe UI", 10F);

            _lblAviso.Location = new Point(18, 292);
            _lblAviso.Size = new Size(462, 40);
            _lblAviso.Font = new Font("Segoe UI", 9F);
            _lblAviso.ForeColor = Color.FromArgb(180, 40, 40);

            var btnGuardar = new Button
            {
                Text = Localizacion("comun.guardar"),
                Location = new Point(296, 352),
                Size = new Size(90, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 108, 223),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            var btnCancelar = new Button
            {
                Text = Localizacion("comun.cancelar"),
                Location = new Point(392, 352),
                Size = new Size(88, 36),
                FlatStyle = FlatStyle.Flat
            };
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; };

            AcceptButton = btnGuardar;
            CancelButton = btnCancelar;

            Controls.Add(_txtUsuario);
            Controls.Add(_cmbPerfil);
            Controls.Add(_txtNombre);
            Controls.Add(_txtEmail);
            Controls.Add(_txtContrasena);
            Controls.Add(_cmbPregunta);
            Controls.Add(_txtRespuesta);
            Controls.AddRange(new Control[] { _lblAviso, btnGuardar, btnCancelar });
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            try
            {
                string perfil = _cmbPerfil.SelectedItem?.ToString() ?? "administrativo";
                string? pregunta = _cmbPregunta.SelectedIndex >= 0 ? _cmbPregunta.SelectedItem?.ToString() : null;
                string? respuesta = string.IsNullOrWhiteSpace(_txtRespuesta.Text) ? null : _txtRespuesta.Text.Trim();

                int id = SeguridadService.RegistrarUsuario(
                    _txtUsuario.Text.Trim(),
                    _txtNombre.Text.Trim(),
                    _txtContrasena.Text,
                    perfil,
                    string.IsNullOrWhiteSpace(_txtEmail.Text) ? null : _txtEmail.Text.Trim(),
                    pregunta,
                    respuesta);

                MessageBox.Show(FindForm(), $"{Localizacion("sistema.usuarioRegistrado")} (Id {id}).",
                    Localizacion("sistema.nuevoUsuario"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (ArgumentException ex)
            {
                _lblAviso.Text = ex.Message;
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "UsuarioNuevoForm");
            }
        }
    }
}
