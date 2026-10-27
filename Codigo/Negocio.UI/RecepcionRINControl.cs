using System.Text;
using Negocio.BLL;
using Negocio.DAL.Context;
using Negocio.DomainModel;
using Negocio.DomainModel.Enums;
using Negocio.DomainModel.Exceptions;
using Services.Facade;

namespace Negocio.UI
{
    /// <summary>
    /// Recepción de RIN (REQ-FUNC-004/005): búsqueda del paciente por documento, carga manual
    /// del valor (canal digital o telefónico) y resultado inmediato del triaje: clasificación
    /// de criticidad, reclasificación por tendencia peligrosa y alerta urgente generada.
    /// </summary>
    public class RecepcionRINControl : UserControl
    {
        private readonly string _nombreUsuario;

        private readonly TextBox _txtDni = new();
        private readonly Button _btnBuscar = new();
        private readonly Label _lblIdentificacion = new();
        private readonly Label _lblAviso = new();
        private readonly Label _lblFicha = new();
        private readonly Label _lblValor = new();
        private readonly NumericUpDown _numValor = new();
        private readonly Label _lblFecha = new();
        private readonly DateTimePicker _dtpFecha = new();
        private readonly Label _lblCanal = new();
        private readonly ComboBox _cmbCanal = new();
        private readonly Button _btnReportar = new();
        private readonly Label _lblResultado = new();
        private readonly Label _lblHistorial = new();
        private readonly DataGridView _grillaHistorial = new();

        private Paciente? _pacienteActual;
        private HistoriaClinica? _historiaActual;

        public RecepcionRINControl(string nombreUsuario)
        {
            _nombreUsuario = nombreUsuario;
            ConstruirInterfaz();
            AplicarTextos();
            _lblResultado.Text = Localizacion("rin.resultadoSinDatos");
        }

        private static string Localizacion(string clave) => LocalizationService.ObtenerTexto(clave);

        /// <summary>Reaplica textos por cambio de idioma y revalida la búsqueda activa.</summary>
        public void RefrescarTextos()
        {
            AplicarTextos();
            if (_pacienteActual != null)
            {
                BuscarPaciente();
            }
        }

        private void ConstruirInterfaz()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(244, 249, 254);
            Padding = new Padding(8, 4, 8, 6);

            var colorEtiqueta = Color.FromArgb(60, 85, 115);

            // ---- Barra de identificación ----
            var panelBusqueda = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = BackColor };

            _lblIdentificacion.Location = new Point(0, 4);
            _lblIdentificacion.Size = new Size(320, 18);
            _lblIdentificacion.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _lblIdentificacion.ForeColor = colorEtiqueta;

            _txtDni.Name = "txtDni";
            _txtDni.Location = new Point(0, 26);
            _txtDni.Size = new Size(200, 28);
            _txtDni.Font = new Font("Segoe UI", 11F);
            _txtDni.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    BuscarPaciente();
                }
            };

            _btnBuscar.Name = "btnBuscar";
            _btnBuscar.Location = new Point(212, 25);
            _btnBuscar.Size = new Size(110, 30);
            _btnBuscar.FlatStyle = FlatStyle.Flat;
            _btnBuscar.BackColor = Color.FromArgb(45, 108, 223);
            _btnBuscar.ForeColor = Color.White;
            _btnBuscar.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _btnBuscar.FlatAppearance.BorderSize = 0;
            _btnBuscar.Cursor = Cursors.Hand;
            _btnBuscar.Click += (s, e) => BuscarPaciente();

            _lblAviso.Location = new Point(336, 32);
            _lblAviso.Size = new Size(700, 22);
            _lblAviso.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _lblAviso.ForeColor = Color.FromArgb(180, 40, 40);

            panelBusqueda.Controls.Add(_lblIdentificacion);
            panelBusqueda.Controls.Add(_txtDni);
            panelBusqueda.Controls.Add(_btnBuscar);
            panelBusqueda.Controls.Add(_lblAviso);

            // ---- Ficha del paciente ----
            var panelFicha = new Panel
            {
                Dock = DockStyle.Left,
                Width = 380,
                BackColor = Color.White,
                Padding = new Padding(14, 10, 14, 10)
            };
            _lblFicha.Dock = DockStyle.Fill;
            _lblFicha.Font = new Font("Segoe UI", 10.5F);
            _lblFicha.ForeColor = Color.FromArgb(40, 60, 90);
            panelFicha.Controls.Add(_lblFicha);

            // ---- Formulario de reporte + resultado + historial ----
            var panelDerecho = new Panel { Dock = DockStyle.Fill, BackColor = BackColor };

            _lblValor.Location = new Point(10, 0);
            _lblValor.Size = new Size(160, 18);
            _lblValor.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _lblValor.ForeColor = colorEtiqueta;

            _numValor.Name = "numValor";
            _numValor.Location = new Point(10, 20);
            _numValor.Size = new Size(140, 28);
            _numValor.DecimalPlaces = 2;
            _numValor.Minimum = 0.1M;
            _numValor.Maximum = 20.0M;
            _numValor.Increment = 0.1M;
            _numValor.Value = 2.5M;
            _numValor.Font = new Font("Segoe UI", 11F);

            _lblFecha.Location = new Point(170, 0);
            _lblFecha.Size = new Size(160, 18);
            _lblFecha.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _lblFecha.ForeColor = colorEtiqueta;

            _dtpFecha.Name = "dtpFecha";
            _dtpFecha.Location = new Point(170, 20);
            _dtpFecha.Size = new Size(150, 28);
            _dtpFecha.Format = DateTimePickerFormat.Short;
            _dtpFecha.Font = new Font("Segoe UI", 10F);

            _lblCanal.Location = new Point(340, 0);
            _lblCanal.Size = new Size(200, 18);
            _lblCanal.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _lblCanal.ForeColor = colorEtiqueta;

            _cmbCanal.Name = "cmbCanal";
            _cmbCanal.Location = new Point(340, 20);
            _cmbCanal.Size = new Size(220, 28);
            _cmbCanal.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbCanal.Font = new Font("Segoe UI", 10F);

            _btnReportar.Name = "btnReportar";
            _btnReportar.Location = new Point(580, 18);
            _btnReportar.Size = new Size(160, 34);
            _btnReportar.FlatStyle = FlatStyle.Flat;
            _btnReportar.BackColor = Color.FromArgb(45, 108, 223);
            _btnReportar.ForeColor = Color.White;
            _btnReportar.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _btnReportar.FlatAppearance.BorderSize = 0;
            _btnReportar.Cursor = Cursors.Hand;
            _btnReportar.Click += (s, e) => ReportarValor();

            _lblResultado.Location = new Point(10, 66);
            _lblResultado.Size = new Size(730, 96);
            _lblResultado.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            _lblResultado.ForeColor = Color.FromArgb(120, 135, 150);

            _lblHistorial.Location = new Point(10, 170);
            _lblHistorial.Size = new Size(300, 18);
            _lblHistorial.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _lblHistorial.ForeColor = colorEtiqueta;

            _grillaHistorial.Dock = DockStyle.Bottom;
            _grillaHistorial.Height = 210;
            _grillaHistorial.ReadOnly = true;
            _grillaHistorial.AllowUserToAddRows = false;
            _grillaHistorial.AllowUserToDeleteRows = false;
            _grillaHistorial.AllowUserToResizeRows = false;
            _grillaHistorial.RowHeadersVisible = false;
            _grillaHistorial.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grillaHistorial.MultiSelect = false;
            _grillaHistorial.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grillaHistorial.BackgroundColor = Color.White;
            _grillaHistorial.BorderStyle = BorderStyle.None;
            _grillaHistorial.GridColor = Color.FromArgb(225, 235, 245);
            _grillaHistorial.EnableHeadersVisualStyles = false;
            _grillaHistorial.ColumnHeadersHeight = 34;
            _grillaHistorial.RowTemplate.Height = 32;
            _grillaHistorial.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(41, 98, 176);
            _grillaHistorial.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _grillaHistorial.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _grillaHistorial.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(41, 98, 176);
            _grillaHistorial.DefaultCellStyle.Font = new Font("Segoe UI", 10.5F);
            _grillaHistorial.DefaultCellStyle.SelectionBackColor = Color.FromArgb(210, 230, 250);
            _grillaHistorial.DefaultCellStyle.SelectionForeColor = Color.Black;
            _grillaHistorial.DefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
            _grillaHistorial.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 251, 255);

            _grillaHistorial.Columns.Add("fecha", "");
            _grillaHistorial.Columns.Add("valor", "");
            _grillaHistorial.Columns.Add("canal", "");
            _grillaHistorial.Columns.Add("criticidad", "");
            _grillaHistorial.Columns["fecha"].FillWeight = 22;
            _grillaHistorial.Columns["valor"].FillWeight = 16;
            _grillaHistorial.Columns["canal"].FillWeight = 32;
            _grillaHistorial.Columns["criticidad"].FillWeight = 30;
            _grillaHistorial.Columns["valor"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _grillaHistorial.Columns["criticidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            panelDerecho.Controls.Add(_lblValor);
            panelDerecho.Controls.Add(_numValor);
            panelDerecho.Controls.Add(_lblFecha);
            panelDerecho.Controls.Add(_dtpFecha);
            panelDerecho.Controls.Add(_lblCanal);
            panelDerecho.Controls.Add(_cmbCanal);
            panelDerecho.Controls.Add(_btnReportar);
            panelDerecho.Controls.Add(_lblResultado);
            panelDerecho.Controls.Add(_lblHistorial);
            panelDerecho.Controls.Add(_grillaHistorial);

            Controls.Add(panelDerecho);
            Controls.Add(panelFicha);
            Controls.Add(panelBusqueda);
        }

        private void AplicarTextos()
        {
            _lblIdentificacion.Text = Localizacion("rin.identificacion");
            _btnBuscar.Text = Localizacion("comun.buscar");
            _lblValor.Text = Localizacion("rin.valor");
            _lblFecha.Text = Localizacion("rin.fecha");
            _lblCanal.Text = Localizacion("rin.canal");
            _btnReportar.Text = Localizacion("rin.reportar");
            _lblHistorial.Text = Localizacion("rin.historial");

            int seleccionado = _cmbCanal.SelectedIndex < 0 ? 0 : _cmbCanal.SelectedIndex;
            _cmbCanal.Items.Clear();
            _cmbCanal.Items.Add(Localizacion("rin.canal.digital"));
            _cmbCanal.Items.Add(Localizacion("rin.canal.telefonico"));
            _cmbCanal.SelectedIndex = seleccionado;

            _grillaHistorial.Columns["fecha"].HeaderText = Localizacion("rin.columna.fecha");
            _grillaHistorial.Columns["valor"].HeaderText = Localizacion("rin.columna.valor");
            _grillaHistorial.Columns["canal"].HeaderText = Localizacion("rin.columna.canal");
            _grillaHistorial.Columns["criticidad"].HeaderText = Localizacion("panel.columna.criticidad");
        }

        private string CriticidadTexto(NivelCriticidad nivel) => nivel switch
        {
            NivelCriticidad.Alta => Localizacion("panel.criticidadAlta"),
            NivelCriticidad.Media => Localizacion("panel.criticidadMedia"),
            _ => Localizacion("panel.criticidadBaja")
        };

        private string CanalTexto(CanalReporteRIN canal)
            => canal == CanalReporteRIN.Telefonico ? Localizacion("rin.canal.telefonico") : Localizacion("rin.canal.digital");

        private void BuscarPaciente()
        {
            try
            {
                using var contexto = new NegocioDbContext();
                _pacienteActual = new PacienteLogic(contexto).BuscarPorDNI(_txtDni.Text.Trim());
                if (_pacienteActual == null)
                {
                    _historiaActual = null;
                    _lblFicha.Text = string.Empty;
                    _lblAviso.Text = Localizacion("rin.pacienteNoEncontrado");
                    _grillaHistorial.Rows.Clear();
                    ActualizarHabilitacion();
                    return;
                }

                _historiaActual = new HistoriaClinicaLogic(contexto).ObtenerPorPaciente(_pacienteActual.Id);
                MedicionRIN? ultima = new MedicionRINLogic(contexto).ObtenerUltima(_pacienteActual.Id);
                MostrarFicha(_pacienteActual, _historiaActual, ultima);
                ActualizarHabilitacion();
                CargarHistorial();
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "RecepcionRIN", mostrarMensaje: false);
                _lblAviso.Text = Localizacion("error.generico");
            }
        }

        private void MostrarFicha(Paciente paciente, HistoriaClinica? hc, MedicionRIN? ultima)
        {
            _lblAviso.Text = string.Empty;
            var texto = new StringBuilder();
            texto.AppendLine($"👤 {paciente.NombreCompleto}");
            texto.AppendLine($"{Localizacion("pacientes.documento")}: {paciente.DNI}");
            texto.AppendLine($"{Localizacion("pacientes.estado")}: " +
                (paciente.Estado == EstadoPaciente.Activo ? Localizacion("pacientes.estadoActivo") : Localizacion("pacientes.estadoInactivo")));
            texto.AppendLine();
            texto.AppendLine($"📋 {Localizacion("hc.titulo")}");
            if (hc == null)
            {
                texto.AppendLine($"— {Localizacion("hc.sinConfigurar")}");
            }
            else
            {
                texto.AppendLine($"{Localizacion("rin.rango")}: {hc.LimiteInferiorRIN:0.00} – {hc.LimiteSuperiorRIN:0.00}");
                texto.AppendLine($"{Localizacion("hc.medicamento")}: {hc.Medicamento} — {hc.Dosis}");
                texto.AppendLine($"{Localizacion("hc.periodicidad")}: {hc.PeriodicidadDias}");
            }
            texto.AppendLine();
            texto.AppendLine($"📈 {Localizacion("rin.ultimaMedicion")}");
            if (ultima == null)
            {
                texto.AppendLine($"— {Localizacion("rin.sinMediciones")}");
            }
            else
            {
                string criticidad = ultima.NivelCriticidad == null
                    ? Localizacion("panel.sinDatos")
                    : CriticidadTexto(ultima.NivelCriticidad.Value);
                texto.AppendLine($"RIN {ultima.ValorRIN:0.00} · {ultima.FechaMedicion:dd/MM/yyyy} · {criticidad}" +
                                 (ultima.PorTendenciaPeligrosa ? " ⚠" : string.Empty));
            }
            _lblFicha.Text = texto.ToString();
        }

        private void ActualizarHabilitacion()
        {
            bool activo = _pacienteActual?.Estado == EstadoPaciente.Activo;
            bool conHistoria = _historiaActual != null;
            bool habilitado = _pacienteActual != null && activo && conHistoria;

            _numValor.Enabled = habilitado;
            _dtpFecha.Enabled = habilitado;
            _cmbCanal.Enabled = habilitado;
            _btnReportar.Enabled = habilitado;

            if (_pacienteActual != null && !activo)
            {
                _lblAviso.Text = Localizacion("rin.pacienteInactivo");
            }
            else if (_pacienteActual != null && !conHistoria)
            {
                _lblAviso.Text = Localizacion("rin.sinHistoria");
            }
        }

        private void ReportarValor()
        {
            if (_pacienteActual == null || _historiaActual == null)
            {
                return;
            }

            try
            {
                using var contexto = new NegocioDbContext();
                var medicion = new MedicionRIN
                {
                    IdPaciente = _pacienteActual.Id,
                    ValorRIN = _numValor.Value,
                    FechaMedicion = _dtpFecha.Value.Date,
                    Canal = _cmbCanal.SelectedIndex == 1 ? CanalReporteRIN.Telefonico : CanalReporteRIN.Digital
                };

                ResultadoReporteRIN resultado = new MedicionRINLogic(contexto).Reportar(medicion, _nombreUsuario);
                MostrarResultado(resultado);

                MedicionRIN? ultima = new MedicionRINLogic(contexto).ObtenerUltima(_pacienteActual.Id);
                MostrarFicha(_pacienteActual, _historiaActual, ultima);
                CargarHistorial();
            }
            catch (ValidacionNegocioException ex)
            {
                _lblResultado.ForeColor = Color.FromArgb(180, 40, 40);
                _lblResultado.Text = string.Join(Environment.NewLine, ex.Errores);
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "RecepcionRIN");
            }
        }

        private void MostrarResultado(ResultadoReporteRIN resultado)
        {
            var texto = new StringBuilder();
            texto.AppendLine($"{Localizacion("rin.resultado")}: RIN {resultado.Medicion.ValorRIN:0.00} — " +
                             $"{CriticidadTexto(resultado.Medicion.NivelCriticidad ?? NivelCriticidad.Baja)}");
            if (resultado.Medicion.PorTendenciaPeligrosa)
            {
                texto.AppendLine(Localizacion("rin.tendencia"));
            }
            if (resultado.AlertaGenerada != null)
            {
                texto.AppendLine(Localizacion("rin.alertaGenerada"));
            }

            _lblResultado.Text = texto.ToString().TrimEnd();
            _lblResultado.ForeColor = resultado.Medicion.NivelCriticidad switch
            {
                NivelCriticidad.Alta => Color.FromArgb(180, 30, 30),
                NivelCriticidad.Media => Color.FromArgb(190, 120, 20),
                _ => Color.FromArgb(30, 110, 60)
            };
        }

        private void CargarHistorial()
        {
            _grillaHistorial.Rows.Clear();
            if (_pacienteActual == null)
            {
                return;
            }

            using var contexto = new NegocioDbContext();
            var historial = new MedicionRINLogic(contexto).ObtenerHistorial(_pacienteActual.Id).Take(10).ToList();
            foreach (MedicionRIN medicion in historial)
            {
                string criticidad = medicion.NivelCriticidad == null
                    ? Localizacion("panel.sinDatos")
                    : CriticidadTexto(medicion.NivelCriticidad.Value) + (medicion.PorTendenciaPeligrosa ? " ⚠" : string.Empty);

                int indice = _grillaHistorial.Rows.Add(
                    medicion.FechaMedicion.ToString("dd/MM/yyyy"),
                    medicion.ValorRIN.ToString("0.00"),
                    CanalTexto(medicion.Canal),
                    criticidad);

                DataGridViewCell celda = _grillaHistorial.Rows[indice].Cells[3];
                (Color fondo, Color texto) = medicion.NivelCriticidad switch
                {
                    NivelCriticidad.Alta => (Color.FromArgb(200, 45, 45), Color.White),
                    NivelCriticidad.Media => (Color.FromArgb(232, 163, 61), Color.White),
                    NivelCriticidad.Baja => (Color.FromArgb(46, 125, 70), Color.White),
                    _ => (Color.FromArgb(130, 140, 150), Color.White)
                };
                celda.Style.BackColor = fondo;
                celda.Style.ForeColor = texto;
                celda.Style.SelectionBackColor = fondo;
                celda.Style.SelectionForeColor = texto;
                celda.Style.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            }
        }
    }
}
