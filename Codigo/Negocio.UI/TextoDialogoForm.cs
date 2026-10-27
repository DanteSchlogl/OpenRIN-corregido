using Services.Facade;

namespace Negocio.UI
{
    /// <summary>Diálogo genérico para solicitar un texto corto (por ejemplo, el motivo de una baja).</summary>
    public class TextoDialogoForm : Form
    {
        private readonly TextBox _txtValor = new();

        /// <summary>Texto ingresado, sin espacios sobrantes.</summary>
        public string Valor => _txtValor.Text.Trim();

        public TextoDialogoForm(string titulo, string etiqueta)
        {
            Text = titulo;
            ClientSize = new Size(420, 170);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Color.White;

            var lblEtiqueta = new Label
            {
                Text = etiqueta,
                Location = new Point(18, 16),
                Size = new Size(384, 20),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            _txtValor.Location = new Point(18, 42);
            _txtValor.Size = new Size(384, 28);
            _txtValor.Font = new Font("Segoe UI", 10.5F);

            var btnAceptar = new Button
            {
                Text = LocalizationService.ObtenerTexto("comun.aceptar"),
                Location = new Point(222, 100),
                Size = new Size(90, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 108, 223),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btnAceptar.FlatAppearance.BorderSize = 0;
            btnAceptar.Click += (s, e) => { DialogResult = DialogResult.OK; };

            var btnCancelar = new Button
            {
                Text = LocalizationService.ObtenerTexto("comun.cancelar"),
                Location = new Point(318, 100),
                Size = new Size(84, 32),
                FlatStyle = FlatStyle.Flat
            };
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; };

            AcceptButton = btnAceptar;
            CancelButton = btnCancelar;
            Controls.AddRange(new Control[] { lblEtiqueta, _txtValor, btnAceptar, btnCancelar });
        }
    }
}
