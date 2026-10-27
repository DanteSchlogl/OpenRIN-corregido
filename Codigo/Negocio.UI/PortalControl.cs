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
    /// Portal del paciente: vista para la persona anticoagulada autenticada (REQ-FUNC-005/013):
    /// reporta su propio RIN por el canal digital y consulta su historial clínico unificado.
    /// El vínculo sesión → paciente se resuelve por Paciente.IdUsuarioPortal.
    /// </summary>
    public class PortalControl : UserControl
    {
        private readonly int _idUsuario;
        private readonly string _nombreUsuario;

        private readonly Label _lblBienvenida = new();
        private readonly Panel _panelReporte = new();
        private readonly Label _lblReportar = new();
        private readonly Label _lblValor = new();
        private readonly NumericUpDown _numValor = new();
        private readonly Label _lblFecha = new();
        private readonly DateTimePicker _dtpFecha = new();
        private readonly Label _lblCanal = new();
        private readonly Label _lblCanalValor = new();
        private readonly Button _btnEnviar = new();
        private readonly Label _lblEstado = new();
        private readonly Panel _panelHistorial = new();
        private readonly Label _lblHistorial = new();
        private readonly DataGridView _grillaHistorial = new();

        private Paciente? _paciente;

        public PortalControl(int idUsuario, string nombreUsuario)
        {
            _idUsuario = idUsuario;
            _nombreUsuario = nombreUsuario;
            ConstruirInterfaz();
            AplicarTextos();
            Recargar();
        }

        private static string Localizacion(string clave) => LocalizationService.ObtenerTexto(clave);

        /// <summary>Resuelve el paciente vinculado a la sesión y recarga la vista.</summary>
        public void Recargar()
        {
            ResolverPaciente();
            if (_paciente != null)
            {
                CargarHistorial();
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

            _lblBienvenida.Dock = DockStyle.Top;
            _lblBienvenida.Height = 44;
            _lblBienvenida.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            _lblBienvenida.ForeColor = Color.FromArgb(30, 66, 120);
            _lblBienvenida.Padding = new Padding(6, 8, 0, 0);

            // ---- Sección de reporte ----
            _panelReporte.Dock = DockStyle.Top;
            _panelReporte.Height = 156;
            _panelReporte.BackColor = Color.White;
            _panelReporte.Padding = new Padding(12, 10, 12, 10);

            _lblReportar.Location = new Point(14, 10);
            _lblReportar.Size = new Size(400, 22);
            _lblReportar.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _lblReportar.ForeColor = Color.FromArgb(41, 98, 176);

            _lblValor.Location = new Point(14, 42);
            _lblValor.Size = new Size(150, 16);
            _lblValor.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _lblValor.ForeColor = Color.FromArgb(60, 85, 115);

            _numValor.Name = "numValorPortal";
            _numValor.Location = new Point(14, 62);
            _numValor.Size = new Size(140, 28);
            _numValor.DecimalPlaces = 2;
            _numValor.Minimum = 0.1M;
            _numValor.Maximum = 20M;
            _numValor.Increment = 0.1M;
            _numValor.Value = 2.5M;
            _numValor.Font = new Font("Segoe UI", 11F);

            _lblFecha.Location = new Point(176, 42);
            _lblFecha.Size = new Size(150, 16);
            _lblFecha.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _lblFecha.ForeColor = Color.FromArgb(60, 85, 115);

            _dtpFecha.Name = "dtpFechaPortal";
            _dtpFecha.Location = new Point(176, 62);
            _dtpFecha.Size = new Size(150, 28);
            _dtpFecha.Format = DateTimePickerFormat.Short;
            _dtpFecha.Font = new Font("Segoe UI", 10F);
            _dtpFecha.Value = DateTime.Today;

            _lblCanal.Location = new Point(348, 42);
            _lblCanal.Size = new Size(150, 16);
            _lblCanal.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _lblCanal.ForeColor = Color.FromArgb(60, 85, 115);

            _lblCanalValor.Location = new Point(348, 66);
            _lblCanalValor.Size = new Size(160, 20);
            _lblCanalValor.Font = new Font("Segoe UI", 10F);
            _lblCanalValor.ForeColor = Color.FromArgb(40, 60, 90);

            _btnEnviar.Location = new Point(520, 60);
            _btnEnviar.Size = new Size(190, 34);
            _btnEnviar.FlatStyle = FlatStyle.Flat;
            _btnEnviar.BackColor = Color.FromArgb(45, 108, 223);
            _btnEnviar.ForeColor = Color.White;
            _btnEnviar.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _btnEnviar.FlatAppearance.BorderSize = 0;
            _btnEnviar.Cursor = Cursors.Hand;
            _btnEnviar.Click += (s, e) => EnviarReporte();

            _lblEstado.Location = new Point(14, 104);
            _lblEstado.Size = new Size(900, 40);
            _lblEstado.Font = new Font("Segoe UI", 9.5F);
            _lblEstado.ForeColor = Color.FromArgb(30, 130, 75);

            _panelReporte.Controls.Add(_lblReportar);
            _panelReporte.Controls.Add(_lblValor);
            _panelReporte.Controls.Add(_numValor);
            _panelReporte.Controls.Add(_lblFecha);
            _panelReporte.Controls.Add(_dtpFecha);
            _panelReporte.Controls.Add(_lblCanal);
            _panelReporte.Controls.Add(_lblCanalValor);
            _panelReporte.Controls.Add(_btnEnviar);
            _panelReporte.Controls.Add(_lblEstado);

            // ---- Sección de historial ----
            _panelHistorial.Dock = DockStyle.Fill;
            _panelHistorial.BackColor = Color.White;
            _panelHistorial.Padding = new Padding(12, 8, 12, 12);

            _lblHistorial.Dock = DockStyle.Top;
            _lblHistorial.Height = 30;
            _lblHistorial.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _lblHistorial.ForeColor = Color.FromArgb(41, 98, 176);

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

            _panelHistorial.Controls.Add(_grillaHistorial);
            _panelHistorial.Controls.Add(_lblHistorial);

            Controls.Add(_panelHistorial);
            Controls.Add(_panelReporte);
            Controls.Add(_lblBienvenida);
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
            _lblReportar.Text = Localizacion("portal.reportar");
            _lblValor.Text = Localizacion("rin.valor");
            _lblFecha.Text = Localizacion("rin.fecha");
            _lblCanal.Text = Localizacion("rin.canal");
            _lblCanalValor.Text = Localizacion("rin.canal.digital");
            _btnEnviar.Text = Localizacion("portal.enviar");
            _lblHistorial.Text = Localizacion("portal.historial");
            _grillaHistorial.Columns["fecha"].HeaderText = Localizacion("rin.columna.fecha");
            _grillaHistorial.Columns["tipo"].HeaderText = Localizacion("reportes.columna.tipo");
            _grillaHistorial.Columns["descripcion"].HeaderText = Localizacion("reportes.columna.descripcion");

            _lblBienvenida.Text = _paciente != null
                ? $"{Localizacion("portal.bienvenida")} {_paciente.NombreCompleto}"
                : Localizacion("portal.sinVinculo");
        }

        private void ResolverPaciente()
        {
            try
            {
                using var contexto = new NegocioDbContext();
                Usuario? usuarioNegocio = contexto.Usuarios.AsNoTracking()
                    .FirstOrDefault(u => u.NombreUsuario == _nombreUsuario);
                _paciente = usuarioNegocio == null
                    ? null
                    : contexto.Pacientes.AsNoTracking().FirstOrDefault(p => p.IdUsuarioPortal == usuarioNegocio.Id);

                if (_paciente != null)
                {
                    _lblBienvenida.Text = $"{Localizacion("portal.bienvenida")} {_paciente.NombreCompleto}";
                }

                bool puedeReportar = SeguridadService.TienePermiso(_idUsuario, "PORTAL_REPORTAR_RIN");
                bool puedeVer = SeguridadService.TienePermiso(_idUsuario, "PORTAL_VER_HISTORIAL");
                _panelReporte.Visible = puedeReportar;
                _panelHistorial.Visible = puedeVer;

                if (_paciente == null)
                {
                    _lblBienvenida.Text = Localizacion("portal.sinVinculo");
                    _panelReporte.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "PortalControl", mostrarMensaje: false);
            }
        }

        private void CargarHistorial()
        {
            try
            {
                using var contexto = new NegocioDbContext();
                List<ItemHistorial> items = new ReporteLogic(contexto).ObtenerHistorialPaciente(_paciente!.Id);
                _grillaHistorial.Rows.Clear();
                foreach (ItemHistorial item in items)
                {
                    _grillaHistorial.Rows.Add(item.Fecha.ToString("dd/MM/yyyy"), item.Tipo, item.Descripcion);
                }
                _grillaHistorial.ClearSelection();
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "PortalControl", mostrarMensaje: false);
            }
        }

        private void EnviarReporte()
        {
            if (_paciente == null)
            {
                return;
            }

            try
            {
                using var contexto = new NegocioDbContext();
                var medicion = new MedicionRIN
                {
                    IdPaciente = _paciente.Id,
                    ValorRIN = _numValor.Value,
                    FechaMedicion = _dtpFecha.Value.Date,
                    Canal = CanalReporteRIN.Digital
                };

                ResultadoReporteRIN resultado = new MedicionRINLogic(contexto).Reportar(medicion, _nombreUsuario);

                _lblEstado.ForeColor = Color.FromArgb(30, 130, 75);
                _lblEstado.Text = Localizacion("portal.gracias") +
                    (resultado.AlertaGenerada != null ? $"  {Localizacion("portal.notificado")}" : string.Empty);
                CargarHistorial();
            }
            catch (ValidacionNegocioException ex)
            {
                _lblEstado.ForeColor = Color.FromArgb(180, 40, 40);
                _lblEstado.Text = string.Join(Environment.NewLine, ex.Errores);
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "PortalControl");
            }
        }
    }
}
