using Microsoft.EntityFrameworkCore;
using Negocio.BLL;
using Negocio.DAL.Context;
using Negocio.DomainModel;
using Negocio.DomainModel.Enums;
using Negocio.DomainModel.Exceptions;
using Services.Facade;

namespace Negocio.UI
{
    /// <summary>
    /// Diálogo para agendar un turno presencial (REQ-FUNC-009): selección de paciente activo,
    /// médico habilitado, fecha y hora, con las validaciones de agenda de la BLL.
    /// </summary>
    public class AgendarTurnoForm : Form
    {
        private sealed class OpcionPaciente
        {
            public int Id { get; set; }
            public string Texto { get; set; } = string.Empty;
            public override string ToString() => Texto;
        }

        private sealed class OpcionMedico
        {
            public int Id { get; set; }
            public string Texto { get; set; } = string.Empty;
            public override string ToString() => Texto;
        }

        private readonly string _usuarioResponsable;
        private readonly ComboBox _cmbPaciente = new();
        private readonly ComboBox _cmbMedico = new();
        private readonly DateTimePicker _dtpFecha = new();
        private readonly DateTimePicker _dtpHora = new();
        private readonly TextBox _txtObservaciones = new();
        private readonly Label _lblAviso = new();

        public AgendarTurnoForm(string usuarioResponsable)
        {
            _usuarioResponsable = usuarioResponsable;

            Text = $"OpenRIN — {Localizacion("agenda.agendar")}";
            ClientSize = new Size(480, 340);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Color.White;

            ConstruirInterfaz();
            CargarCombos();
        }

        private static string Localizacion(string clave) => LocalizationService.ObtenerTexto(clave);

        private static Label Etiqueta(string clave, int x, int y, int ancho = 210)
            => new()
            {
                Text = Localizacion(clave),
                Location = new Point(x, y),
                Size = new Size(ancho, 18),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

        private void ConstruirInterfaz()
        {
            Controls.Add(Etiqueta("panel.columna.paciente", 18, 14));
            _cmbPaciente.Location = new Point(18, 34);
            _cmbPaciente.Size = new Size(444, 28);
            _cmbPaciente.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbPaciente.Font = new Font("Segoe UI", 10F);

            Controls.Add(Etiqueta("agenda.columna.medico", 18, 70));
            _cmbMedico.Location = new Point(18, 90);
            _cmbMedico.Size = new Size(280, 28);
            _cmbMedico.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbMedico.Font = new Font("Segoe UI", 10F);

            Controls.Add(Etiqueta("rin.columna.fecha", 310, 70));
            _dtpFecha.Location = new Point(310, 90);
            _dtpFecha.Size = new Size(120, 28);
            _dtpFecha.Format = DateTimePickerFormat.Short;
            _dtpFecha.Font = new Font("Segoe UI", 10F);
            _dtpFecha.Value = DateTime.Today.AddDays(1);

            Controls.Add(Etiqueta("agenda.hora", 18, 128, 120));
            _dtpHora.Location = new Point(18, 148);
            _dtpHora.Size = new Size(110, 28);
            _dtpHora.Format = DateTimePickerFormat.Custom;
            _dtpHora.CustomFormat = "HH:mm";
            _dtpHora.ShowUpDown = true;
            _dtpHora.Font = new Font("Segoe UI", 10F);

            Controls.Add(Etiqueta("agenda.columna.observaciones", 144, 128, 318));
            _txtObservaciones.Location = new Point(144, 148);
            _txtObservaciones.Size = new Size(318, 28);
            _txtObservaciones.Font = new Font("Segoe UI", 10F);

            _lblAviso.Location = new Point(18, 186);
            _lblAviso.Size = new Size(444, 40);
            _lblAviso.Font = new Font("Segoe UI", 9F);
            _lblAviso.ForeColor = Color.FromArgb(180, 40, 40);

            var btnGuardar = new Button
            {
                Text = Localizacion("comun.guardar"),
                Location = new Point(276, 236),
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
                Location = new Point(374, 236),
                Size = new Size(90, 36),
                FlatStyle = FlatStyle.Flat
            };
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; };

            AcceptButton = btnGuardar;
            CancelButton = btnCancelar;

            Controls.AddRange(new Control[]
            {
                _cmbPaciente, _cmbMedico, _dtpFecha, _dtpHora, _txtObservaciones, _lblAviso, btnGuardar, btnCancelar
            });
        }

        private void CargarCombos()
        {
            using var contexto = new NegocioDbContext();

            var pacientes = contexto.Pacientes.AsNoTracking()
                .Where(p => p.Estado == EstadoPaciente.Activo)
                .OrderBy(p => p.NombreCompleto)
                .ToList();
            foreach (Paciente paciente in pacientes)
            {
                _cmbPaciente.Items.Add(new OpcionPaciente { Id = paciente.Id, Texto = $"{paciente.NombreCompleto} ({paciente.DNI})" });
            }
            if (_cmbPaciente.Items.Count > 0)
            {
                _cmbPaciente.SelectedIndex = 0;
            }

            var medicos = contexto.Usuarios.AsNoTracking()
                .Where(u => u.Perfil == "medico" && u.Activo)
                .OrderBy(u => u.NombreCompleto)
                .ToList();
            foreach (Usuario medico in medicos)
            {
                _cmbMedico.Items.Add(new OpcionMedico { Id = medico.Id, Texto = medico.NombreCompleto });
            }
            if (_cmbMedico.Items.Count > 0)
            {
                _cmbMedico.SelectedIndex = 0;
            }
            else
            {
                _lblAviso.Text = Localizacion("agenda.sinMedicos");
            }
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            var paciente = _cmbPaciente.SelectedItem as OpcionPaciente;
            var medico = _cmbMedico.SelectedItem as OpcionMedico;
            if (paciente == null || medico == null)
            {
                _lblAviso.Text = Localizacion("agenda.sinMedicos");
                return;
            }

            try
            {
                using var contexto = new NegocioDbContext();
                var turno = new Turno
                {
                    IdPaciente = paciente.Id,
                    IdUsuarioMedico = medico.Id,
                    FechaHora = _dtpFecha.Value.Date.Add(_dtpHora.Value.TimeOfDay),
                    Observaciones = string.IsNullOrWhiteSpace(_txtObservaciones.Text) ? null : _txtObservaciones.Text.Trim()
                };

                new TurnoLogic(contexto).AgendarTurno(turno, _usuarioResponsable);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (ValidacionNegocioException ex)
            {
                _lblAviso.Text = string.Join(Environment.NewLine, ex.Errores);
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "AgendarTurnoForm");
            }
        }
    }
}
