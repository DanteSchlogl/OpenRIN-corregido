using Services.Facade;

namespace Negocio.UI
{
    /// <summary>
    /// Alta de idioma (T05): diálogo con código y nombre para incorporar un idioma nuevo
    /// desde la administración del sistema. La creación efectiva la realiza la fachada de
    /// localización, que valida y audita la operación.
    /// </summary>
    public sealed class IdiomaNuevoForm : Form
    {
        private readonly Label _lblCodigo = new();
        private readonly Label _lblNombre = new();
        private readonly TextBox _txtCodigo = new();
        private readonly TextBox _txtNombre = new();
        private readonly Button _btnGuardar = new();
        private readonly Button _btnCancelar = new();

        /// <summary>Crea el diálogo de alta de idioma.</summary>
        public IdiomaNuevoForm()
        {
            Text = LocalizationService.ObtenerTexto("idiomas.nuevo");
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(430, 190);
            BackColor = Color.White;
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            _lblCodigo.Text = LocalizationService.ObtenerTexto("idiomas.codigo");
            _lblCodigo.Location = new Point(24, 30);
            _lblCodigo.Size = new Size(90, 20);
            _lblCodigo.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _lblCodigo.ForeColor = Color.FromArgb(60, 85, 115);
            Controls.Add(_lblCodigo);

            _txtCodigo.Location = new Point(120, 26);
            _txtCodigo.Size = new Size(150, 28);
            _txtCodigo.Font = new Font("Segoe UI", 9.5F);
            Controls.Add(_txtCodigo);

            _lblNombre.Text = LocalizationService.ObtenerTexto("idiomas.nombre");
            _lblNombre.Location = new Point(24, 78);
            _lblNombre.Size = new Size(90, 20);
            _lblNombre.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _lblNombre.ForeColor = Color.FromArgb(60, 85, 115);
            Controls.Add(_lblNombre);

            _txtNombre.Location = new Point(120, 74);
            _txtNombre.Size = new Size(280, 28);
            _txtNombre.Font = new Font("Segoe UI", 9.5F);
            Controls.Add(_txtNombre);

            _btnGuardar.Text = LocalizationService.ObtenerTexto("comun.aceptar");
            _btnGuardar.Location = new Point(170, 132);
            _btnGuardar.Size = new Size(120, 36);
            _btnGuardar.FlatStyle = FlatStyle.Flat;
            _btnGuardar.BackColor = Color.FromArgb(45, 108, 223);
            _btnGuardar.ForeColor = Color.White;
            _btnGuardar.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _btnGuardar.FlatAppearance.BorderSize = 0;
            _btnGuardar.Cursor = Cursors.Hand;
            _btnGuardar.Click += (s, e) => Guardar();
            Controls.Add(_btnGuardar);

            _btnCancelar.Text = LocalizationService.ObtenerTexto("comun.cancelar");
            _btnCancelar.Location = new Point(300, 132);
            _btnCancelar.Size = new Size(100, 36);
            _btnCancelar.FlatStyle = FlatStyle.Flat;
            _btnCancelar.BackColor = Color.White;
            _btnCancelar.ForeColor = Color.FromArgb(28, 60, 105);
            _btnCancelar.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _btnCancelar.FlatAppearance.BorderColor = Color.FromArgb(202, 220, 240);
            _btnCancelar.Cursor = Cursors.Hand;
            _btnCancelar.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.Add(_btnCancelar);

            AcceptButton = _btnGuardar;
            CancelButton = _btnCancelar;
        }

        private void Guardar()
        {
            try
            {
                LocalizationService.RegistrarIdioma(_txtCodigo.Text, _txtNombre.Text);
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                Services.Facade.ExceptionManager.ManejarExcepcion(ex, "Alta de idioma", mostrarMensaje: false);
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
