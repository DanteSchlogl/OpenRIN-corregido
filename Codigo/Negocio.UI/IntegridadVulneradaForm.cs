using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Services.Facade;

namespace Negocio.UI
{
    /// <summary>
    /// Pantalla de bloqueo por integridad (T08): reemplaza a la ventana de log-in cuando la
    /// verificación de arranque detecta alteraciones en la base de datos realizadas por
    /// fuera del sistema. Informa la situación al administrador para que tome medidas.
    /// </summary>
    public sealed class IntegridadVulneradaForm : Form
    {
        private readonly Button _btnSalir = new();
        private readonly TextBox _detalle = new();

        /// <summary>Crea la pantalla a partir de la lista de problemas detectados por la auditoría.</summary>
        public IntegridadVulneradaForm(IReadOnlyList<string> problemas)
        {
            Text = LocalizationService.ObtenerTexto("integridad.vulnerada.titulo");
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            ClientSize = new Size(620, 430);
            BackColor = Color.White;
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            var lblIcono = new Label
            {
                Text = "⚠",
                Font = new Font("Segoe UI", 44F, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 40, 40),
                Location = new Point(24, 18),
                Size = new Size(78, 78),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(lblIcono);

            var lblTitulo = new Label
            {
                Text = LocalizationService.ObtenerTexto("integridad.vulnerada.titulo"),
                Font = new Font("Segoe UI", 16.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 25, 25),
                Location = new Point(108, 30),
                Size = new Size(490, 32)
            };
            Controls.Add(lblTitulo);

            var lblMensaje = new Label
            {
                Text = LocalizationService.ObtenerTexto("integridad.vulnerada.mensaje"),
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = Color.FromArgb(55, 55, 55),
                Location = new Point(110, 66),
                Size = new Size(488, 96)
            };
            Controls.Add(lblMensaje);

            var lblDetalle = new Label
            {
                Text = LocalizationService.ObtenerTexto("integridad.vulnerada.detalle"),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(90, 90, 90),
                Location = new Point(26, 172),
                Size = new Size(568, 20)
            };
            Controls.Add(lblDetalle);

            var texto = new StringBuilder();
            foreach (string problema in problemas.Take(12))
            {
                texto.AppendLine("• " + problema);
            }
            if (problemas.Count > 12)
            {
                texto.AppendLine($"… (+{problemas.Count - 12})");
            }

            _detalle.Multiline = true;
            _detalle.ReadOnly = true;
            _detalle.ScrollBars = ScrollBars.Vertical;
            _detalle.BackColor = Color.FromArgb(252, 245, 245);
            _detalle.ForeColor = Color.FromArgb(120, 40, 40);
            _detalle.Font = new Font("Segoe UI", 9F);
            _detalle.Location = new Point(26, 194);
            _detalle.Size = new Size(568, 160);
            _detalle.Text = texto.ToString();
            Controls.Add(_detalle);

            _btnSalir.Text = LocalizationService.ObtenerTexto("integridad.vulnerada.salir");
            _btnSalir.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _btnSalir.BackColor = Color.FromArgb(200, 40, 40);
            _btnSalir.ForeColor = Color.White;
            _btnSalir.FlatStyle = FlatStyle.Flat;
            _btnSalir.FlatAppearance.BorderSize = 0;
            _btnSalir.Cursor = Cursors.Hand;
            _btnSalir.Location = new Point(464, 372);
            _btnSalir.Size = new Size(130, 40);
            _btnSalir.Click += (s, e) => Close();
            Controls.Add(_btnSalir);
            AcceptButton = _btnSalir;
        }
    }
}
