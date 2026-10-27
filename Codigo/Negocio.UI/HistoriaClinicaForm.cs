using Microsoft.EntityFrameworkCore;
using Negocio.BLL;
using Negocio.DAL.Context;
using Negocio.DomainModel;
using Negocio.DomainModel.Exceptions;
using Services.Facade;

namespace Negocio.UI
{
    /// <summary>
    /// Historia clínica hematológica de un paciente (REQ-FUNC-002): creación/actualización por
    /// perfiles con permiso HISTORIA_CLINICA, o vista de solo lectura para el resto.
    /// </summary>
    public class HistoriaClinicaForm : Form
    {
        private sealed class OpcionDiagnostico
        {
            public int? Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public override string ToString() => Nombre;
        }

        private readonly Paciente _paciente;
        private readonly string _usuarioResponsable;
        private readonly bool _puedeEditar;

        private readonly ComboBox _cmbDiagnostico = new();
        private readonly NumericUpDown _numInferior = new();
        private readonly NumericUpDown _numSuperior = new();
        private readonly TextBox _txtMedicamento = new();
        private readonly TextBox _txtDosis = new();
        private readonly NumericUpDown _numPeriodicidad = new();
        private readonly CheckBox _chkProximoControl = new();
        private readonly DateTimePicker _dtpProximoControl = new();
        private readonly Label _lblAviso = new();

        public HistoriaClinicaForm(Paciente paciente, string usuarioResponsable, bool puedeEditar)
        {
            _paciente = paciente;
            _usuarioResponsable = usuarioResponsable;
            _puedeEditar = puedeEditar;

            Text = $"OpenRIN — {Localizacion("hc.titulo")} — {paciente.NombreCompleto}";
            ClientSize = new Size(520, 430);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Color.White;

            ConstruirInterfaz();
            Cargar();
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
            _lblAviso.Location = new Point(18, 12);
            _lblAviso.Size = new Size(484, 36);
            _lblAviso.Font = new Font("Segoe UI", 9F);
            _lblAviso.ForeColor = Color.FromArgb(140, 80, 20);

            Controls.Add(Etiqueta("hc.diagnostico", 18, 52));
            _cmbDiagnostico.Location = new Point(18, 72);
            _cmbDiagnostico.Size = new Size(484, 28);
            _cmbDiagnostico.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbDiagnostico.Font = new Font("Segoe UI", 10F);

            Controls.Add(Etiqueta("hc.limiteInferior", 18, 112));
            _numInferior.Location = new Point(18, 132);
            _numInferior.Size = new Size(200, 28);
            _numInferior.DecimalPlaces = 2;
            _numInferior.Minimum = 0.1M;
            _numInferior.Maximum = 20.0M;
            _numInferior.Increment = 0.1M;
            _numInferior.Value = 2.0M;
            _numInferior.Font = new Font("Segoe UI", 10.5F);

            Controls.Add(Etiqueta("hc.limiteSuperior", 264, 112));
            _numSuperior.Location = new Point(264, 132);
            _numSuperior.Size = new Size(238, 28);
            _numSuperior.DecimalPlaces = 2;
            _numSuperior.Minimum = 0.1M;
            _numSuperior.Maximum = 20.0M;
            _numSuperior.Increment = 0.1M;
            _numSuperior.Value = 3.0M;
            _numSuperior.Font = new Font("Segoe UI", 10.5F);

            Controls.Add(Etiqueta("hc.medicamento", 18, 172));
            _txtMedicamento.Location = new Point(18, 192);
            _txtMedicamento.Size = new Size(238, 28);
            _txtMedicamento.Font = new Font("Segoe UI", 10.5F);

            Controls.Add(Etiqueta("hc.dosis", 264, 172));
            _txtDosis.Location = new Point(264, 192);
            _txtDosis.Size = new Size(238, 28);
            _txtDosis.Font = new Font("Segoe UI", 10.5F);

            Controls.Add(Etiqueta("hc.periodicidad", 18, 232));
            _numPeriodicidad.Location = new Point(18, 252);
            _numPeriodicidad.Size = new Size(160, 28);
            _numPeriodicidad.Minimum = 1;
            _numPeriodicidad.Maximum = 365;
            _numPeriodicidad.Value = 30;
            _numPeriodicidad.Font = new Font("Segoe UI", 10.5F);

            _chkProximoControl.Location = new Point(264, 234);
            _chkProximoControl.Size = new Size(238, 22);
            _chkProximoControl.Font = new Font("Segoe UI", 9.5F);
            _chkProximoControl.CheckedChanged += (s, e) => _dtpProximoControl.Enabled = _chkProximoControl.Checked && _puedeEditar;

            _dtpProximoControl.Location = new Point(264, 256);
            _dtpProximoControl.Size = new Size(160, 28);
            _dtpProximoControl.Format = DateTimePickerFormat.Short;
            _dtpProximoControl.Enabled = false;
            _dtpProximoControl.Font = new Font("Segoe UI", 10F);

            var btnGuardar = new Button
            {
                Text = Localizacion("comun.guardar"),
                Location = new Point(316, 320),
                Size = new Size(90, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 108, 223),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            var btnCerrar = new Button
            {
                Text = Localizacion("comun.cerrar"),
                Location = new Point(414, 320),
                Size = new Size(90, 36),
                FlatStyle = FlatStyle.Flat
            };
            btnCerrar.Click += (s, e) => { DialogResult = DialogResult.Cancel; };

            AcceptButton = btnGuardar;
            CancelButton = btnCerrar;

            Controls.AddRange(new Control[]
            {
                _lblAviso, _cmbDiagnostico, _numInferior, _numSuperior, _txtMedicamento, _txtDosis,
                _numPeriodicidad, _chkProximoControl, _dtpProximoControl, btnGuardar, btnCerrar
            });

            if (!_puedeEditar)
            {
                _cmbDiagnostico.Enabled = false;
                _numInferior.Enabled = false;
                _numSuperior.Enabled = false;
                _txtMedicamento.ReadOnly = true;
                _txtDosis.ReadOnly = true;
                _numPeriodicidad.Enabled = false;
                _chkProximoControl.Enabled = false;
                _dtpProximoControl.Enabled = false;
                btnGuardar.Visible = false;
                _lblAviso.Text = Localizacion("hc.soloLectura");
            }
        }

        private void Cargar()
        {
            using var contexto = new NegocioDbContext();

            var diagnosticos = contexto.Diagnosticos.AsNoTracking()
                .Where(d => d.Activo)
                .OrderBy(d => d.Nombre)
                .ToList();
            _cmbDiagnostico.Items.Add(new OpcionDiagnostico { Id = null, Nombre = Localizacion("hc.sinDiagnostico") });
            foreach (Diagnostico diagnostico in diagnosticos)
            {
                _cmbDiagnostico.Items.Add(new OpcionDiagnostico { Id = diagnostico.Id, Nombre = diagnostico.Nombre });
            }
            _cmbDiagnostico.SelectedIndex = 0;

            HistoriaClinica? hc = new HistoriaClinicaLogic(contexto).ObtenerPorPaciente(_paciente.Id);
            if (hc == null)
            {
                if (_puedeEditar)
                {
                    _lblAviso.Text = Localizacion("hc.sinConfigurar");
                }
                return;
            }

            foreach (OpcionDiagnostico? opcion in _cmbDiagnostico.Items)
            {
                if (opcion != null && opcion.Id == hc.IdDiagnostico)
                {
                    _cmbDiagnostico.SelectedItem = opcion;
                    break;
                }
            }
            _numInferior.Value = Math.Clamp(hc.LimiteInferiorRIN, _numInferior.Minimum, _numInferior.Maximum);
            _numSuperior.Value = Math.Clamp(hc.LimiteSuperiorRIN, _numSuperior.Minimum, _numSuperior.Maximum);
            _txtMedicamento.Text = hc.Medicamento;
            _txtDosis.Text = hc.Dosis;
            _numPeriodicidad.Value = Math.Clamp(hc.PeriodicidadDias, 1, 365);
            if (hc.ProximaFechaControl.HasValue)
            {
                _chkProximoControl.Checked = true;
                _dtpProximoControl.Value = hc.ProximaFechaControl.Value;
            }
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            try
            {
                using var contexto = new NegocioDbContext();
                var hc = new HistoriaClinica
                {
                    IdPaciente = _paciente.Id,
                    IdDiagnostico = (_cmbDiagnostico.SelectedItem as OpcionDiagnostico)?.Id,
                    LimiteInferiorRIN = _numInferior.Value,
                    LimiteSuperiorRIN = _numSuperior.Value,
                    Medicamento = _txtMedicamento.Text.Trim(),
                    Dosis = _txtDosis.Text.Trim(),
                    PeriodicidadDias = (int)_numPeriodicidad.Value,
                    ProximaFechaControl = _chkProximoControl.Checked ? _dtpProximoControl.Value.Date : null
                };

                new HistoriaClinicaLogic(contexto).Configurar(hc, _usuarioResponsable);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (ValidacionNegocioException ex)
            {
                MessageBox.Show(this, string.Join(Environment.NewLine, ex.Errores),
                    Localizacion("error.titulo"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "HistoriaClinicaForm");
            }
        }
    }
}
