using System.Text;
using Negocio.BLL;
using Negocio.DAL.Context;
using Services.DomainModel;
using Services.Facade;

namespace Negocio.UI
{
    /// <summary>
    /// Ventana principal (shell): cabecera de sesión con indicador de integridad (DVH/DVV),
    /// menú de módulos habilitados según los permisos del perfil (REQ-ARQ-006) y área de
    /// contenido donde se incrustan las pantallas de cada módulo (UserControls). Permite
    /// cambiar el idioma en vivo (REQ-ARQ-001).
    /// </summary>
    public class PrincipalForm : Form
    {
        private List<Idioma> _idiomasDisponibles = new();

        private readonly UsuarioAutenticado _sesion;
        private readonly Panel _panelCabecera = new();
        private readonly FlowLayoutPanel _flowCabecera = new();
        private readonly Label _lblApp = new();
        private readonly PictureBox _picLogo = new();
        private readonly Label _lblSesion = new();
        private readonly Label _lblIntegridad = new();
        private readonly Button _btnCerrarSesion = new();
        private readonly ComboBox _cmbIdioma = new();
        private readonly Panel _panelMenu = new();
        private readonly Label _lblMenuTitulo = new();
        private readonly FlowLayoutPanel _flowModulos = new();
        private readonly Panel _panelContenido = new();
        private readonly Panel _panelTitulo = new();
        private readonly Label _lblModuloTitulo = new();
        private readonly Label _lblModuloDetalle = new();
        private readonly Panel _panelVista = new();
        private readonly List<(Button Boton, string Clave)> _botonesModulo = new();
        private PanelTriajeControl? _controlTriaje;
        private PacientesControl? _controlPacientes;
        private RecepcionRINControl? _controlRecepcion;
        private AgendaControl? _controlAgenda;
        private SeguimientoControl? _controlSeguimiento;
        private EventosAdversosControl? _controlEventos;
        private ReportesControl? _controlReportes;
        private PortalControl? _controlPortal;
        private SistemaControl? _controlSistema;
        private string? _claveModuloActual;
        private bool _actualizandoIdioma;
        private int _problemasIntegridad = -1;

        public PrincipalForm(UsuarioAutenticado sesion)
        {
            _sesion = sesion ?? throw new ArgumentNullException(nameof(sesion));
            System.Drawing.Icon? icono = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (icono != null)
            {
                Icon = icono;
            }
            ConstruirInterfaz();
            ConstruirMenu();
            AplicarTextos();
            VerificarIntegridad();

            // Observer de localización (T05): al cambiar el idioma o editar sus leyendas,
            // las vistas se actualizan en caliente sin intervención manual.
            LocalizationService.Suscribir(AlCambiarIdioma);
            FormClosed += (s, e) => LocalizationService.Desuscribir(AlCambiarIdioma);
        }

        /// <summary>Observador de localización (T05): reaplica los textos al cambiar el idioma.</summary>
        private void AlCambiarIdioma()
        {
            AplicarTextos();
        }

        private static string Texto(string clave) => LocalizationService.ObtenerTexto(clave);

        private void ConstruirInterfaz()
        {
            Text = "OpenRIN";
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1024, 640);
            BackColor = Color.FromArgb(238, 246, 253);

            // ---- Cabecera ----
            _panelCabecera.Dock = DockStyle.Top;
            _panelCabecera.Height = 74;
            _panelCabecera.BackColor = Color.FromArgb(41, 98, 176);

            _lblApp.Location = new Point(80, 10);
            _lblApp.Size = new Size(300, 34);
            _lblApp.Font = new Font("Segoe UI", 19F, FontStyle.Bold);
            _lblApp.ForeColor = Color.White;
            _lblApp.Text = "OpenRIN";

            _picLogo.Location = new Point(22, 13);
            _picLogo.Size = new Size(46, 46);
            _picLogo.SizeMode = PictureBoxSizeMode.Zoom;
            _picLogo.BackColor = Color.Transparent;
            _picLogo.Image = Recursos.CargarIcono(64)?.ToBitmap();

            _lblSesion.Location = new Point(82, 46);
            _lblSesion.Size = new Size(640, 20);
            _lblSesion.Font = new Font("Segoe UI", 9.5F);
            _lblSesion.ForeColor = Color.FromArgb(214, 232, 250);

            _flowCabecera.Dock = DockStyle.Right;
            _flowCabecera.Width = 560;
            _flowCabecera.FlowDirection = FlowDirection.RightToLeft;
            _flowCabecera.Padding = new Padding(0, 18, 16, 0);
            _flowCabecera.BackColor = Color.FromArgb(41, 98, 176);
            _flowCabecera.WrapContents = false;

            _btnCerrarSesion.Size = new Size(130, 36);
            _btnCerrarSesion.FlatStyle = FlatStyle.Flat;
            _btnCerrarSesion.FlatAppearance.BorderColor = Color.FromArgb(160, 195, 232);
            _btnCerrarSesion.BackColor = Color.FromArgb(57, 120, 198);
            _btnCerrarSesion.ForeColor = Color.White;
            _btnCerrarSesion.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _btnCerrarSesion.Cursor = Cursors.Hand;
            _btnCerrarSesion.Click += BtnCerrarSesion_Click;

            _cmbIdioma.Size = new Size(170, 28);
            _cmbIdioma.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbIdioma.Font = new Font("Segoe UI", 9.5F);
            _cmbIdioma.Margin = new Padding(10, 4, 6, 0);
            _cmbIdioma.SelectedIndexChanged += CmbIdioma_SelectedIndexChanged;

            _lblIntegridad.Size = new Size(170, 36);
            _lblIntegridad.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _lblIntegridad.TextAlign = ContentAlignment.MiddleCenter;
            _lblIntegridad.Margin = new Padding(0, 0, 10, 0);
            _lblIntegridad.ForeColor = Color.FromArgb(214, 232, 250);

            _flowCabecera.Controls.Add(_btnCerrarSesion);
            _flowCabecera.Controls.Add(_cmbIdioma);
            _flowCabecera.Controls.Add(_lblIntegridad);

            _panelCabecera.Controls.Add(_picLogo);
            _panelCabecera.Controls.Add(_lblApp);
            _panelCabecera.Controls.Add(_lblSesion);
            _panelCabecera.Controls.Add(_flowCabecera);

            // ---- Menú lateral ----
            _panelMenu.Dock = DockStyle.Left;
            _panelMenu.Width = 248;
            _panelMenu.BackColor = Color.White;

            _lblMenuTitulo.Location = new Point(16, 16);
            _lblMenuTitulo.Size = new Size(216, 24);
            _lblMenuTitulo.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _lblMenuTitulo.ForeColor = Color.FromArgb(41, 98, 176);

            _flowModulos.Location = new Point(0, 52);
            _flowModulos.Size = new Size(248, 700);
            _flowModulos.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            _flowModulos.FlowDirection = FlowDirection.TopDown;
            _flowModulos.WrapContents = false;
            _flowModulos.AutoScroll = true;
            _flowModulos.BackColor = Color.White;

            _panelMenu.Controls.Add(_lblMenuTitulo);
            _panelMenu.Controls.Add(_flowModulos);

            // ---- Contenido (título + vista del módulo) ----
            _panelContenido.Dock = DockStyle.Fill;
            _panelContenido.BackColor = Color.FromArgb(244, 249, 254);

            _panelTitulo.Dock = DockStyle.Top;
            _panelTitulo.Height = 104;
            _panelTitulo.BackColor = Color.FromArgb(244, 249, 254);

            _lblModuloTitulo.Location = new Point(36, 22);
            _lblModuloTitulo.Size = new Size(900, 40);
            _lblModuloTitulo.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            _lblModuloTitulo.ForeColor = Color.FromArgb(30, 66, 120);

            _lblModuloDetalle.Location = new Point(38, 68);
            _lblModuloDetalle.Size = new Size(900, 24);
            _lblModuloDetalle.Font = new Font("Segoe UI", 10.5F);
            _lblModuloDetalle.ForeColor = Color.FromArgb(90, 110, 135);

            _panelTitulo.Controls.Add(_lblModuloTitulo);
            _panelTitulo.Controls.Add(_lblModuloDetalle);

            _panelVista.Dock = DockStyle.Fill;
            _panelVista.BackColor = Color.FromArgb(244, 249, 254);

            _panelContenido.Controls.Add(_panelVista);
            _panelContenido.Controls.Add(_panelTitulo);

            Controls.Add(_panelContenido);
            Controls.Add(_panelMenu);
            Controls.Add(_panelCabecera);
        }

        private void ConstruirMenu()
        {
            bool sysadmin = string.Equals(_sesion.Perfil, "sysadmin", StringComparison.OrdinalIgnoreCase);
            bool tiene(string permiso) => SeguridadService.TienePermiso(_sesion.Id, permiso);

            AgregarModulo("mod.pacientes", sysadmin || tiene("PA002") || tiene("PA006"));
            AgregarModulo("panel.titulo", sysadmin || tiene("PA001"));
            // Recepción de RIN es una pantalla operativa del personal (no del portal del paciente).
            AgregarModulo("mod.rin", sysadmin || tiene("PA004"));
            AgregarModulo("mod.agenda", sysadmin || tiene("PA003"));
            AgregarModulo("mod.seguimiento", sysadmin || tiene("PA007"));
            AgregarModulo("mod.eventos", sysadmin || tiene("PA005"));
            AgregarModulo("mod.reportes", sysadmin || tiene("PA008"));
            AgregarModulo("mod.portal", sysadmin || tiene("PO001") || tiene("PO002"));
            AgregarModulo("mod.sistema", sysadmin || tiene("SI001"));
        }

        private void AgregarModulo(string clave, bool permitido)
        {
            if (!permitido)
            {
                return;
            }

            var boton = new Button
            {
                Width = 210,
                Height = 44,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0),
                Margin = new Padding(14, 4, 12, 4),
                BackColor = Color.FromArgb(232, 242, 252),
                ForeColor = Color.FromArgb(28, 60, 105),
                Font = new Font("Segoe UI", 9.5F),
                Cursor = Cursors.Hand
            };
            boton.FlatAppearance.BorderColor = Color.FromArgb(202, 220, 240);
            boton.Click += (s, e) => MostrarModulo(clave);
            boton.MouseEnter += (s, e) => boton.BackColor = Color.FromArgb(214, 233, 250);
            boton.MouseLeave += (s, e) => boton.BackColor = Color.FromArgb(232, 242, 252);

            _botonesModulo.Add((boton, clave));
            _flowModulos.Controls.Add(boton);
        }

        private void MostrarModulo(string clave)
        {
            _controlTriaje?.Pausar();
            _claveModuloActual = clave;
            _lblModuloTitulo.Text = Texto(clave);

            if (clave == "panel.titulo")
            {
                _lblModuloDetalle.Text = string.Empty;
                _controlTriaje ??= new PanelTriajeControl();
                _panelVista.Controls.Clear();
                _panelVista.Controls.Add(_controlTriaje);
                _controlTriaje.Reanudar();
            }
            else if (clave == "mod.pacientes")
            {
                _lblModuloDetalle.Text = string.Empty;
                _controlPacientes ??= new PacientesControl(_sesion.Id, _sesion.NombreUsuario);
                _panelVista.Controls.Clear();
                _panelVista.Controls.Add(_controlPacientes);
                _controlPacientes.Recargar();
            }
            else if (clave == "mod.rin")
            {
                _lblModuloDetalle.Text = string.Empty;
                _controlRecepcion ??= new RecepcionRINControl(_sesion.NombreUsuario);
                _panelVista.Controls.Clear();
                _panelVista.Controls.Add(_controlRecepcion);
                _controlRecepcion.RefrescarTextos();
            }
            else if (clave == "mod.agenda")
            {
                _lblModuloDetalle.Text = string.Empty;
                _controlAgenda ??= new AgendaControl(_sesion.Id, _sesion.NombreUsuario);
                _panelVista.Controls.Clear();
                _panelVista.Controls.Add(_controlAgenda);
                _controlAgenda.Recargar();
            }
            else if (clave == "mod.seguimiento")
            {
                _lblModuloDetalle.Text = string.Empty;
                _controlSeguimiento ??= new SeguimientoControl(_sesion.Id, _sesion.NombreUsuario);
                _panelVista.Controls.Clear();
                _panelVista.Controls.Add(_controlSeguimiento);
                _controlSeguimiento.Recargar();
            }
            else if (clave == "mod.eventos")
            {
                _lblModuloDetalle.Text = string.Empty;
                _controlEventos ??= new EventosAdversosControl(_sesion.NombreUsuario);
                _panelVista.Controls.Clear();
                _panelVista.Controls.Add(_controlEventos);
                _controlEventos.Recargar();
            }
            else if (clave == "mod.reportes")
            {
                _lblModuloDetalle.Text = string.Empty;
                _controlReportes ??= new ReportesControl(_sesion.NombreUsuario);
                _panelVista.Controls.Clear();
                _panelVista.Controls.Add(_controlReportes);
                _controlReportes.Recargar();
            }
            else if (clave == "mod.portal")
            {
                _lblModuloDetalle.Text = string.Empty;
                _controlPortal ??= new PortalControl(_sesion.Id, _sesion.NombreUsuario);
                _panelVista.Controls.Clear();
                _panelVista.Controls.Add(_controlPortal);
                _controlPortal.Recargar();
            }
            else if (clave == "mod.sistema")
            {
                _lblModuloDetalle.Text = string.Empty;
                _controlSistema ??= new SistemaControl();
                _panelVista.Controls.Clear();
                _panelVista.Controls.Add(_controlSistema);
                _controlSistema.Recargar();
            }
            else
            {
                _lblModuloDetalle.Text = Texto("shell.enConstruccion");
                _panelVista.Controls.Clear();
            }
        }

        /// <summary>
        /// Verifica la integridad (DVH/DVV) al iniciar sesión: actualiza el indicador de la
        /// cabecera y, si hay problemas, los informa con su detalle.
        /// </summary>
        private void VerificarIntegridad()
        {
            try
            {
                using var contexto = new NegocioDbContext();
                ResultadoAuditoria resultado = new IntegridadLogic(contexto).Auditar();
                _problemasIntegridad = resultado.Problemas.Count;
                ActualizarIndicadorIntegridad();

                if (_problemasIntegridad > 0)
                {
                    var detalle = new StringBuilder();
                    detalle.AppendLine(Texto("integridad.mensaje"));
                    detalle.AppendLine();
                    foreach (string problema in resultado.Problemas.Take(10))
                    {
                        detalle.AppendLine("• " + problema);
                    }
                    if (resultado.Problemas.Count > 10)
                    {
                        detalle.AppendLine($"… (+{resultado.Problemas.Count - 10})");
                    }
                    MessageBox.Show(this, detalle.ToString(), Texto("integridad.titulo"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "PrincipalForm", mostrarMensaje: false);
                _problemasIntegridad = -1;
                ActualizarIndicadorIntegridad();
            }
        }

        private void ActualizarIndicadorIntegridad()
        {
            if (_problemasIntegridad < 0)
            {
                _lblIntegridad.Text = "—";
                _lblIntegridad.ForeColor = Color.FromArgb(200, 214, 230);
            }
            else if (_problemasIntegridad == 0)
            {
                _lblIntegridad.Text = Texto("integridad.ok");
                _lblIntegridad.ForeColor = Color.FromArgb(190, 245, 205);
            }
            else
            {
                _lblIntegridad.Text = $"{Texto("integridad.alerta")} {_problemasIntegridad}";
                _lblIntegridad.ForeColor = Color.FromArgb(255, 190, 190);
            }
        }

        private void AplicarTextos()
        {
            Text = $"{Texto("app.nombre")} — {Texto("shell.titulo")}";
            _lblSesion.Text = $"{Texto("shell.sesion")} {_sesion.NombreCompleto} ({_sesion.Perfil})";
            _lblMenuTitulo.Text = Texto("shell.modulos");
            _btnCerrarSesion.Text = Texto("shell.cerrarSesion");

            foreach ((Button boton, string clave) in _botonesModulo)
            {
                boton.Text = Texto(clave);
            }

            if (_claveModuloActual == null)
            {
                _lblModuloTitulo.Text = Texto("shell.titulo");
                _lblModuloDetalle.Text = Texto("shell.seleccione");
            }
            else
            {
                MostrarModulo(_claveModuloActual);
            }

            _controlTriaje?.RefrescarTextos();
            _controlPacientes?.RefrescarTextos();
            _controlAgenda?.RefrescarTextos();
            _controlSeguimiento?.RefrescarTextos();
            _controlEventos?.RefrescarTextos();
            _controlReportes?.RefrescarTextos();
            _controlPortal?.RefrescarTextos();
            _controlSistema?.RefrescarTextos();
            ActualizarIndicadorIntegridad();

            _actualizandoIdioma = true;
            try
            {
                _cmbIdioma.Items.Clear();
                _idiomasDisponibles = LocalizationService.Idiomas.ToList();
                foreach (Idioma idioma in _idiomasDisponibles)
                {
                    _cmbIdioma.Items.Add(idioma.Nombre);
                }
                int indice = _idiomasDisponibles.FindIndex(i => i.Codigo == LocalizationService.IdiomaActual);
                _cmbIdioma.SelectedIndex = indice >= 0 ? indice : 0;
            }
            finally
            {
                _actualizandoIdioma = false;
            }
        }

        private void CmbIdioma_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_actualizandoIdioma || _cmbIdioma.SelectedIndex < 0)
            {
                return;
            }
            LocalizationService.EstablecerIdioma(_idiomasDisponibles[_cmbIdioma.SelectedIndex].Codigo);
        }

        private void BtnCerrarSesion_Click(object? sender, EventArgs e)
        {
            DialogResult respuesta = MessageBox.Show(this, Texto("shell.confirmarCerrar"), Texto("shell.cerrarSesion"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (respuesta == DialogResult.Yes)
            {
                SesionActual.Instancia.Cerrar();
                Application.Restart();
            }
        }
    }
}
