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
    /// Seguimiento clínico (REQ-FUNC-010): casos con reporte vencido (alertas de ausencia
    /// activas), protocolo progresivo de contacto (umbral de intentos fallidos → escalado al
    /// médico) y registro de decisiones clínicas (que pueden actualizar la periodicidad de la HC).
    /// </summary>
    public class SeguimientoControl : UserControl
    {
        /// <summary>Fila del listado de casos en seguimiento.</summary>
        private sealed class FilaCaso
        {
            public int IdPaciente { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public DateTime? UltimoReporte { get; set; }
            public int DiasVencido { get; set; }
            public int Intentos { get; set; }
            public bool Escalada { get; set; }
        }

        private readonly int _idUsuario;
        private readonly string _nombreUsuario;
        private readonly List<FilaCaso> _casos = new();

        private readonly Button _btnDetectar = new();
        private readonly DataGridView _grillaCasos = new();
        private readonly Label _lblSinCasos = new();

        private readonly Panel _panelDetalle = new();
        private readonly Label _lblPacienteDetalle = new();
        private readonly Label _lblProtocolo = new();
        private readonly Label _lblContactoSeccion = new();
        private readonly Label _lblTipoContacto = new();
        private readonly ComboBox _cmbTipoContacto = new();
        private readonly Label _lblResultado = new();
        private readonly ComboBox _cmbResultado = new();
        private readonly Label _lblObservaciones = new();
        private readonly TextBox _txtObservaciones = new();
        private readonly Button _btnRegistrarIntento = new();
        private readonly Label _lblAviso = new();
        private readonly Label _lblDecisionSeccion = new();
        private readonly Label _lblDecision = new();
        private readonly ComboBox _cmbDecision = new();
        private readonly Label _lblPeriodicidad = new();
        private readonly NumericUpDown _numPeriodicidad = new();
        private readonly Label _lblDetalleDecision = new();
        private readonly TextBox _txtDetalleDecision = new();
        private readonly Button _btnRegistrarDecision = new();
        private readonly Label _lblHistorial = new();
        private readonly DataGridView _grillaHistorial = new();

        private int _idPacienteSeleccionado;
        private bool _recargando;

        public SeguimientoControl(int idUsuario, string nombreUsuario)
        {
            _idUsuario = idUsuario;
            _nombreUsuario = nombreUsuario;

            ConstruirInterfaz();
            AplicarTextos();
            Recargar();
        }

        private static string Localizacion(string clave) => LocalizationService.ObtenerTexto(clave);

        /// <summary>Recarga el listado de casos y el detalle (al entrar al módulo).</summary>
        public void Recargar()
        {
            try
            {
                _recargando = true;
                CargarCasos();
            }
            finally
            {
                _recargando = false;
            }
        }

        /// <summary>Reaplica textos por cambio de idioma y recarga.</summary>
        public void RefrescarTextos()
        {
            AplicarTextos();
            Recargar();
        }

        private void ConstruirInterfaz()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(244, 249, 254);
            Padding = new Padding(8, 4, 8, 6);

            // ---- Barra superior ----
            var panelBarra = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = BackColor };
            _btnDetectar.Location = new Point(0, 15);
            _btnDetectar.Size = new Size(230, 32);
            _btnDetectar.FlatStyle = FlatStyle.Flat;
            _btnDetectar.BackColor = Color.FromArgb(45, 108, 223);
            _btnDetectar.ForeColor = Color.White;
            _btnDetectar.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _btnDetectar.FlatAppearance.BorderSize = 0;
            _btnDetectar.Cursor = Cursors.Hand;
            _btnDetectar.Click += (s, e) => DetectarPlazos();
            panelBarra.Controls.Add(_btnDetectar);

            // ---- Grilla de casos ----
            _grillaCasos.Dock = DockStyle.Fill;
            EstiloGrilla(_grillaCasos);
            _grillaCasos.Columns.Add("paciente", "");
            _grillaCasos.Columns.Add("ultimo", "");
            _grillaCasos.Columns.Add("dias", "");
            _grillaCasos.Columns.Add("intentos", "");
            _grillaCasos.Columns.Add("protocolo", "");
            _grillaCasos.Columns["paciente"].FillWeight = 30;
            _grillaCasos.Columns["ultimo"].FillWeight = 18;
            _grillaCasos.Columns["dias"].FillWeight = 12;
            _grillaCasos.Columns["intentos"].FillWeight = 12;
            _grillaCasos.Columns["protocolo"].FillWeight = 28;
            foreach (string columna in new[] { "dias", "intentos", "protocolo" })
            {
                _grillaCasos.Columns[columna].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            _grillaCasos.SelectionChanged += (s, e) =>
            {
                if (_recargando || _grillaCasos.SelectedRows.Count == 0)
                {
                    return;
                }
                int indice = _grillaCasos.SelectedRows[0].Index;
                if (indice >= 0 && indice < _casos.Count)
                {
                    CargarDetalle(_casos[indice].IdPaciente);
                }
            };

            _lblSinCasos.Dock = DockStyle.Bottom;
            _lblSinCasos.Height = 24;
            _lblSinCasos.Font = new Font("Segoe UI", 9.5F);
            _lblSinCasos.ForeColor = Color.FromArgb(150, 100, 40);

            // ---- Panel de detalle ----
            _panelDetalle.Dock = DockStyle.Right;
            _panelDetalle.Width = 520;
            _panelDetalle.BackColor = Color.White;
            _panelDetalle.Padding = new Padding(12, 10, 12, 10);

            var colorEtiqueta = Color.FromArgb(60, 85, 115);
            Font fuenteEtiqueta = new("Segoe UI", 9F, FontStyle.Bold);

            _lblPacienteDetalle.Location = new Point(14, 10);
            _lblPacienteDetalle.Size = new Size(480, 24);
            _lblPacienteDetalle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            _lblPacienteDetalle.ForeColor = Color.FromArgb(30, 66, 120);

            _lblProtocolo.Location = new Point(14, 36);
            _lblProtocolo.Size = new Size(480, 20);
            _lblProtocolo.Font = new Font("Segoe UI", 9.5F);
            _lblProtocolo.ForeColor = colorEtiqueta;

            _lblContactoSeccion.Location = new Point(14, 64);
            _lblContactoSeccion.Size = new Size(480, 20);
            _lblContactoSeccion.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _lblContactoSeccion.ForeColor = Color.FromArgb(41, 98, 176);

            _lblTipoContacto.Location = new Point(14, 86);
            _lblTipoContacto.Size = new Size(220, 16);
            _lblTipoContacto.Font = fuenteEtiqueta;
            _lblTipoContacto.ForeColor = colorEtiqueta;

            _cmbTipoContacto.Location = new Point(14, 104);
            _cmbTipoContacto.Size = new Size(220, 28);
            _cmbTipoContacto.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbTipoContacto.Font = new Font("Segoe UI", 9.5F);

            _lblResultado.Location = new Point(250, 86);
            _lblResultado.Size = new Size(240, 16);
            _lblResultado.Font = fuenteEtiqueta;
            _lblResultado.ForeColor = colorEtiqueta;

            _cmbResultado.Location = new Point(250, 104);
            _cmbResultado.Size = new Size(240, 28);
            _cmbResultado.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbResultado.Font = new Font("Segoe UI", 9.5F);

            _lblObservaciones.Location = new Point(14, 136);
            _lblObservaciones.Size = new Size(476, 16);
            _lblObservaciones.Font = fuenteEtiqueta;
            _lblObservaciones.ForeColor = colorEtiqueta;

            _txtObservaciones.Name = "txtObservaciones";
            _txtObservaciones.Location = new Point(14, 154);
            _txtObservaciones.Size = new Size(476, 26);
            _txtObservaciones.Font = new Font("Segoe UI", 9.5F);

            _btnRegistrarIntento.Location = new Point(14, 188);
            _btnRegistrarIntento.Size = new Size(200, 34);
            _btnRegistrarIntento.FlatStyle = FlatStyle.Flat;
            _btnRegistrarIntento.BackColor = Color.FromArgb(45, 108, 223);
            _btnRegistrarIntento.ForeColor = Color.White;
            _btnRegistrarIntento.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _btnRegistrarIntento.FlatAppearance.BorderSize = 0;
            _btnRegistrarIntento.Cursor = Cursors.Hand;
            _btnRegistrarIntento.Click += (s, e) => RegistrarIntento();

            _lblAviso.Location = new Point(224, 190);
            _lblAviso.Size = new Size(266, 44);
            _lblAviso.Font = new Font("Segoe UI", 8.5F);
            _lblAviso.ForeColor = Color.FromArgb(180, 40, 40);

            _lblDecisionSeccion.Location = new Point(14, 234);
            _lblDecisionSeccion.Size = new Size(480, 20);
            _lblDecisionSeccion.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _lblDecisionSeccion.ForeColor = Color.FromArgb(41, 98, 176);

            _lblDecision.Location = new Point(14, 256);
            _lblDecision.Size = new Size(240, 16);
            _lblDecision.Font = fuenteEtiqueta;
            _lblDecision.ForeColor = colorEtiqueta;

            _cmbDecision.Location = new Point(14, 274);
            _cmbDecision.Size = new Size(240, 28);
            _cmbDecision.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbDecision.Font = new Font("Segoe UI", 9.5F);
            _cmbDecision.SelectedIndexChanged += (s, e) => ActualizarPeriodicidadHabilitada();

            _lblPeriodicidad.Location = new Point(270, 256);
            _lblPeriodicidad.Size = new Size(220, 16);
            _lblPeriodicidad.Font = fuenteEtiqueta;
            _lblPeriodicidad.ForeColor = colorEtiqueta;

            _numPeriodicidad.Location = new Point(270, 274);
            _numPeriodicidad.Size = new Size(100, 28);
            _numPeriodicidad.Minimum = 1;
            _numPeriodicidad.Maximum = 365;
            _numPeriodicidad.Value = 30;
            _numPeriodicidad.Font = new Font("Segoe UI", 9.5F);
            _numPeriodicidad.Enabled = false;

            _lblDetalleDecision.Location = new Point(14, 306);
            _lblDetalleDecision.Size = new Size(476, 16);
            _lblDetalleDecision.Font = fuenteEtiqueta;
            _lblDetalleDecision.ForeColor = colorEtiqueta;

            _txtDetalleDecision.Name = "txtDetalleDecision";
            _txtDetalleDecision.Location = new Point(14, 324);
            _txtDetalleDecision.Size = new Size(476, 26);
            _txtDetalleDecision.Font = new Font("Segoe UI", 9.5F);

            _btnRegistrarDecision.Location = new Point(14, 358);
            _btnRegistrarDecision.Size = new Size(200, 34);
            _btnRegistrarDecision.FlatStyle = FlatStyle.Flat;
            _btnRegistrarDecision.BackColor = Color.FromArgb(45, 108, 223);
            _btnRegistrarDecision.ForeColor = Color.White;
            _btnRegistrarDecision.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _btnRegistrarDecision.FlatAppearance.BorderSize = 0;
            _btnRegistrarDecision.Cursor = Cursors.Hand;
            _btnRegistrarDecision.Click += (s, e) => RegistrarDecision();

            _lblHistorial.Location = new Point(14, 398);
            _lblHistorial.Size = new Size(480, 20);
            _lblHistorial.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _lblHistorial.ForeColor = Color.FromArgb(41, 98, 176);

            _grillaHistorial.Dock = DockStyle.Bottom;
            _grillaHistorial.Height = 196;
            EstiloGrilla(_grillaHistorial);
            _grillaHistorial.Columns.Add("fecha", "");
            _grillaHistorial.Columns.Add("tipo", "");
            _grillaHistorial.Columns.Add("resultado", "");
            _grillaHistorial.Columns.Add("decision", "");
            _grillaHistorial.Columns.Add("observaciones", "");
            _grillaHistorial.Columns["fecha"].FillWeight = 24;
            _grillaHistorial.Columns["tipo"].FillWeight = 16;
            _grillaHistorial.Columns["resultado"].FillWeight = 16;
            _grillaHistorial.Columns["decision"].FillWeight = 20;
            _grillaHistorial.Columns["observaciones"].FillWeight = 24;
            // Las celdas de texto largo crecen en alto para mostrar el contenido completo.
            _grillaHistorial.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            _grillaHistorial.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            _panelDetalle.Controls.Add(_grillaHistorial);
            _panelDetalle.Controls.Add(_lblPacienteDetalle);
            _panelDetalle.Controls.Add(_lblProtocolo);
            _panelDetalle.Controls.Add(_lblContactoSeccion);
            _panelDetalle.Controls.Add(_lblTipoContacto);
            _panelDetalle.Controls.Add(_cmbTipoContacto);
            _panelDetalle.Controls.Add(_lblResultado);
            _panelDetalle.Controls.Add(_cmbResultado);
            _panelDetalle.Controls.Add(_lblObservaciones);
            _panelDetalle.Controls.Add(_txtObservaciones);
            _panelDetalle.Controls.Add(_btnRegistrarIntento);
            _panelDetalle.Controls.Add(_lblAviso);
            _panelDetalle.Controls.Add(_lblDecisionSeccion);
            _panelDetalle.Controls.Add(_lblDecision);
            _panelDetalle.Controls.Add(_cmbDecision);
            _panelDetalle.Controls.Add(_lblPeriodicidad);
            _panelDetalle.Controls.Add(_numPeriodicidad);
            _panelDetalle.Controls.Add(_lblDetalleDecision);
            _panelDetalle.Controls.Add(_txtDetalleDecision);
            _panelDetalle.Controls.Add(_btnRegistrarDecision);
            _panelDetalle.Controls.Add(_lblHistorial);

            Controls.Add(_grillaCasos);
            Controls.Add(_lblSinCasos);
            Controls.Add(_panelDetalle);
            Controls.Add(panelBarra);
        }

        private static void EstiloGrilla(DataGridView grilla)
        {
            grilla.ReadOnly = true;
            grilla.AllowUserToAddRows = false;
            grilla.AllowUserToDeleteRows = false;
            grilla.AllowUserToResizeRows = false;
            grilla.RowHeadersVisible = false;
            grilla.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grilla.MultiSelect = false;
            grilla.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grilla.BackgroundColor = Color.White;
            grilla.BorderStyle = BorderStyle.None;
            grilla.GridColor = Color.FromArgb(225, 235, 245);
            grilla.EnableHeadersVisualStyles = false;
            grilla.ColumnHeadersHeight = 34;
            grilla.RowTemplate.Height = 32;
            grilla.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(41, 98, 176);
            grilla.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grilla.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            grilla.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(41, 98, 176);
            grilla.DefaultCellStyle.Font = new Font("Segoe UI", 10F);
            grilla.DefaultCellStyle.SelectionBackColor = Color.FromArgb(210, 230, 250);
            grilla.DefaultCellStyle.SelectionForeColor = Color.Black;
            grilla.DefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
            grilla.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 251, 255);
        }

        private void AplicarTextos()
        {
            _btnDetectar.Text = Localizacion("seguimiento.detectar");
            _grillaCasos.Columns["paciente"].HeaderText = Localizacion("panel.columna.paciente");
            _grillaCasos.Columns["ultimo"].HeaderText = Localizacion("seguimiento.columna.ultimoReporte");
            _grillaCasos.Columns["dias"].HeaderText = Localizacion("seguimiento.columna.diasVencido");
            _grillaCasos.Columns["intentos"].HeaderText = Localizacion("seguimiento.columna.intentos");
            _grillaCasos.Columns["protocolo"].HeaderText = Localizacion("seguimiento.columna.protocolo");

            _lblContactoSeccion.Text = Localizacion("seguimiento.contacto");
            _lblTipoContacto.Text = Localizacion("seguimiento.tipoContacto");
            _lblResultado.Text = Localizacion("seguimiento.resultado");
            _lblObservaciones.Text = Localizacion("agenda.columna.observaciones");
            _btnRegistrarIntento.Text = Localizacion("seguimiento.registrarIntento");

            int tipoPrevio = _cmbTipoContacto.SelectedIndex;
            _cmbTipoContacto.Items.Clear();
            _cmbTipoContacto.Items.Add(Localizacion("seguimiento.contacto.llamado"));
            _cmbTipoContacto.Items.Add(Localizacion("seguimiento.contacto.mensaje"));
            _cmbTipoContacto.Items.Add(Localizacion("seguimiento.contacto.correo"));
            _cmbTipoContacto.Items.Add(Localizacion("seguimiento.contacto.otro"));
            _cmbTipoContacto.SelectedIndex = tipoPrevio < 0 ? 0 : tipoPrevio;

            int resultadoPrevio = _cmbResultado.SelectedIndex;
            _cmbResultado.Items.Clear();
            _cmbResultado.Items.Add(Localizacion("seguimiento.resultado.exitoso"));
            _cmbResultado.Items.Add(Localizacion("seguimiento.resultado.fallido"));
            // Por defecto 'Fallido': el caso típico del protocolo es la llamada sin respuesta.
            _cmbResultado.SelectedIndex = resultadoPrevio < 0 ? 1 : resultadoPrevio;

            _lblDecisionSeccion.Text = Localizacion("seguimiento.decision");
            _lblDecision.Text = Localizacion("seguimiento.decision");
            _lblPeriodicidad.Text = Localizacion("seguimiento.nuevaPeriodicidad");
            _lblDetalleDecision.Text = Localizacion("seguimiento.detalleDecision");
            _btnRegistrarDecision.Text = Localizacion("seguimiento.registrarDecision");

            int decisionPrevia = _cmbDecision.SelectedIndex;
            _cmbDecision.Items.Clear();
            _cmbDecision.Items.Add(Localizacion("seguimiento.dec.contactar"));
            _cmbDecision.Items.Add(Localizacion("seguimiento.dec.visita"));
            _cmbDecision.Items.Add(Localizacion("seguimiento.dec.periodicidad"));
            _cmbDecision.Items.Add(Localizacion("seguimiento.dec.nocontactable"));
            _cmbDecision.SelectedIndex = decisionPrevia < 0 ? 0 : decisionPrevia;

            _lblHistorial.Text = Localizacion("seguimiento.historial");
            _grillaHistorial.Columns["fecha"].HeaderText = Localizacion("rin.columna.fecha");
            _grillaHistorial.Columns["tipo"].HeaderText = Localizacion("seguimiento.tipoContacto");
            _grillaHistorial.Columns["resultado"].HeaderText = Localizacion("seguimiento.resultado");
            _grillaHistorial.Columns["decision"].HeaderText = Localizacion("seguimiento.columna.decision");
            _grillaHistorial.Columns["observaciones"].HeaderText = Localizacion("agenda.columna.observaciones");
        }

        private string TipoTexto(TipoContacto tipo) => tipo switch
        {
            TipoContacto.Llamado => Localizacion("seguimiento.contacto.llamado"),
            TipoContacto.Mensaje => Localizacion("seguimiento.contacto.mensaje"),
            TipoContacto.Correo => Localizacion("seguimiento.contacto.correo"),
            _ => Localizacion("seguimiento.contacto.otro")
        };

        private string ResultadoTexto(ResultadoContacto resultado)
            => Localizacion(resultado == ResultadoContacto.Exitoso ? "seguimiento.resultado.exitoso" : "seguimiento.resultado.fallido");

        private string DecisionTexto(DecisionClinica decision) => decision switch
        {
            DecisionClinica.ContactarPersonalmente => Localizacion("seguimiento.dec.contactar"),
            DecisionClinica.VisitaDomiciliaria => Localizacion("seguimiento.dec.visita"),
            DecisionClinica.CambiarPeriodicidad => Localizacion("seguimiento.dec.periodicidad"),
            _ => Localizacion("seguimiento.dec.nocontactable")
        };

        private void CargarCasos()
        {
            using var contexto = new NegocioDbContext();

            var alertas = contexto.Alertas.AsNoTracking()
                .Where(a => a.Tipo == TipoAlerta.ReporteAusente && a.Estado == EstadoAlerta.Activa)
                .OrderByDescending(a => a.FechaGeneracion)
                .ToList();
            Dictionary<int, string> dictPacientes = contexto.Pacientes.AsNoTracking()
                .ToDictionary(p => p.Id, p => p.NombreCompleto);

            _casos.Clear();
            foreach (Alerta alerta in alertas)
            {
                if (!dictPacientes.TryGetValue(alerta.IdPaciente, out string? nombre))
                {
                    continue;
                }
                HistoriaClinica? hc = contexto.HistoriasClinicas.AsNoTracking()
                    .FirstOrDefault(h => h.IdPaciente == alerta.IdPaciente);
                DateTime? ultimo = contexto.MedicionesRIN.AsNoTracking()
                    .Where(m => m.IdPaciente == alerta.IdPaciente)
                    .OrderByDescending(m => m.FechaMedicion).ThenByDescending(m => m.Id)
                    .Select(m => (DateTime?)m.FechaMedicion)
                    .FirstOrDefault();
                DateTime fechaBase = ultimo?.Date ?? hc?.FechaConfiguracion.Date ?? alerta.FechaGeneracion.Date;
                int periodicidad = hc?.PeriodicidadDias ?? 30;
                int dias = Math.Max(0, (int)(DateTime.Today - fechaBase.AddDays(periodicidad)).TotalDays);
                int intentos = contexto.Seguimientos.AsNoTracking()
                    .Count(s => s.IdPaciente == alerta.IdPaciente &&
                                s.Resultado == ResultadoContacto.Fallido &&
                                s.FechaRegistro >= alerta.FechaGeneracion);

                _casos.Add(new FilaCaso
                {
                    IdPaciente = alerta.IdPaciente,
                    Nombre = nombre,
                    UltimoReporte = ultimo,
                    DiasVencido = dias,
                    Intentos = intentos,
                    Escalada = alerta.EscaladaAMedico
                });
            }

            _grillaCasos.Rows.Clear();
            foreach (FilaCaso caso in _casos)
            {
                int indice = _grillaCasos.Rows.Add(
                    caso.Nombre,
                    caso.UltimoReporte?.ToString("dd/MM/yyyy") ?? "—",
                    caso.DiasVencido,
                    caso.Intentos,
                    caso.Escalada ? Localizacion("seguimiento.protocolo.escalada") : Localizacion("seguimiento.protocolo.enCurso"));
                if (caso.Escalada)
                {
                    _grillaCasos.Rows[indice].DefaultCellStyle.ForeColor = Color.FromArgb(180, 30, 30);
                    _grillaCasos.Rows[indice].DefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                }
            }

            _lblSinCasos.Text = _casos.Count == 0 ? Localizacion("seguimiento.sinPendientes") : string.Empty;

            if (_casos.Count == 0)
            {
                _idPacienteSeleccionado = 0;
                LimpiarDetalle();
                return;
            }

            int objetivo = _casos.FindIndex(c => c.IdPaciente == _idPacienteSeleccionado);
            if (objetivo < 0)
            {
                objetivo = 0;
            }
            _grillaCasos.Rows[objetivo].Selected = true;
            CargarDetalle(_casos[objetivo].IdPaciente);
        }

        private void CargarDetalle(int idPaciente)
        {
            _idPacienteSeleccionado = idPaciente;
            FilaCaso? caso = _casos.FirstOrDefault(c => c.IdPaciente == idPaciente);
            if (caso == null)
            {
                LimpiarDetalle();
                return;
            }

            _lblPacienteDetalle.Text = caso.Nombre;
            string estado = caso.Escalada ? Localizacion("seguimiento.protocolo.escalada") : Localizacion("seguimiento.protocolo.enCurso");
            _lblProtocolo.Text = $"{Localizacion("seguimiento.columna.intentos")}: {caso.Intentos} / {SeguimientoLogic.UmbralIntentosFallidos} · {estado}";
            _lblAviso.Text = string.Empty;

            using var contexto = new NegocioDbContext();
            List<Seguimiento> historial = new SeguimientoLogic(contexto).ObtenerSeguimientos(idPaciente);
            _grillaHistorial.Rows.Clear();
            foreach (Seguimiento s in historial)
            {
                _grillaHistorial.Rows.Add(
                    s.FechaRegistro.ToString("dd/MM/yyyy HH:mm"),
                    TipoTexto(s.TipoContacto),
                    ResultadoTexto(s.Resultado),
                    s.DecisionClinica == DecisionClinica.Ninguna ? string.Empty : DecisionTexto(s.DecisionClinica),
                    s.Observaciones ?? string.Empty);
            }

            bool puedeDecidir = historial.Count > 0;
            _cmbDecision.Enabled = puedeDecidir;
            _txtDetalleDecision.Enabled = puedeDecidir;
            _btnRegistrarDecision.Enabled = puedeDecidir;
            ActualizarPeriodicidadHabilitada();
            if (!puedeDecidir)
            {
                _lblProtocolo.Text += $" · {Localizacion("seguimiento.decSinIntentos")}";
            }
        }

        private void LimpiarDetalle()
        {
            _lblPacienteDetalle.Text = Localizacion("seguimiento.detalleVacio");
            _lblProtocolo.Text = string.Empty;
            _grillaHistorial.Rows.Clear();
            _lblAviso.Text = string.Empty;
            _cmbDecision.Enabled = false;
            _txtDetalleDecision.Enabled = false;
            _btnRegistrarDecision.Enabled = false;
            _numPeriodicidad.Enabled = false;
        }

        private void ActualizarPeriodicidadHabilitada()
        {
            bool esCambio = _cmbDecision.SelectedIndex == 2; // Cambiar periodicidad
            _numPeriodicidad.Enabled = esCambio && _cmbDecision.Enabled;
        }

        private void DetectarPlazos()
        {
            try
            {
                using var contexto = new NegocioDbContext();
                List<Alerta> creadas = new SeguimientoLogic(contexto).DetectarPlazosVencidos();
                MessageBox.Show(FindForm(),
                    $"{Localizacion("seguimiento.deteccion")} {creadas.Count}",
                    Localizacion("mod.seguimiento"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                Recargar();
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "SeguimientoControl");
            }
        }

        private void RegistrarIntento()
        {
            if (_idPacienteSeleccionado == 0)
            {
                _lblAviso.Text = Localizacion("seguimiento.detalleVacio");
                return;
            }

            try
            {
                using var contexto = new NegocioDbContext();
                var intento = new Seguimiento
                {
                    IdPaciente = _idPacienteSeleccionado,
                    TipoContacto = (TipoContacto)Math.Max(0, _cmbTipoContacto.SelectedIndex),
                    Resultado = _cmbResultado.SelectedIndex == 0 ? ResultadoContacto.Exitoso : ResultadoContacto.Fallido,
                    Observaciones = string.IsNullOrWhiteSpace(_txtObservaciones.Text) ? null : _txtObservaciones.Text.Trim()
                };

                ResultadoIntentoContacto resultado = new SeguimientoLogic(contexto).RegistrarIntentoContacto(intento, _nombreUsuario);

                if (resultado.CerroAlerta)
                {
                    MessageBox.Show(FindForm(), Localizacion("seguimiento.contactoOk"), Localizacion("seguimiento.contacto"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (resultado.EscaladaAlMedico)
                {
                    MessageBox.Show(FindForm(), Localizacion("seguimiento.escaladaMsg"), Localizacion("seguimiento.escalada"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    _lblAviso.Text = Localizacion("seguimiento.intentoRegistrado");
                }

                _txtObservaciones.Clear();
                Recargar();
            }
            catch (ValidacionNegocioException ex)
            {
                _lblAviso.Text = string.Join(Environment.NewLine, ex.Errores);
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "SeguimientoControl");
            }
        }

        private void RegistrarDecision()
        {
            if (_idPacienteSeleccionado == 0 || !_cmbDecision.Enabled)
            {
                _lblAviso.Text = Localizacion("seguimiento.decSinIntentos");
                return;
            }

            DecisionClinica[] decisiones =
            {
                DecisionClinica.ContactarPersonalmente,
                DecisionClinica.VisitaDomiciliaria,
                DecisionClinica.CambiarPeriodicidad,
                DecisionClinica.DeclararNoContactable
            };
            DecisionClinica decision = decisiones[Math.Max(0, _cmbDecision.SelectedIndex)];
            int? nuevaPeriodicidad = decision == DecisionClinica.CambiarPeriodicidad ? (int)_numPeriodicidad.Value : null;

            try
            {
                using var contexto = new NegocioDbContext();
                new SeguimientoLogic(contexto).RegistrarDecisionClinica(
                    _idPacienteSeleccionado, decision, _txtDetalleDecision.Text.Trim(), nuevaPeriodicidad, _nombreUsuario);

                MessageBox.Show(FindForm(), Localizacion("seguimiento.decisionOk"), Localizacion("seguimiento.decision"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                _txtDetalleDecision.Clear();
                Recargar();
            }
            catch (ValidacionNegocioException ex)
            {
                _lblAviso.Text = string.Join(Environment.NewLine, ex.Errores);
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "SeguimientoControl");
            }
        }
    }
}
