using System.Drawing.Drawing2D;
using System.Drawing.Text;
using Services.DomainModel;
using Services.DomainModel.Exceptions;
using Services.Facade;

namespace Negocio.UI
{
    /// <summary>
    /// Pantalla de inicio de sesión: autentica contra el módulo de seguridad de Services
    /// (REQ-ARQ-006), muestra el logo institucional y permite cambiar el idioma en vivo
    /// (REQ-ARQ-001) y recuperar la contraseña con pregunta de seguridad.
    /// </summary>
    public class LoginForm : Form
    {
        private List<Idioma> _idiomasDisponibles = new();

        private readonly Label _lblTitulo = new();
        private readonly Label _lblSubtitulo = new();
        private readonly Panel _panelTarjeta = new();
        private readonly Label _lblUsuario = new();
        private readonly TextBox _txtUsuario = new();
        private readonly Label _lblContrasena = new();
        private readonly TextBox _txtContrasena = new();
        private readonly Button _btnIngresar = new();
        private readonly LinkLabel _lnkRecuperar = new();
        private readonly Label _lblIdioma = new();
        private readonly ComboBox _cmbIdioma = new();
        private readonly Label _lblVersion = new();
        private readonly Icon? _logoLogin = Recursos.CargarIcono(96);
        private bool _actualizandoIdioma;

        /// <summary>Sesión autenticada; disponible cuando el diálogo devuelve OK.</summary>
        public UsuarioAutenticado? Sesion { get; private set; }

        public LoginForm()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            System.Drawing.Icon? icono = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (icono != null)
            {
                Icon = icono;
            }
            ConstruirInterfaz();
            AplicarTextos();
        }

        private static string Texto(string clave) => LocalizationService.ObtenerTexto(clave);

        private void ConstruirInterfaz()
        {
            ClientSize = new Size(460, 520);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            _lblTitulo.Location = new Point(36, 26);
            _lblTitulo.Size = new Size(268, 40);
            _lblTitulo.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            _lblTitulo.ForeColor = Color.FromArgb(27, 79, 138);

            _lblSubtitulo.Location = new Point(38, 68);
            _lblSubtitulo.Size = new Size(388, 20);
            _lblSubtitulo.Font = new Font("Segoe UI", 9.5F);
            _lblSubtitulo.ForeColor = Color.FromArgb(74, 107, 138);

            _panelTarjeta.Location = new Point(36, 104);
            _panelTarjeta.Size = new Size(388, 348);
            _panelTarjeta.BackColor = Color.White;

            _lblUsuario.Location = new Point(24, 26);
            _lblUsuario.Size = new Size(340, 18);
            _lblUsuario.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            _txtUsuario.Location = new Point(24, 48);
            _txtUsuario.Size = new Size(340, 28);
            _txtUsuario.Font = new Font("Segoe UI", 11F);

            _lblContrasena.Location = new Point(24, 92);
            _lblContrasena.Size = new Size(340, 18);
            _lblContrasena.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            _txtContrasena.Location = new Point(24, 114);
            _txtContrasena.Size = new Size(340, 28);
            _txtContrasena.Font = new Font("Segoe UI", 11F);
            _txtContrasena.UseSystemPasswordChar = true;

            _btnIngresar.Location = new Point(24, 168);
            _btnIngresar.Size = new Size(340, 42);
            _btnIngresar.FlatStyle = FlatStyle.Flat;
            _btnIngresar.FlatAppearance.BorderSize = 0;
            _btnIngresar.BackColor = Color.FromArgb(45, 108, 223);
            _btnIngresar.ForeColor = Color.White;
            _btnIngresar.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _btnIngresar.Cursor = Cursors.Hand;
            _btnIngresar.Click += BtnIngresar_Click;

            _lnkRecuperar.Location = new Point(24, 224);
            _lnkRecuperar.Size = new Size(340, 20);
            _lnkRecuperar.Font = new Font("Segoe UI", 9F);
            _lnkRecuperar.LinkColor = Color.FromArgb(45, 108, 223);
            _lnkRecuperar.Click += LnkRecuperar_Click;

            _lblIdioma.Location = new Point(24, 264);
            _lblIdioma.Size = new Size(340, 18);
            _lblIdioma.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            _cmbIdioma.Location = new Point(24, 286);
            _cmbIdioma.Size = new Size(220, 26);
            _cmbIdioma.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbIdioma.Font = new Font("Segoe UI", 9.5F);
            _cmbIdioma.SelectedIndexChanged += CmbIdioma_SelectedIndexChanged;

            _panelTarjeta.Controls.AddRange(new Control[]
            {
                _lblUsuario, _txtUsuario, _lblContrasena, _txtContrasena,
                _btnIngresar, _lnkRecuperar, _lblIdioma, _cmbIdioma
            });

            _lblVersion.Location = new Point(38, 468);
            _lblVersion.Size = new Size(388, 18);
            _lblVersion.Font = new Font("Segoe UI", 8F);
            _lblVersion.ForeColor = Color.FromArgb(110, 130, 150);

            Controls.Add(_panelTarjeta);
            AcceptButton = _btnIngresar;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using var pincel = new LinearGradientBrush(
                ClientRectangle,
                Color.FromArgb(196, 226, 250),
                Color.FromArgb(235, 246, 255),
                LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(pincel, ClientRectangle);

            // Logo institucional (carta de presentación), arriba a la derecha del título.
            if (_logoLogin != null)
            {
                e.Graphics.DrawIcon(_logoLogin, new Rectangle(336, 8, 92, 92));
            }

            // Títulos dibujados a mano: se funden con el degradé (sin cajas grises de fondo).
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (var fuenteTitulo = new Font("Segoe UI", 22F, FontStyle.Bold))
            using (var pincelTitulo = new SolidBrush(Color.FromArgb(27, 79, 138)))
            {
                e.Graphics.DrawString(_lblTitulo.Text, fuenteTitulo, pincelTitulo, 36, 24);
            }
            using (var fuenteSubtitulo = new Font("Segoe UI", 9.5F))
            using (var pincelSubtitulo = new SolidBrush(Color.FromArgb(74, 107, 138)))
            {
                e.Graphics.DrawString(_lblSubtitulo.Text, fuenteSubtitulo, pincelSubtitulo, 38, 68);
            }
            using (var fuenteVersion = new Font("Segoe UI", 8F))
            using (var pincelVersion = new SolidBrush(Color.FromArgb(110, 130, 150)))
            {
                e.Graphics.DrawString(_lblVersion.Text, fuenteVersion, pincelVersion, 38, 468);
            }
        }

        private void AplicarTextos()
        {
            Text = $"{Texto("app.nombre")} — {Texto("login.titulo")}";
            _lblTitulo.Text = Texto("app.nombre");
            _lblSubtitulo.Text = Texto("app.titulo");
            _lblUsuario.Text = Texto("login.usuario");
            _lblContrasena.Text = Texto("login.contrasena");
            _btnIngresar.Text = Texto("login.ingresar");
            _lnkRecuperar.Text = Texto("login.olvidoContrasena");
            _lblIdioma.Text = Texto("login.idioma");
            _lblVersion.Text = "OpenRIN · .NET 8 · v1.0";

            _actualizandoIdioma = true;
            try
            {
                _cmbIdioma.Items.Clear();
                _idiomasDisponibles = LocalizationService.Idiomas.ToList();
                foreach (Idioma idioma in _idiomasDisponibles)
                {
                    _cmbIdioma.Items.Add(idioma.Nombre);
                }
                int indice = _idiomasDisponibles.FindIndex(i => i.Codigo == LocalizationService.IdiomaActual);
                _cmbIdioma.SelectedIndex = indice >= 0 ? indice : 0;
            }
            finally
            {
                _actualizandoIdioma = false;
            }

            Invalidate();
        }

        private void CmbIdioma_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_actualizandoIdioma || _cmbIdioma.SelectedIndex < 0)
            {
                return;
            }
            LocalizationService.EstablecerIdioma(_idiomasDisponibles[_cmbIdioma.SelectedIndex].Codigo);
            AplicarTextos();
        }

        private void BtnIngresar_Click(object? sender, EventArgs e)
        {
            string usuario = _txtUsuario.Text.Trim();
            string contrasena = _txtContrasena.Text;
            if (usuario.Length == 0 || contrasena.Length == 0)
            {
                MostrarAdvertencia(Texto("login.campoObligatorio"));
                return;
            }

            _btnIngresar.Enabled = false;
            UseWaitCursor = true;
            try
            {
                Sesion = SeguridadService.Autenticar(usuario, contrasena);
                SesionActual.Instancia.Iniciar(Sesion);
                DialogResult = DialogResult.OK;
            }
            catch (UsuarioBloqueadoException ex)
            {
                MostrarAdvertencia(ex.Message);
                _txtContrasena.Clear();
            }
            catch (CredencialesInvalidasException ex)
            {
                MostrarAdvertencia(ex.Message);
                _txtContrasena.Clear();
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "LoginForm", mostrarMensaje: false);
                MostrarAdvertencia(Texto("error.generico"));
                _txtContrasena.Clear();
            }
            finally
            {
                _btnIngresar.Enabled = true;
                UseWaitCursor = false;
            }
        }

        private void LnkRecuperar_Click(object? sender, EventArgs e)
        {
            using var recuperacion = new RecuperacionForm(_txtUsuario.Text.Trim());
            recuperacion.ShowDialog(this);
        }

        private void MostrarAdvertencia(string mensaje)
        {
            MessageBox.Show(this, mensaje, Texto("login.titulo"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
