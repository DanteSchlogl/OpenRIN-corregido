using Negocio.BLL;
using Negocio.DAL.Context;
using Negocio.DomainModel;
using Negocio.DomainModel.Enums;
using Negocio.DomainModel.Exceptions;
using Services.Facade;

namespace Negocio.UI
{
    /// <summary>
    /// Diálogo de alta de un evento adverso (REQ-FUNC-003): fecha (no futura), tipo, gravedad,
    /// descripción y acción tomada, con las validaciones de la BLL.
    /// </summary>
    public class EventoAdversoForm : Form
    {
        private readonly Paciente _paciente;
        private readonly string _usuarioResponsable;

        private readonly DateTimePicker _dtpFecha = new();
        private readonly ComboBox _cmbTipo = new();
        private readonly ComboBox _cmbGravedad = new();
        private readonly TextBox _txtDescripcion = new();
        private readonly TextBox _txtAccion = new();
        private readonly Label _lblAviso = new();

        public EventoAdversoForm(Paciente paciente, string usuarioResponsable)
        {
            _paciente = paciente;
            _usuarioResponsable = usuarioResponsable;

            Text = $"OpenRIN — {Localizacion("eventos.registrar")}";
            ClientSize = new Size(520, 400);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Color.White;

            ConstruirInterfaz();
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
            var lblPaciente = new Label
            {
                Text = $"{_paciente.NombreCompleto} ({_paciente.DNI})",
                Location = new Point(18, 14),
                Size = new Size(480, 24),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 66, 120)
            };

            Controls.Add(lblPaciente);

            Controls.Add(Etiqueta("eventos.fechaEvento", 18, 48, 160));
            _dtpFecha.Location = new Point(18, 66);
            _dtpFecha.Size = new Size(160, 28);
            _dtpFecha.Format = DateTimePickerFormat.Short;
            _dtpFecha.Font = new Font("Segoe UI", 10F);
            _dtpFecha.Value = DateTime.Today;

            Controls.Add(Etiqueta("eventos.columna.tipo", 196, 48, 150));
            _cmbTipo.Location = new Point(196, 66);
            _cmbTipo.Size = new Size(150, 28);
            _cmbTipo.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbTipo.Font = new Font("Segoe UI", 10F);
            _cmbTipo.Items.Add(Localizacion("eventos.tipo.hemorragia"));
            _cmbTipo.Items.Add(Localizacion("eventos.tipo.trombosis"));
            _cmbTipo.Items.Add(Localizacion("eventos.tipo.reaccion"));
            _cmbTipo.Items.Add(Localizacion("eventos.tipo.otro"));
            _cmbTipo.SelectedIndex = 0;

            Controls.Add(Etiqueta("eventos.columna.gravedad", 364, 48, 138));
            _cmbGravedad.Location = new Point(364, 66);
            _cmbGravedad.Size = new Size(138, 28);
            _cmbGravedad.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbGravedad.Font = new Font("Segoe UI", 10F);
            _cmbGravedad.Items.Add(Localizacion("eventos.gravedad.leve"));
            _cmbGravedad.Items.Add(Localizacion("eventos.gravedad.moderada"));
            _cmbGravedad.Items.Add(Localizacion("eventos.gravedad.grave"));
            _cmbGravedad.SelectedIndex = 0;

            Controls.Add(Etiqueta("reportes.columna.descripcion", 18, 106, 480));
            _txtDescripcion.Name = "txtDescripcionEvento";
            _txtDescripcion.Location = new Point(18, 124);
            _txtDescripcion.Size = new Size(484, 62);
            _txtDescripcion.Multiline = true;
            _txtDescripcion.Font = new Font("Segoe UI", 9.5F);
            _txtDescripcion.ScrollBars = ScrollBars.Vertical;

            Controls.Add(Etiqueta("eventos.columna.accion", 18, 194, 480));
            _txtAccion.Name = "txtAccionEvento";
            _txtAccion.Location = new Point(18, 212);
            _txtAccion.Size = new Size(484, 62);
            _txtAccion.Multiline = true;
            _txtAccion.Font = new Font("Segoe UI", 9.5F);
            _txtAccion.ScrollBars = ScrollBars.Vertical;

            Controls.Add(_dtpFecha);
            Controls.Add(_cmbTipo);
            Controls.Add(_cmbGravedad);
            Controls.Add(_txtDescripcion);
            Controls.Add(_txtAccion);

            _lblAviso.Location = new Point(18, 280);
            _lblAviso.Size = new Size(484, 42);
            _lblAviso.Font = new Font("Segoe UI", 9F);
            _lblAviso.ForeColor = Color.FromArgb(180, 40, 40);

            var btnGuardar = new Button
            {
                Text = Localizacion("comun.guardar"),
                Location = new Point(312, 334),
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
                Location = new Point(410, 334),
                Size = new Size(92, 36),
                FlatStyle = FlatStyle.Flat
            };
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; };

            AcceptButton = btnGuardar;
            CancelButton = btnCancelar;

            Controls.AddRange(new Control[] { _lblAviso, btnGuardar, btnCancelar });
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            try
            {
                using var contexto = new NegocioDbContext();
                var evento = new EventoAdverso
                {
                    IdPaciente = _paciente.Id,
                    Fecha = _dtpFecha.Value.Date,
                    Tipo = (TipoEventoAdverso)_cmbTipo.SelectedIndex,
                    Gravedad = (GravedadEvento)_cmbGravedad.SelectedIndex,
                    Descripcion = _txtDescripcion.Text.Trim(),
                    AccionTomada = _txtAccion.Text.Trim()
                };

                new EventoAdversoLogic(contexto).Registrar(evento, _usuarioResponsable);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (ValidacionNegocioException ex)
            {
                _lblAviso.Text = string.Join(Environment.NewLine, ex.Errores);
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "EventoAdversoForm");
            }
        }
    }
}
