using System.Text;
using System.Text.Json;
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
    /// Reportes estadísticos (REQ-FUNC-011/012/013): reporte mensual de eficacia con semáforo
    /// óptimo/subóptimo/deficiente, reporte de eficacia agrupado por diagnóstico (destacando
    /// los grupos con bajo desempeño), historial clínico unificado por paciente y exportación
    /// auditada del historial a PDF/Excel.
    /// </summary>
    public class ReportesControl : UserControl
    {
        private readonly string _nombreUsuario;

        private readonly Button _btnVistaMensual = new();
        private readonly Button _btnVistaDiagnostico = new();
        private readonly Button _btnVistaHistorial = new();
        private readonly Label _lblPeriodo = new();
        private readonly DateTimePicker _dtpMes = new();

        private readonly Panel _panelMensual = new();
        private readonly Button _btnGenerarMensual = new();
        private readonly Label _lblTituloIndicador = new();
        private readonly Label _lblIndicador = new();
        private readonly Label _lblClasificacionGlobal = new();
        private readonly Label _lblResumen = new();
        private readonly DataGridView _grillaPacientes = new();

        private readonly Panel _panelDiagnostico = new();
        private readonly Button _btnGenerarDiagnostico = new();
        private readonly DataGridView _grillaDiagnosticos = new();

        private readonly Panel _panelHistorial = new();
        private readonly Label _lblDni = new();
        private readonly TextBox _txtDni = new();
        private readonly Button _btnBuscar = new();
        private readonly Label _lblPacienteHistorial = new();
        private readonly Label _lblFormato = new();
        private readonly ComboBox _cmbFormato = new();
        private readonly Button _btnExportar = new();
        private readonly DataGridView _grillaHistorial = new();

        private int _vistaActual;
        private int _idPacienteHistorial;

        public ReportesControl(string nombreUsuario)
        {
            _nombreUsuario = nombreUsuario;
            ConstruirInterfaz();
            AplicarTextos();
            MostrarVista(0);
        }

        private static string Localizacion(string clave) => LocalizationService.ObtenerTexto(clave);

        /// <summary>Reaplica textos por cambio de idioma.</summary>
        public void RefrescarTextos() => AplicarTextos();

        /// <summary>Restablece la vista por defecto (al entrar al módulo).</summary>
        public void Recargar() => MostrarVista(_vistaActual);

        private void ConstruirInterfaz()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(244, 249, 254);
            Padding = new Padding(8, 4, 8, 6);

            // ---- Barra superior: vistas + período ----
            var panelBarra = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = BackColor };

            ConfigurarBotonVista(_btnVistaMensual, 0);
            ConfigurarBotonVista(_btnVistaDiagnostico, 176);
            ConfigurarBotonVista(_btnVistaHistorial, 352);

            _lblPeriodo.Location = new Point(760, 20);
            _lblPeriodo.Size = new Size(72, 20);
            _lblPeriodo.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _lblPeriodo.ForeColor = Color.FromArgb(60, 85, 115);

            _dtpMes.Location = new Point(836, 16);
            _dtpMes.Size = new Size(170, 28);
            _dtpMes.Format = DateTimePickerFormat.Custom;
            _dtpMes.CustomFormat = "MMMM yyyy";
            _dtpMes.ShowUpDown = true;
            _dtpMes.Font = new Font("Segoe UI", 10F);

            panelBarra.Controls.Add(_btnVistaMensual);
            panelBarra.Controls.Add(_btnVistaDiagnostico);
            panelBarra.Controls.Add(_btnVistaHistorial);
            panelBarra.Controls.Add(_lblPeriodo);
            panelBarra.Controls.Add(_dtpMes);

            // ---- Vista 1: reporte mensual ----
            _panelMensual.Dock = DockStyle.Fill;
            _panelMensual.BackColor = Color.White;

            _btnGenerarMensual.Location = new Point(14, 12);
            _btnGenerarMensual.Size = new Size(210, 32);
            _btnGenerarMensual.FlatStyle = FlatStyle.Flat;
            _btnGenerarMensual.BackColor = Color.FromArgb(45, 108, 223);
            _btnGenerarMensual.ForeColor = Color.White;
            _btnGenerarMensual.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _btnGenerarMensual.FlatAppearance.BorderSize = 0;
            _btnGenerarMensual.Cursor = Cursors.Hand;
            _btnGenerarMensual.Click += (s, e) => CargarMensual();

            _lblTituloIndicador.Location = new Point(14, 54);
            _lblTituloIndicador.Size = new Size(420, 20);
            _lblTituloIndicador.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _lblTituloIndicador.ForeColor = Color.FromArgb(60, 85, 115);

            _lblIndicador.Location = new Point(12, 72);
            _lblIndicador.Size = new Size(330, 56);
            _lblIndicador.Font = new Font("Segoe UI", 30F, FontStyle.Bold);
            _lblIndicador.ForeColor = Color.FromArgb(120, 130, 145);

            _lblClasificacionGlobal.Location = new Point(16, 128);
            _lblClasificacionGlobal.Size = new Size(320, 26);
            _lblClasificacionGlobal.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            _lblClasificacionGlobal.ForeColor = Color.FromArgb(120, 130, 145);

            _lblResumen.Location = new Point(370, 76);
            _lblResumen.Size = new Size(760, 60);
            _lblResumen.Font = new Font("Segoe UI", 10.5F);
            _lblResumen.ForeColor = Color.FromArgb(40, 60, 90);

            var hostPacientes = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 166, 14, 14) };
            _grillaPacientes.Dock = DockStyle.Fill;
            EstiloGrilla(_grillaPacientes);
            _grillaPacientes.Columns.Add("paciente", "");
            _grillaPacientes.Columns.Add("mediciones", "");
            _grillaPacientes.Columns.Add("porcentaje", "");
            _grillaPacientes.Columns.Add("clasificacion", "");
            _grillaPacientes.Columns["paciente"].FillWeight = 40;
            _grillaPacientes.Columns["mediciones"].FillWeight = 14;
            _grillaPacientes.Columns["porcentaje"].FillWeight = 16;
            _grillaPacientes.Columns["clasificacion"].FillWeight = 18;
            foreach (string columna in new[] { "mediciones", "porcentaje", "clasificacion" })
            {
                _grillaPacientes.Columns[columna].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            _panelMensual.Controls.Add(hostPacientes);
            hostPacientes.Controls.Add(_grillaPacientes);
            _panelMensual.Controls.Add(_btnGenerarMensual);
            _panelMensual.Controls.Add(_lblTituloIndicador);
            _panelMensual.Controls.Add(_lblIndicador);
            _panelMensual.Controls.Add(_lblClasificacionGlobal);
            _panelMensual.Controls.Add(_lblResumen);
            _btnGenerarMensual.BringToFront();
            _lblTituloIndicador.BringToFront();
            _lblIndicador.BringToFront();
            _lblClasificacionGlobal.BringToFront();
            _lblResumen.BringToFront();

            // ---- Vista 2: por diagnóstico ----
            _panelDiagnostico.Dock = DockStyle.Fill;
            _panelDiagnostico.BackColor = Color.White;

            _btnGenerarDiagnostico.Location = new Point(14, 12);
            _btnGenerarDiagnostico.Size = new Size(210, 32);
            _btnGenerarDiagnostico.FlatStyle = FlatStyle.Flat;
            _btnGenerarDiagnostico.BackColor = Color.FromArgb(45, 108, 223);
            _btnGenerarDiagnostico.ForeColor = Color.White;
            _btnGenerarDiagnostico.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _btnGenerarDiagnostico.FlatAppearance.BorderSize = 0;
            _btnGenerarDiagnostico.Cursor = Cursors.Hand;
            _btnGenerarDiagnostico.Click += (s, e) => CargarDiagnosticos();

            var hostDiagnosticos = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 56, 14, 14) };
            _grillaDiagnosticos.Dock = DockStyle.Fill;
            EstiloGrilla(_grillaDiagnosticos);
            _grillaDiagnosticos.Columns.Add("diagnostico", "");
            _grillaDiagnosticos.Columns.Add("pacientes", "");
            _grillaDiagnosticos.Columns.Add("indicador", "");
            _grillaDiagnosticos.Columns.Add("optimos", "");
            _grillaDiagnosticos.Columns.Add("suboptimos", "");
            _grillaDiagnosticos.Columns.Add("deficientes", "");
            _grillaDiagnosticos.Columns.Add("bajoDesempeno", "");
            _grillaDiagnosticos.Columns["diagnostico"].FillWeight = 30;
            _grillaDiagnosticos.Columns["pacientes"].FillWeight = 10;
            _grillaDiagnosticos.Columns["indicador"].FillWeight = 14;
            _grillaDiagnosticos.Columns["optimos"].FillWeight = 10;
            _grillaDiagnosticos.Columns["suboptimos"].FillWeight = 10;
            _grillaDiagnosticos.Columns["deficientes"].FillWeight = 10;
            _grillaDiagnosticos.Columns["bajoDesempeno"].FillWeight = 16;
            foreach (string columna in new[] { "pacientes", "indicador", "optimos", "suboptimos", "deficientes", "bajoDesempeno" })
            {
                _grillaDiagnosticos.Columns[columna].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            _panelDiagnostico.Controls.Add(hostDiagnosticos);
            hostDiagnosticos.Controls.Add(_grillaDiagnosticos);
            _panelDiagnostico.Controls.Add(_btnGenerarDiagnostico);
            _btnGenerarDiagnostico.BringToFront();

            // ---- Vista 3: historial del paciente ----
            _panelHistorial.Dock = DockStyle.Fill;
            _panelHistorial.BackColor = Color.White;

            _lblDni.Location = new Point(14, 10);
            _lblDni.Size = new Size(300, 18);
            _lblDni.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _lblDni.ForeColor = Color.FromArgb(60, 85, 115);

            _txtDni.Name = "txtDniHist";
            _txtDni.Location = new Point(14, 28);
            _txtDni.Size = new Size(200, 28);
            _txtDni.Font = new Font("Segoe UI", 11F);
            _txtDni.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; BuscarHistorial(); } };

            _btnBuscar.Location = new Point(226, 27);
            _btnBuscar.Size = new Size(110, 30);
            _btnBuscar.FlatStyle = FlatStyle.Flat;
            _btnBuscar.BackColor = Color.FromArgb(45, 108, 223);
            _btnBuscar.ForeColor = Color.White;
            _btnBuscar.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _btnBuscar.FlatAppearance.BorderSize = 0;
            _btnBuscar.Cursor = Cursors.Hand;
            _btnBuscar.Click += (s, e) => BuscarHistorial();

            _lblPacienteHistorial.Location = new Point(350, 30);
            _lblPacienteHistorial.Size = new Size(560, 24);
            _lblPacienteHistorial.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _lblPacienteHistorial.ForeColor = Color.FromArgb(30, 66, 120);

            _lblFormato.Location = new Point(14, 70);
            _lblFormato.Size = new Size(80, 18);
            _lblFormato.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _lblFormato.ForeColor = Color.FromArgb(60, 85, 115);

            _cmbFormato.Location = new Point(96, 66);
            _cmbFormato.Size = new Size(120, 26);
            _cmbFormato.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbFormato.Font = new Font("Segoe UI", 9.5F);

            _btnExportar.Location = new Point(230, 64);
            _btnExportar.Size = new Size(190, 30);
            _btnExportar.FlatStyle = FlatStyle.Flat;
            _btnExportar.BackColor = Color.FromArgb(45, 108, 223);
            _btnExportar.ForeColor = Color.White;
            _btnExportar.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _btnExportar.FlatAppearance.BorderSize = 0;
            _btnExportar.Cursor = Cursors.Hand;
            _btnExportar.Click += (s, e) => ExportarHistorial();

            var hostHistorial = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 104, 14, 14) };
            _grillaHistorial.Dock = DockStyle.Fill;
            EstiloGrilla(_grillaHistorial);
            _grillaHistorial.Columns.Add("fecha", "");
            _grillaHistorial.Columns.Add("tipo", "");
            _grillaHistorial.Columns.Add("descripcion", "");
            _grillaHistorial.Columns["fecha"].FillWeight = 16;
            _grillaHistorial.Columns["tipo"].FillWeight = 16;
            _grillaHistorial.Columns["descripcion"].FillWeight = 68;
            // Las celdas de texto largo crecen en alto para mostrar el contenido completo.
            _grillaHistorial.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            _grillaHistorial.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            _panelHistorial.Controls.Add(hostHistorial);
            hostHistorial.Controls.Add(_grillaHistorial);
            _panelHistorial.Controls.Add(_lblDni);
            _panelHistorial.Controls.Add(_txtDni);
            _panelHistorial.Controls.Add(_btnBuscar);
            _panelHistorial.Controls.Add(_lblPacienteHistorial);
            _panelHistorial.Controls.Add(_lblFormato);
            _panelHistorial.Controls.Add(_cmbFormato);
            _panelHistorial.Controls.Add(_btnExportar);
            _txtDni.BringToFront();
            _lblDni.BringToFront();
            _lblPacienteHistorial.BringToFront();
            _lblFormato.BringToFront();
            _btnBuscar.BringToFront();
            _cmbFormato.BringToFront();
            _btnExportar.BringToFront();

            Controls.Add(_panelMensual);
            Controls.Add(_panelDiagnostico);
            Controls.Add(_panelHistorial);
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

        private void ConfigurarBotonVista(Button boton, int x)
        {
            boton.Location = new Point(x, 15);
            boton.Size = new Size(168, 32);
            boton.FlatStyle = FlatStyle.Flat;
            boton.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            boton.Cursor = Cursors.Hand;
            boton.FlatAppearance.BorderColor = Color.FromArgb(202, 220, 240);
            boton.Click += (s, e) => MostrarVista(boton == _btnVistaMensual ? 0 : boton == _btnVistaDiagnostico ? 1 : 2);
        }

        private void MostrarVista(int vista)
        {
            _vistaActual = vista;
            _panelMensual.Visible = vista == 0;
            _panelDiagnostico.Visible = vista == 1;
            _panelHistorial.Visible = vista == 2;

            EstiloBotonVista(_btnVistaMensual, vista == 0);
            EstiloBotonVista(_btnVistaDiagnostico, vista == 1);
            EstiloBotonVista(_btnVistaHistorial, vista == 2);
        }

        private static void EstiloBotonVista(Button boton, bool activo)
        {
            boton.BackColor = activo ? Color.FromArgb(45, 108, 223) : Color.White;
            boton.ForeColor = activo ? Color.White : Color.FromArgb(28, 60, 105);
        }

        private void AplicarTextos()
        {
            _btnVistaMensual.Text = Localizacion("reportes.mensual");
            _btnVistaDiagnostico.Text = Localizacion("reportes.diagnostico");
            _btnVistaHistorial.Text = Localizacion("reportes.historial");
            _lblPeriodo.Text = Localizacion("reportes.periodo");

            _btnGenerarMensual.Text = Localizacion("reportes.generar");
            _lblTituloIndicador.Text = Localizacion("reportes.indicadorGlobal");
            if (_lblIndicador.Text.Length == 0 || _lblIndicador.Text == "—")
            {
                _lblIndicador.Text = "—";
            }
            _grillaPacientes.Columns["paciente"].HeaderText = Localizacion("panel.columna.paciente");
            _grillaPacientes.Columns["mediciones"].HeaderText = Localizacion("reportes.columna.mediciones");
            _grillaPacientes.Columns["porcentaje"].HeaderText = Localizacion("reportes.columna.porcentaje");
            _grillaPacientes.Columns["clasificacion"].HeaderText = Localizacion("reportes.columna.clasificacion");

            _btnGenerarDiagnostico.Text = Localizacion("reportes.generarDiag");
            _grillaDiagnosticos.Columns["diagnostico"].HeaderText = Localizacion("reportes.columna.diagnostico");
            _grillaDiagnosticos.Columns["pacientes"].HeaderText = Localizacion("reportes.columna.pacientes");
            _grillaDiagnosticos.Columns["indicador"].HeaderText = Localizacion("reportes.columna.indicador");
            _grillaDiagnosticos.Columns["optimos"].HeaderText = Localizacion("reportes.clas.optimo");
            _grillaDiagnosticos.Columns["suboptimos"].HeaderText = Localizacion("reportes.clas.suboptimo");
            _grillaDiagnosticos.Columns["deficientes"].HeaderText = Localizacion("reportes.clas.deficiente");
            _grillaDiagnosticos.Columns["bajoDesempeno"].HeaderText = "";

            _lblDni.Text = Localizacion("rin.identificacion");
            _btnBuscar.Text = Localizacion("comun.buscar");
            if (_lblPacienteHistorial.Text.Length == 0)
            {
                _lblPacienteHistorial.Text = Localizacion("reportes.historialVacio");
            }
            _lblFormato.Text = Localizacion("reportes.formato");
            int formatoPrevio = _cmbFormato.SelectedIndex;
            _cmbFormato.Items.Clear();
            _cmbFormato.Items.Add("PDF");
            _cmbFormato.Items.Add("Excel");
            _cmbFormato.Items.Add("JSON");
            _cmbFormato.SelectedIndex = formatoPrevio < 0 ? 0 : formatoPrevio;
            _btnExportar.Text = Localizacion("reportes.exportar");
            _grillaHistorial.Columns["fecha"].HeaderText = Localizacion("rin.columna.fecha");
            _grillaHistorial.Columns["tipo"].HeaderText = Localizacion("reportes.columna.tipo");
            _grillaHistorial.Columns["descripcion"].HeaderText = Localizacion("reportes.columna.descripcion");

            if (_lblResumen.Text.Length == 0)
            {
                _lblResumen.Text = Localizacion("reportes.sinReporte");
            }
        }

        private string ClaseTexto(ClasificacionControl clase) => clase switch
        {
            ClasificacionControl.Optimo => Localizacion("reportes.clas.optimo"),
            ClasificacionControl.Suboptimo => Localizacion("reportes.clas.suboptimo"),
            _ => Localizacion("reportes.clas.deficiente")
        };

        private static Color ColorClase(ClasificacionControl clase) => clase switch
        {
            ClasificacionControl.Optimo => Color.FromArgb(30, 130, 75),
            ClasificacionControl.Suboptimo => Color.FromArgb(190, 125, 20),
            _ => Color.FromArgb(185, 45, 40)
        };

        private void CargarMensual()
        {
            try
            {
                using var contexto = new NegocioDbContext();
                ReporteEstadistico reporte = new ReporteLogic(contexto)
                    .GenerarReporteMensual(_dtpMes.Value.Year, _dtpMes.Value.Month, _nombreUsuario);

                var clase = ReporteLogic.Clasificar(reporte.IndicadorGlobalEficacia);
                _lblIndicador.Text = $"{reporte.IndicadorGlobalEficacia:0.00} %";
                _lblIndicador.ForeColor = ColorClase(clase);
                _lblClasificacionGlobal.Text = ClaseTexto(clase);
                _lblClasificacionGlobal.ForeColor = ColorClase(clase);

                int sinMediciones = 0;
                _grillaPacientes.Rows.Clear();
                if (!string.IsNullOrWhiteSpace(reporte.DetalleJson))
                {
                    using JsonDocument doc = JsonDocument.Parse(reporte.DetalleJson);
                    if (doc.RootElement.TryGetProperty("PacientesSinMediciones", out JsonElement sin))
                    {
                        sinMediciones = sin.GetInt32();
                    }
                    if (doc.RootElement.TryGetProperty("Pacientes", out JsonElement pacientes))
                    {
                        foreach (JsonElement paciente in pacientes.EnumerateArray())
                        {
                            string nombre = paciente.GetProperty("NombreCompleto").GetString() ?? "—";
                            int mediciones = paciente.GetProperty("Mediciones").GetInt32();
                            decimal porcentaje = paciente.GetProperty("PorcentajeEnRango").GetDecimal();
                            string claseTexto = paciente.GetProperty("Clasificacion").GetString() ?? "";
                            Enum.TryParse(claseTexto, out ClasificacionControl clasePaciente);
                            int indice = _grillaPacientes.Rows.Add(nombre, mediciones, $"{porcentaje:0.00} %", ClaseTexto(clasePaciente));
                            DataGridViewCell celda = _grillaPacientes.Rows[indice].Cells[3];
                            celda.Style.BackColor = ColorClase(clasePaciente);
                            celda.Style.ForeColor = Color.White;
                            celda.Style.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                            celda.Style.SelectionBackColor = ColorClase(clasePaciente);
                            celda.Style.SelectionForeColor = Color.White;
                        }
                    }
                }

                _grillaPacientes.ClearSelection();
                int conDatos = reporte.CantidadOptimos + reporte.CantidadSuboptimos + reporte.CantidadDeficientes;
                _lblResumen.Text =
                    $"{Localizacion("reportes.pacientesDatos")} {conDatos}   ·   " +
                    $"{Localizacion("reportes.clas.optimo")}: {reporte.CantidadOptimos}   ·   " +
                    $"{Localizacion("reportes.clas.suboptimo")}: {reporte.CantidadSuboptimos}   ·   " +
                    $"{Localizacion("reportes.clas.deficiente")}: {reporte.CantidadDeficientes}   ·   " +
                    $"{Localizacion("reportes.sinMediciones")} {sinMediciones}";
            }
            catch (ValidacionNegocioException ex)
            {
                _lblResumen.Text = string.Join(Environment.NewLine, ex.Errores);
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "ReportesControl");
            }
        }

        private void CargarDiagnosticos()
        {
            try
            {
                using var contexto = new NegocioDbContext();
                List<FilaReporteDiagnostico> filas = new ReporteLogic(contexto)
                    .GenerarReportePorDiagnostico(_dtpMes.Value.Year, _dtpMes.Value.Month, null, _nombreUsuario);

                _grillaDiagnosticos.Rows.Clear();
                foreach (FilaReporteDiagnostico fila in filas)
                {
                    int indice = _grillaDiagnosticos.Rows.Add(
                        fila.NombreDiagnostico,
                        fila.CantidadPacientes,
                        $"{fila.IndicadorPromedio:0.00} %",
                        fila.CantidadOptimos,
                        fila.CantidadSuboptimos,
                        fila.CantidadDeficientes,
                        fila.BajoDesempeno ? Localizacion("reportes.bajoDesempeno") : string.Empty);

                    if (fila.BajoDesempeno)
                    {
                        _grillaDiagnosticos.Rows[indice].DefaultCellStyle.BackColor = Color.FromArgb(253, 236, 234);
                        _grillaDiagnosticos.Rows[indice].DefaultCellStyle.SelectionBackColor = Color.FromArgb(250, 222, 219);
                        _grillaDiagnosticos.Rows[indice].Cells[6].Style.ForeColor = Color.FromArgb(185, 45, 40);
                        _grillaDiagnosticos.Rows[indice].Cells[6].Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                        _grillaDiagnosticos.Rows[indice].Cells[2].Style.ForeColor = Color.FromArgb(185, 45, 40);
                        _grillaDiagnosticos.Rows[indice].Cells[2].Style.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                    }
                }
                _grillaDiagnosticos.ClearSelection();
            }
            catch (ValidacionNegocioException ex)
            {
                MessageBox.Show(FindForm(), string.Join(Environment.NewLine, ex.Errores), Localizacion("mod.reportes"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "ReportesControl");
            }
        }

        private void BuscarHistorial()
        {
            try
            {
                using var contexto = new NegocioDbContext();
                Paciente? paciente = new PacienteLogic(contexto).BuscarPorDNI(_txtDni.Text.Trim());
                if (paciente == null)
                {
                    _idPacienteHistorial = 0;
                    _grillaHistorial.Rows.Clear();
                    _lblPacienteHistorial.Text = Localizacion("rin.pacienteNoEncontrado");
                    return;
                }

                _idPacienteHistorial = paciente.Id;
                _lblPacienteHistorial.Text = $"{paciente.NombreCompleto} ({paciente.DNI})";

                List<ItemHistorial> items = new ReporteLogic(contexto).ObtenerHistorialPaciente(paciente.Id);
                _grillaHistorial.Rows.Clear();
                foreach (ItemHistorial item in items)
                {
                    _grillaHistorial.Rows.Add(item.Fecha.ToString("dd/MM/yyyy"), item.Tipo, item.Descripcion);
                }
                if (items.Count == 0)
                {
                    _lblPacienteHistorial.Text += $" — {Localizacion("reportes.sinItems")}";
                }
                _grillaHistorial.ClearSelection();
            }
            catch (ValidacionNegocioException ex)
            {
                _lblPacienteHistorial.Text = string.Join(Environment.NewLine, ex.Errores);
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "ReportesControl");
            }
        }

        private void ExportarHistorial()
        {
            if (_idPacienteHistorial == 0)
            {
                MessageBox.Show(FindForm(), Localizacion("reportes.historialVacio"), Localizacion("mod.reportes"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string formato = _cmbFormato.SelectedIndex == 1 ? "Excel" : "PDF";
            try
            {
                using var contexto = new NegocioDbContext();
                Paciente paciente = contexto.Pacientes.AsNoTracking().First(p => p.Id == _idPacienteHistorial);
                ExportacionHistorial exportacion = new ReporteLogic(contexto)
                    .ExportarHistorialPaciente(_idPacienteHistorial, formato, _nombreUsuario);
                string ruta = Exportadores.EscribirHistorial(exportacion, paciente.NombreCompleto, formato);

                MessageBox.Show(FindForm(), $"{Localizacion("reportes.exportadoOk")}{Environment.NewLine}{ruta}",
                    Localizacion("reportes.exportar"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (ValidacionNegocioException ex)
            {
                MessageBox.Show(FindForm(), string.Join(Environment.NewLine, ex.Errores), Localizacion("mod.reportes"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "ReportesControl");
            }
        }
    }
}
