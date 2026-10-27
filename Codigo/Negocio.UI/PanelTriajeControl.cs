using Negocio.BLL;
using Negocio.DAL.Context;
using Negocio.DomainModel;
using Negocio.DomainModel.Enums;
using Services.Facade;

namespace Negocio.UI
{
    /// <summary>
    /// Panel de Monitoreo / Triaje (REQ-FUNC-006): ranking en tiempo real de pacientes
    /// activos ordenado por criticidad y antigüedad del último reporte, con indicadores
    /// visuales (colores por criticidad, alerta activa y tendencia peligrosa).
    /// Se refresca automáticamente cada 30 segundos y de forma manual con "Actualizar".
    /// </summary>
    public class PanelTriajeControl : UserControl
    {
        private readonly Button _btnActualizar = new();
        private readonly Label _lblRefresco = new();
        private readonly DataGridView _grilla = new();
        private readonly FlowLayoutPanel _panelLeyenda = new();
        private readonly System.Windows.Forms.Timer _temporizador = new();

        public PanelTriajeControl()
        {
            ConstruirInterfaz();
            AplicarTextos();
            CargarDatos();

            _temporizador.Interval = 30_000;
            _temporizador.Tick += (s, e) => CargarDatos();
            _temporizador.Start();
        }

        private static string Texto(string clave) => LocalizationService.ObtenerTexto(clave);

        /// <summary>Detiene el refresco automático (al ocultar el panel).</summary>
        public void Pausar() => _temporizador.Stop();

        /// <summary>Reanuda el refresco automático y recarga los datos (al mostrar el panel).</summary>
        public void Reanudar()
        {
            CargarDatos();
            _temporizador.Start();
        }

        /// <summary>Reaplica los textos según el idioma activo (hot-swap).</summary>
        public void RefrescarTextos()
        {
            AplicarTextos();
            CargarDatos();
        }

        private void ConstruirInterfaz()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(244, 249, 254);
            Padding = new Padding(8, 4, 8, 6);

            // ---- Barra superior: botón actualizar + sello de refresco ----
            var panelBarra = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = BackColor };

            _btnActualizar.Location = new Point(0, 4);
            _btnActualizar.Size = new Size(130, 34);
            _btnActualizar.FlatStyle = FlatStyle.Flat;
            _btnActualizar.FlatAppearance.BorderSize = 0;
            _btnActualizar.BackColor = Color.FromArgb(45, 108, 223);
            _btnActualizar.ForeColor = Color.White;
            _btnActualizar.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _btnActualizar.Cursor = Cursors.Hand;
            _btnActualizar.Click += (s, e) => CargarDatos();

            _lblRefresco.AutoSize = true;
            _lblRefresco.Location = new Point(146, 12);
            _lblRefresco.Font = new Font("Segoe UI", 9F);
            _lblRefresco.ForeColor = Color.FromArgb(90, 110, 135);

            panelBarra.Controls.Add(_btnActualizar);
            panelBarra.Controls.Add(_lblRefresco);

            // ---- Leyenda inferior ----
            _panelLeyenda.Dock = DockStyle.Bottom;
            _panelLeyenda.Height = 44;
            _panelLeyenda.FlowDirection = FlowDirection.LeftToRight;
            _panelLeyenda.WrapContents = false;
            _panelLeyenda.BackColor = BackColor;
            _panelLeyenda.Padding = new Padding(0, 8, 0, 0);

            // ---- Grilla principal ----
            _grilla.Dock = DockStyle.Fill;
            _grilla.ReadOnly = true;
            _grilla.AllowUserToAddRows = false;
            _grilla.AllowUserToDeleteRows = false;
            _grilla.AllowUserToResizeRows = false;
            _grilla.RowHeadersVisible = false;
            _grilla.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grilla.MultiSelect = false;
            _grilla.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grilla.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            _grilla.BackgroundColor = Color.White;
            _grilla.BorderStyle = BorderStyle.None;
            _grilla.GridColor = Color.FromArgb(225, 235, 245);
            _grilla.EnableHeadersVisualStyles = false;
            _grilla.ColumnHeadersHeight = 38;
            _grilla.RowTemplate.Height = 34;
            _grilla.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(41, 98, 176);
            _grilla.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _grilla.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _grilla.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(41, 98, 176);
            _grilla.DefaultCellStyle.Font = new Font("Segoe UI", 11F);
            _grilla.DefaultCellStyle.SelectionBackColor = Color.FromArgb(210, 230, 250);
            _grilla.DefaultCellStyle.SelectionForeColor = Color.Black;
            _grilla.DefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
            _grilla.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 251, 255);

            _grilla.Columns.Add("paciente", "");
            _grilla.Columns.Add("documento", "");
            _grilla.Columns.Add("ultimoRin", "");
            _grilla.Columns.Add("fecha", "");
            _grilla.Columns.Add("dias", "");
            _grilla.Columns.Add("criticidad", "");
            _grilla.Columns.Add("alerta", "");
            _grilla.Columns["paciente"].FillWeight = 34;
            _grilla.Columns["documento"].FillWeight = 16;
            _grilla.Columns["ultimoRin"].FillWeight = 12;
            _grilla.Columns["fecha"].FillWeight = 14;
            _grilla.Columns["dias"].FillWeight = 14;
            _grilla.Columns["criticidad"].FillWeight = 18;
            _grilla.Columns["alerta"].FillWeight = 8;
            _grilla.Columns["ultimoRin"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _grilla.Columns["dias"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _grilla.Columns["criticidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _grilla.Columns["alerta"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            Controls.Add(_grilla);
            Controls.Add(panelBarra);
            Controls.Add(_panelLeyenda);
        }

        private void AplicarTextos()
        {
            _btnActualizar.Text = Texto("panel.actualizar");
            _grilla.Columns["paciente"].HeaderText = Texto("panel.columna.paciente");
            _grilla.Columns["documento"].HeaderText = Texto("panel.columna.documento");
            _grilla.Columns["ultimoRin"].HeaderText = Texto("panel.columna.ultimoRin");
            _grilla.Columns["fecha"].HeaderText = Texto("panel.columna.fecha");
            _grilla.Columns["dias"].HeaderText = Texto("panel.columna.dias");
            _grilla.Columns["criticidad"].HeaderText = Texto("panel.columna.criticidad");
            _grilla.Columns["alerta"].HeaderText = Texto("panel.columna.alerta");

            _panelLeyenda.Controls.Clear();
            var lblLeyenda = new Label
            {
                Text = Texto("panel.leyenda"),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 85, 115),
                Margin = new Padding(0, 6, 8, 0)
            };
            _panelLeyenda.Controls.Add(lblLeyenda);
            AgregarLeyenda(Color.FromArgb(200, 45, 45), Texto("panel.criticidadAlta"));
            AgregarLeyenda(Color.FromArgb(232, 163, 61), Texto("panel.criticidadMedia"));
            AgregarLeyenda(Color.FromArgb(46, 125, 70), Texto("panel.criticidadBaja"));
            AgregarLeyenda(Color.FromArgb(130, 140, 150), Texto("panel.sinDatos"));
        }

        private void AgregarLeyenda(Color color, string texto)
        {
            var chip = new Label
            {
                Size = new Size(16, 16),
                BackColor = color,
                Margin = new Padding(0, 6, 4, 0)
            };
            var etiqueta = new Label
            {
                Text = texto,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(60, 85, 115),
                Margin = new Padding(0, 6, 16, 0)
            };
            _panelLeyenda.Controls.Add(chip);
            _panelLeyenda.Controls.Add(etiqueta);
        }

        private void CargarDatos()
        {
            try
            {
                using var contexto = new NegocioDbContext();
                List<FilaPanelMonitoreo> filas = new PanelMonitoreoLogic(contexto).ObtenerPanel();

                _grilla.Rows.Clear();
                foreach (FilaPanelMonitoreo fila in filas)
                {
                    string criticidad = fila.NivelCriticidad == null
                        ? Texto("panel.sinDatos")
                        : Texto(fila.NivelCriticidad == NivelCriticidad.Alta ? "panel.criticidadAlta"
                            : fila.NivelCriticidad == NivelCriticidad.Media ? "panel.criticidadMedia"
                            : "panel.criticidadBaja") +
                          (fila.PorTendenciaPeligrosa ? " ⚠" : "");

                    int indice = _grilla.Rows.Add(
                        fila.NombreCompleto,
                        fila.DNI,
                        fila.UltimoValorRIN?.ToString("0.00") ?? "—",
                        fila.FechaUltimoReporte?.ToString("dd/MM/yyyy") ?? "—",
                        fila.DiasDesdeUltimoReporte?.ToString() ?? "—",
                        criticidad,
                        fila.TieneAlertaActiva ? "🔔" : "");

                    AplicarEstilo(fila, _grilla.Rows[indice]);
                }

                _lblRefresco.Text = Texto("panel.refresco") + " " + DateTime.Now.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "PanelTriaje", mostrarMensaje: false);
                _lblRefresco.Text = Texto("error.generico");
            }
        }

        private static void AplicarEstilo(FilaPanelMonitoreo fila, DataGridViewRow filaGrilla)
        {
            DataGridViewCell celdaCriticidad = filaGrilla.Cells[5];
            celdaCriticidad.Style.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            Color colorTexto = Color.White;
            Color colorFondo = Color.FromArgb(130, 140, 150);
            switch (fila.NivelCriticidad)
            {
                case NivelCriticidad.Alta:
                    colorFondo = Color.FromArgb(200, 45, 45);
                    filaGrilla.DefaultCellStyle.BackColor = Color.FromArgb(253, 231, 231);
                    break;
                case NivelCriticidad.Media:
                    colorFondo = Color.FromArgb(232, 163, 61);
                    break;
                case NivelCriticidad.Baja:
                    colorFondo = Color.FromArgb(46, 125, 70);
                    break;
            }

            celdaCriticidad.Style.BackColor = colorFondo;
            celdaCriticidad.Style.ForeColor = colorTexto;
            celdaCriticidad.Style.SelectionBackColor = colorFondo;
            celdaCriticidad.Style.SelectionForeColor = colorTexto;
        }
    }
}
