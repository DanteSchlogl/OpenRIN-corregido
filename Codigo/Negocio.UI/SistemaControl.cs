using System.Text.Encodings.Web;
using System.Text.Json;
using Negocio.BLL;
using Negocio.DAL.Context;
using Negocio.DomainModel;
using Services.DomainModel;
using Services.Facade;

namespace Negocio.UI
{
    /// <summary>
    /// Administración del sistema (REQ-ARQ-004/006, solo perfil sysadmin): gestión de usuarios
    /// (listado, alta vía la API de seguridad, habilitación/deshabilitación) y consulta de la
    /// bitácora centralizada con filtro por nivel mínimo.
    /// </summary>
    public class SistemaControl : UserControl
    {
        private readonly Button _btnVistaUsuarios = new();
        private readonly Button _btnVistaBitacora = new();
        private readonly Button _btnVistaCambios = new();
        private readonly Button _btnVistaIdiomas = new();
        private readonly Button _btnVistaPerfiles = new();

        private readonly Panel _panelUsuarios = new();
        private readonly Button _btnNuevoUsuario = new();
        private readonly Button _btnHabilitar = new();
        private readonly Button _btnDeshabilitar = new();
        private readonly DataGridView _grillaUsuarios = new();

        private readonly Panel _panelBitacora = new();
        private readonly Label _lblNivel = new();
        private readonly ComboBox _cmbNivel = new();
        private readonly Button _btnActualizar = new();
        private readonly DataGridView _grillaBitacora = new();

        private readonly Panel _panelCambios = new();
        private readonly Label _lblEntidadCambios = new();
        private readonly ComboBox _cmbEntidadCambios = new();
        private readonly Label _lblIdCambios = new();
        private readonly TextBox _txtIdCambios = new();
        private readonly Button _btnBuscarCambios = new();
        private readonly DataGridView _grillaCambios = new();
        private readonly Label _lblCambioAnterior = new();
        private readonly Label _lblCambioNuevo = new();
        private readonly TextBox _txtCambioAnterior = new();
        private readonly TextBox _txtCambioNuevo = new();
        private readonly Button _btnRestaurarCambio = new();
        private List<CambioAuditado> _cambios = new();

        private readonly Panel _panelIdiomas = new();
        private readonly DataGridView _grillaIdiomas = new();
        private readonly Button _btnNuevoIdioma = new();
        private readonly Button _btnEliminarIdioma = new();
        private readonly ComboBox _cmbIdiomaEditor = new();
        private readonly DataGridView _grillaTextos = new();
        private readonly TextBox _txtNuevaClave = new();
        private readonly TextBox _txtNuevoValor = new();
        private readonly Button _btnAgregarLeyenda = new();
        private readonly Button _btnGuardarTextos = new();
        private List<Idioma> _idiomasEditor = new();
        private List<TextoLocalizacion> _textosEditor = new();

        private readonly Panel _panelPerfiles = new();
        private readonly ComboBox _cmbPerfiles = new();
        private readonly TreeView _treePermisos = new();
        private readonly Button _btnGuardarPermisos = new();
        private readonly Button _btnMarcarTodo = new();
        private readonly Button _btnDesmarcarTodo = new();
        private bool _actualizandoArbol;

        private static readonly string[] EntidadesAuditables =
        {
            "Pacientes", "ObrasSociales", "Diagnosticos", "HistoriasClinicas", "EventosAdversos",
            "MedicionesRIN", "Alertas", "Turnos", "Seguimientos", "Usuarios", "ReportesEstadisticos"
        };

        private int _vistaActual;
        private List<UsuarioListado> _usuarios = new();

        public SistemaControl()
        {
            ConstruirInterfaz();
            AplicarTextos();
            MostrarVista(0);
            CargarUsuarios();
        }

        private static string Localizacion(string clave) => LocalizationService.ObtenerTexto(clave);

        /// <summary>Recarga la vista activa (al entrar al módulo).</summary>
        public void Recargar()
        {
            switch (_vistaActual)
            {
                case 0:
                    CargarUsuarios();
                    break;
                case 1:
                    CargarBitacora();
                    break;
                case 2:
                    CargarCambios();
                    break;
                case 3:
                    CargarIdiomas();
                    break;
                case 4:
                    CargarPerfiles();
                    break;
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

            // ---- Barra superior: vistas ----
            var panelBarra = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = BackColor };
            ConfigurarBotonVista(_btnVistaUsuarios, 0, 0);
            ConfigurarBotonVista(_btnVistaBitacora, 176, 1);
            ConfigurarBotonVista(_btnVistaCambios, 352, 2);
            ConfigurarBotonVista(_btnVistaIdiomas, 528, 3);
            ConfigurarBotonVista(_btnVistaPerfiles, 704, 4);
            panelBarra.Controls.Add(_btnVistaUsuarios);
            panelBarra.Controls.Add(_btnVistaBitacora);
            panelBarra.Controls.Add(_btnVistaCambios);
            panelBarra.Controls.Add(_btnVistaIdiomas);
            panelBarra.Controls.Add(_btnVistaPerfiles);

            // ---- Vista 1: usuarios ----
            _panelUsuarios.Dock = DockStyle.Fill;
            _panelUsuarios.BackColor = Color.White;

            _btnNuevoUsuario.Location = new Point(14, 12);
            _btnNuevoUsuario.Size = new Size(180, 32);
            EstiloPrimario(_btnNuevoUsuario);
            _btnNuevoUsuario.Click += (s, e) => AbrirAltaUsuario();

            _btnHabilitar.Location = new Point(204, 12);
            _btnHabilitar.Size = new Size(140, 32);
            EstiloSecundario(_btnHabilitar);
            _btnHabilitar.Click += (s, e) => CambiarEstadoSeleccionado(true);

            _btnDeshabilitar.Location = new Point(354, 12);
            _btnDeshabilitar.Size = new Size(150, 32);
            EstiloSecundario(_btnDeshabilitar);
            _btnDeshabilitar.Click += (s, e) => CambiarEstadoSeleccionado(false);

            var hostUsuarios = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 56, 14, 14) };
            _grillaUsuarios.Dock = DockStyle.Fill;
            EstiloGrilla(_grillaUsuarios);
            _grillaUsuarios.Columns.Add("usuario", "");
            _grillaUsuarios.Columns.Add("nombre", "");
            _grillaUsuarios.Columns.Add("perfil", "");
            _grillaUsuarios.Columns.Add("email", "");
            _grillaUsuarios.Columns.Add("activo", "");
            _grillaUsuarios.Columns.Add("intentos", "");
            _grillaUsuarios.Columns.Add("bloqueado", "");
            _grillaUsuarios.Columns["usuario"].FillWeight = 16;
            _grillaUsuarios.Columns["nombre"].FillWeight = 24;
            _grillaUsuarios.Columns["perfil"].FillWeight = 14;
            _grillaUsuarios.Columns["email"].FillWeight = 18;
            _grillaUsuarios.Columns["activo"].FillWeight = 8;
            _grillaUsuarios.Columns["intentos"].FillWeight = 8;
            _grillaUsuarios.Columns["bloqueado"].FillWeight = 12;
            foreach (string columna in new[] { "perfil", "activo", "intentos", "bloqueado" })
            {
                _grillaUsuarios.Columns[columna].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            _panelUsuarios.Controls.Add(hostUsuarios);
            hostUsuarios.Controls.Add(_grillaUsuarios);
            _panelUsuarios.Controls.Add(_btnNuevoUsuario);
            _panelUsuarios.Controls.Add(_btnHabilitar);
            _panelUsuarios.Controls.Add(_btnDeshabilitar);
            _btnNuevoUsuario.BringToFront();
            _btnHabilitar.BringToFront();
            _btnDeshabilitar.BringToFront();

            // ---- Vista 2: bitácora ----
            _panelBitacora.Dock = DockStyle.Fill;
            _panelBitacora.BackColor = Color.White;

            _lblNivel.Location = new Point(14, 20);
            _lblNivel.Size = new Size(110, 20);
            _lblNivel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _lblNivel.ForeColor = Color.FromArgb(60, 85, 115);

            _cmbNivel.Location = new Point(128, 16);
            _cmbNivel.Size = new Size(160, 28);
            _cmbNivel.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbNivel.Font = new Font("Segoe UI", 9.5F);

            _btnActualizar.Location = new Point(300, 15);
            _btnActualizar.Size = new Size(140, 30);
            EstiloSecundario(_btnActualizar);
            _btnActualizar.Click += (s, e) => CargarBitacora();

            var hostBitacora = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 56, 14, 14) };
            _grillaBitacora.Dock = DockStyle.Fill;
            EstiloGrilla(_grillaBitacora);
            _grillaBitacora.Columns.Add("fecha", "");
            _grillaBitacora.Columns.Add("nivel", "");
            _grillaBitacora.Columns.Add("capa", "");
            _grillaBitacora.Columns.Add("usuario", "");
            _grillaBitacora.Columns.Add("mensaje", "");
            _grillaBitacora.Columns["fecha"].FillWeight = 14;
            _grillaBitacora.Columns["nivel"].FillWeight = 9;
            _grillaBitacora.Columns["capa"].FillWeight = 12;
            _grillaBitacora.Columns["usuario"].FillWeight = 14;
            _grillaBitacora.Columns["mensaje"].FillWeight = 51;
            foreach (string columna in new[] { "nivel", "capa" })
            {
                _grillaBitacora.Columns[columna].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            // Las celdas de texto largo crecen en alto para mostrar el contenido completo.
            _grillaBitacora.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            _grillaBitacora.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            _panelBitacora.Controls.Add(hostBitacora);
            hostBitacora.Controls.Add(_grillaBitacora);
            _panelBitacora.Controls.Add(_lblNivel);
            _panelBitacora.Controls.Add(_cmbNivel);
            _panelBitacora.Controls.Add(_btnActualizar);
            _lblNivel.BringToFront();
            _cmbNivel.BringToFront();
            _btnActualizar.BringToFront();

            // ---- Vista 3: control de cambios (T06b) ----
            _panelCambios.Dock = DockStyle.Fill;
            _panelCambios.BackColor = Color.White;

            _lblEntidadCambios.Location = new Point(14, 20);
            _lblEntidadCambios.Size = new Size(70, 20);
            _lblEntidadCambios.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _lblEntidadCambios.ForeColor = Color.FromArgb(60, 85, 115);

            _cmbEntidadCambios.Location = new Point(88, 16);
            _cmbEntidadCambios.Size = new Size(200, 28);
            _cmbEntidadCambios.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbEntidadCambios.Font = new Font("Segoe UI", 9.5F);

            _lblIdCambios.Location = new Point(304, 20);
            _lblIdCambios.Size = new Size(130, 20);
            _lblIdCambios.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _lblIdCambios.ForeColor = Color.FromArgb(60, 85, 115);

            _txtIdCambios.Location = new Point(438, 16);
            _txtIdCambios.Size = new Size(90, 28);
            _txtIdCambios.Font = new Font("Segoe UI", 9.5F);

            _btnBuscarCambios.Location = new Point(544, 15);
            _btnBuscarCambios.Size = new Size(160, 30);
            EstiloSecundario(_btnBuscarCambios);
            _btnBuscarCambios.Click += (s, e) => CargarCambios();

            var hostCambios = new Panel { Dock = DockStyle.Top, Height = 260, Padding = new Padding(14, 56, 14, 6) };
            _grillaCambios.Dock = DockStyle.Fill;
            EstiloGrilla(_grillaCambios);
            _grillaCambios.Columns.Add("fecha", "");
            _grillaCambios.Columns.Add("usuario", "");
            _grillaCambios.Columns.Add("tipo", "");
            _grillaCambios.Columns.Add("entidad", "");
            _grillaCambios.Columns.Add("registro", "");
            _grillaCambios.Columns["fecha"].FillWeight = 18;
            _grillaCambios.Columns["usuario"].FillWeight = 16;
            _grillaCambios.Columns["tipo"].FillWeight = 14;
            _grillaCambios.Columns["entidad"].FillWeight = 30;
            _grillaCambios.Columns["registro"].FillWeight = 12;
            foreach (string columna in new[] { "tipo", "registro" })
            {
                _grillaCambios.Columns[columna].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            _grillaCambios.SelectionChanged += (s, e) => MostrarDetalleCambio();

            var panelRestaurar = new Panel { Dock = DockStyle.Bottom, Height = 54, Padding = new Padding(0, 10, 0, 12) };
            _btnRestaurarCambio.Dock = DockStyle.Right;
            _btnRestaurarCambio.Width = 250;
            EstiloPrimario(_btnRestaurarCambio);
            _btnRestaurarCambio.Click += (s, e) => RestaurarCambioSeleccionado();
            panelRestaurar.Controls.Add(_btnRestaurarCambio);

            var hostDetalleCambios = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 6, 14, 14) };
            var tablaDetalle = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
            tablaDetalle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tablaDetalle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tablaDetalle.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            tablaDetalle.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _lblCambioAnterior.Dock = DockStyle.Fill;
            _lblCambioAnterior.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _lblCambioAnterior.ForeColor = Color.FromArgb(60, 85, 115);
            _lblCambioNuevo.Dock = DockStyle.Fill;
            _lblCambioNuevo.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _lblCambioNuevo.ForeColor = Color.FromArgb(60, 85, 115);

            _txtCambioAnterior.Multiline = true;
            _txtCambioAnterior.ReadOnly = true;
            _txtCambioAnterior.ScrollBars = ScrollBars.Both;
            _txtCambioAnterior.WordWrap = false;
            _txtCambioAnterior.Dock = DockStyle.Fill;
            _txtCambioAnterior.Font = new Font("Consolas", 8.5F);
            _txtCambioAnterior.BackColor = Color.FromArgb(250, 252, 255);
            _txtCambioNuevo.Multiline = true;
            _txtCambioNuevo.ReadOnly = true;
            _txtCambioNuevo.ScrollBars = ScrollBars.Both;
            _txtCambioNuevo.WordWrap = false;
            _txtCambioNuevo.Dock = DockStyle.Fill;
            _txtCambioNuevo.Font = new Font("Consolas", 8.5F);
            _txtCambioNuevo.BackColor = Color.FromArgb(250, 252, 255);

            tablaDetalle.Controls.Add(_lblCambioAnterior, 0, 0);
            tablaDetalle.Controls.Add(_lblCambioNuevo, 1, 0);
            tablaDetalle.Controls.Add(_txtCambioAnterior, 0, 1);
            tablaDetalle.Controls.Add(_txtCambioNuevo, 1, 1);
            hostDetalleCambios.Controls.Add(tablaDetalle);

            _panelCambios.Controls.Add(hostDetalleCambios);
            _panelCambios.Controls.Add(panelRestaurar);
            _panelCambios.Controls.Add(hostCambios);
            hostCambios.Controls.Add(_grillaCambios);
            _panelCambios.Controls.Add(_lblEntidadCambios);
            _panelCambios.Controls.Add(_cmbEntidadCambios);
            _panelCambios.Controls.Add(_lblIdCambios);
            _panelCambios.Controls.Add(_txtIdCambios);
            _panelCambios.Controls.Add(_btnBuscarCambios);
            _lblEntidadCambios.BringToFront();
            _cmbEntidadCambios.BringToFront();
            _lblIdCambios.BringToFront();
            _txtIdCambios.BringToFront();
            _btnBuscarCambios.BringToFront();

            // ---- Vista 4: idiomas (T05) ----
            _panelIdiomas.Dock = DockStyle.Fill;
            _panelIdiomas.BackColor = Color.White;

            var panelIdiomasIzq = new Panel { Dock = DockStyle.Left, Width = 330 };
            _btnNuevoIdioma.Location = new Point(14, 14);
            _btnNuevoIdioma.Size = new Size(150, 32);
            EstiloPrimario(_btnNuevoIdioma);
            _btnNuevoIdioma.Click += (s, e) => AbrirAltaIdioma();

            _btnEliminarIdioma.Location = new Point(172, 14);
            _btnEliminarIdioma.Size = new Size(144, 32);
            EstiloSecundario(_btnEliminarIdioma);
            _btnEliminarIdioma.Click += (s, e) => EliminarIdiomaSeleccionado();

            var hostIdiomas = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 58, 8, 14) };
            _grillaIdiomas.Dock = DockStyle.Fill;
            EstiloGrilla(_grillaIdiomas);
            _grillaIdiomas.Columns.Add("codigo", "");
            _grillaIdiomas.Columns.Add("nombre", "");
            _grillaIdiomas.Columns["codigo"].FillWeight = 30;
            _grillaIdiomas.Columns["nombre"].FillWeight = 70;
            hostIdiomas.Controls.Add(_grillaIdiomas);
            panelIdiomasIzq.Controls.Add(hostIdiomas);
            panelIdiomasIzq.Controls.Add(_btnNuevoIdioma);
            panelIdiomasIzq.Controls.Add(_btnEliminarIdioma);
            _btnNuevoIdioma.BringToFront();
            _btnEliminarIdioma.BringToFront();

            var panelIdiomasDer = new Panel { Dock = DockStyle.Fill };
            _cmbIdiomaEditor.Location = new Point(14, 18);
            _cmbIdiomaEditor.Size = new Size(320, 28);
            _cmbIdiomaEditor.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbIdiomaEditor.Font = new Font("Segoe UI", 9.5F);
            _cmbIdiomaEditor.SelectedIndexChanged += (s, e) => CargarTextosDelIdioma();

            var hostTextos = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 58, 14, 116) };
            _grillaTextos.Dock = DockStyle.Fill;
            EstiloGrilla(_grillaTextos);
            _grillaTextos.ReadOnly = false;
            _grillaTextos.Columns.Add("clave", "");
            _grillaTextos.Columns.Add("valor", "");
            _grillaTextos.Columns["clave"].ReadOnly = true;
            _grillaTextos.Columns["clave"].FillWeight = 38;
            _grillaTextos.Columns["valor"].FillWeight = 62;
            hostTextos.Controls.Add(_grillaTextos);

            var panelTextosAbajo = new Panel { Dock = DockStyle.Bottom, Height = 108 };
            _txtNuevaClave.Location = new Point(14, 12);
            _txtNuevaClave.Size = new Size(220, 28);
            _txtNuevaClave.Font = new Font("Segoe UI", 9.5F);
            _txtNuevoValor.Location = new Point(244, 12);
            _txtNuevoValor.Size = new Size(320, 28);
            _txtNuevoValor.Font = new Font("Segoe UI", 9.5F);
            _btnAgregarLeyenda.Location = new Point(576, 11);
            _btnAgregarLeyenda.Size = new Size(170, 30);
            EstiloSecundario(_btnAgregarLeyenda);
            _btnAgregarLeyenda.Click += (s, e) => AgregarLeyendaInline();
            _btnGuardarTextos.Location = new Point(14, 56);
            _btnGuardarTextos.Size = new Size(220, 34);
            EstiloPrimario(_btnGuardarTextos);
            _btnGuardarTextos.Click += (s, e) => GuardarCambiosTextos();
            panelTextosAbajo.Controls.Add(_txtNuevaClave);
            panelTextosAbajo.Controls.Add(_txtNuevoValor);
            panelTextosAbajo.Controls.Add(_btnAgregarLeyenda);
            panelTextosAbajo.Controls.Add(_btnGuardarTextos);

            panelIdiomasDer.Controls.Add(hostTextos);
            panelIdiomasDer.Controls.Add(panelTextosAbajo);
            panelIdiomasDer.Controls.Add(_cmbIdiomaEditor);
            _cmbIdiomaEditor.BringToFront();

            _panelIdiomas.Controls.Add(panelIdiomasDer);
            _panelIdiomas.Controls.Add(panelIdiomasIzq);

            // ---- Vista 5: perfiles y permisos (T04) ----
            _panelPerfiles.Dock = DockStyle.Fill;
            _panelPerfiles.BackColor = Color.White;

            _cmbPerfiles.Location = new Point(14, 18);
            _cmbPerfiles.Size = new Size(280, 28);
            _cmbPerfiles.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbPerfiles.Font = new Font("Segoe UI", 9.5F);
            _cmbPerfiles.SelectedIndexChanged += (s, e) => CargarArbolDelPerfil();

            _btnMarcarTodo.Location = new Point(310, 17);
            _btnMarcarTodo.Size = new Size(150, 30);
            EstiloSecundario(_btnMarcarTodo);
            _btnMarcarTodo.Click += (s, e) => MarcarTodo(true);

            _btnDesmarcarTodo.Location = new Point(468, 17);
            _btnDesmarcarTodo.Size = new Size(150, 30);
            EstiloSecundario(_btnDesmarcarTodo);
            _btnDesmarcarTodo.Click += (s, e) => MarcarTodo(false);

            _btnGuardarPermisos.Location = new Point(626, 17);
            _btnGuardarPermisos.Size = new Size(200, 30);
            EstiloPrimario(_btnGuardarPermisos);
            _btnGuardarPermisos.Click += (s, e) => GuardarPermisosDelPerfil();

            var hostArbol = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 58, 14, 14) };
            _treePermisos.Dock = DockStyle.Fill;
            _treePermisos.CheckBoxes = true;
            _treePermisos.Font = new Font("Segoe UI", 10F);
            _treePermisos.HideSelection = false;
            _treePermisos.BackColor = Color.White;
            _treePermisos.BorderStyle = BorderStyle.None;
            _treePermisos.AfterCheck += TreePermisos_AfterCheck;
            hostArbol.Controls.Add(_treePermisos);

            _panelPerfiles.Controls.Add(hostArbol);
            _panelPerfiles.Controls.Add(_cmbPerfiles);
            _panelPerfiles.Controls.Add(_btnMarcarTodo);
            _panelPerfiles.Controls.Add(_btnDesmarcarTodo);
            _panelPerfiles.Controls.Add(_btnGuardarPermisos);
            _cmbPerfiles.BringToFront();
            _btnMarcarTodo.BringToFront();
            _btnDesmarcarTodo.BringToFront();
            _btnGuardarPermisos.BringToFront();

            Controls.Add(_panelUsuarios);
            Controls.Add(_panelBitacora);
            Controls.Add(_panelCambios);
            Controls.Add(_panelIdiomas);
            Controls.Add(_panelPerfiles);
            Controls.Add(panelBarra);
        }

        private static void EstiloPrimario(Button boton)
        {
            boton.FlatStyle = FlatStyle.Flat;
            boton.BackColor = Color.FromArgb(45, 108, 223);
            boton.ForeColor = Color.White;
            boton.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            boton.FlatAppearance.BorderSize = 0;
            boton.Cursor = Cursors.Hand;
        }

        private static void EstiloSecundario(Button boton)
        {
            boton.FlatStyle = FlatStyle.Flat;
            boton.BackColor = Color.White;
            boton.ForeColor = Color.FromArgb(28, 60, 105);
            boton.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            boton.FlatAppearance.BorderColor = Color.FromArgb(202, 220, 240);
            boton.Cursor = Cursors.Hand;
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
            grilla.RowTemplate.Height = 30;
            grilla.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(41, 98, 176);
            grilla.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grilla.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            grilla.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(41, 98, 176);
            grilla.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            grilla.DefaultCellStyle.SelectionBackColor = Color.FromArgb(210, 230, 250);
            grilla.DefaultCellStyle.SelectionForeColor = Color.Black;
            grilla.DefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
            grilla.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 251, 255);
        }

        private void ConfigurarBotonVista(Button boton, int x, int vista)
        {
            boton.Location = new Point(x, 15);
            boton.Size = new Size(168, 32);
            boton.FlatStyle = FlatStyle.Flat;
            boton.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            boton.Cursor = Cursors.Hand;
            boton.FlatAppearance.BorderColor = Color.FromArgb(202, 220, 240);
            boton.Click += (s, e) => MostrarVista(vista);
        }

        private void MostrarVista(int vista)
        {
            _vistaActual = vista;
            _panelUsuarios.Visible = vista == 0;
            _panelBitacora.Visible = vista == 1;
            _panelCambios.Visible = vista == 2;
            _panelIdiomas.Visible = vista == 3;
            _panelPerfiles.Visible = vista == 4;
            EstiloBotonVista(_btnVistaUsuarios, vista == 0);
            EstiloBotonVista(_btnVistaBitacora, vista == 1);
            EstiloBotonVista(_btnVistaCambios, vista == 2);
            EstiloBotonVista(_btnVistaIdiomas, vista == 3);
            EstiloBotonVista(_btnVistaPerfiles, vista == 4);
            Recargar();
        }

        private static void EstiloBotonVista(Button boton, bool activo)
        {
            boton.BackColor = activo ? Color.FromArgb(45, 108, 223) : Color.White;
            boton.ForeColor = activo ? Color.White : Color.FromArgb(28, 60, 105);
        }

        private void AplicarTextos()
        {
            _btnVistaUsuarios.Text = Localizacion("sistema.usuarios");
            _btnVistaBitacora.Text = Localizacion("sistema.bitacora");
            _btnNuevoUsuario.Text = Localizacion("sistema.nuevoUsuario");
            _btnHabilitar.Text = Localizacion("sistema.habilitar");
            _btnDeshabilitar.Text = Localizacion("sistema.deshabilitar");
            _grillaUsuarios.Columns["usuario"].HeaderText = Localizacion("sistema.columna.usuario");
            _grillaUsuarios.Columns["nombre"].HeaderText = Localizacion("sistema.columna.nombre");
            _grillaUsuarios.Columns["perfil"].HeaderText = Localizacion("sistema.columna.perfil");
            _grillaUsuarios.Columns["email"].HeaderText = Localizacion("pacientes.email");
            _grillaUsuarios.Columns["activo"].HeaderText = Localizacion("sistema.columna.activo");
            _grillaUsuarios.Columns["intentos"].HeaderText = Localizacion("sistema.columna.intentos");
            _grillaUsuarios.Columns["bloqueado"].HeaderText = Localizacion("sistema.columna.bloqueado");

            _lblNivel.Text = Localizacion("sistema.nivel");
            int previo = _cmbNivel.SelectedIndex;
            _cmbNivel.Items.Clear();
            _cmbNivel.Items.Add(Localizacion("sistema.nivel.todos"));
            _cmbNivel.Items.Add("Info");
            _cmbNivel.Items.Add("Warning");
            _cmbNivel.Items.Add("Error");
            _cmbNivel.SelectedIndex = previo < 0 ? 0 : previo;
            _btnActualizar.Text = Localizacion("sistema.actualizar");
            _grillaBitacora.Columns["fecha"].HeaderText = Localizacion("rin.columna.fecha");
            _grillaBitacora.Columns["nivel"].HeaderText = Localizacion("sistema.columna.nivel");
            _grillaBitacora.Columns["capa"].HeaderText = Localizacion("sistema.columna.capa");
            _grillaBitacora.Columns["usuario"].HeaderText = Localizacion("sistema.columna.usuario");
            _grillaBitacora.Columns["mensaje"].HeaderText = Localizacion("sistema.columna.mensaje");

            _btnVistaCambios.Text = Localizacion("sistema.cambios");
            _lblEntidadCambios.Text = Localizacion("sistema.cambios.entidad");
            _lblIdCambios.Text = Localizacion("sistema.cambios.idRegistro");
            _btnBuscarCambios.Text = Localizacion("sistema.cambios.buscar");
            int seleccionPrevia = _cmbEntidadCambios.SelectedIndex;
            _cmbEntidadCambios.Items.Clear();
            _cmbEntidadCambios.Items.Add(Localizacion("sistema.cambios.todas"));
            foreach (string entidad in EntidadesAuditables)
            {
                _cmbEntidadCambios.Items.Add(entidad);
            }
            _cmbEntidadCambios.SelectedIndex = seleccionPrevia < 0 ? 0 : seleccionPrevia;
            _grillaCambios.Columns["fecha"].HeaderText = Localizacion("rin.columna.fecha");
            _grillaCambios.Columns["usuario"].HeaderText = Localizacion("sistema.columna.usuario");
            _grillaCambios.Columns["tipo"].HeaderText = Localizacion("sistema.cambios.columna.tipo");
            _grillaCambios.Columns["entidad"].HeaderText = Localizacion("sistema.cambios.columna.entidad");
            _grillaCambios.Columns["registro"].HeaderText = Localizacion("sistema.cambios.columna.registro");
            _lblCambioAnterior.Text = Localizacion("sistema.cambios.anterior");
            _lblCambioNuevo.Text = Localizacion("sistema.cambios.nuevo");
            _btnRestaurarCambio.Text = Localizacion("sistema.cambios.restaurar");

            _btnVistaIdiomas.Text = Localizacion("sistema.idiomas");
            _btnNuevoIdioma.Text = Localizacion("idiomas.nuevo");
            _btnEliminarIdioma.Text = Localizacion("idiomas.eliminar");
            _grillaIdiomas.Columns["codigo"].HeaderText = Localizacion("idiomas.codigo");
            _grillaIdiomas.Columns["nombre"].HeaderText = Localizacion("idiomas.nombre");
            _grillaTextos.Columns["clave"].HeaderText = Localizacion("idiomas.clave");
            _grillaTextos.Columns["valor"].HeaderText = Localizacion("idiomas.valor");
            _btnAgregarLeyenda.Text = Localizacion("idiomas.agregar");
            _btnGuardarTextos.Text = Localizacion("idiomas.guardar");

            _btnVistaPerfiles.Text = Localizacion("sistema.perfiles");
            _btnGuardarPermisos.Text = Localizacion("perfiles.guardar");
            _btnMarcarTodo.Text = Localizacion("perfiles.marcarTodo");
            _btnDesmarcarTodo.Text = Localizacion("perfiles.desmarcarTodo");
        }

        private void CargarUsuarios()
        {
            try
            {
                _usuarios = SeguridadService.ObtenerUsuarios();
                _grillaUsuarios.Rows.Clear();
                foreach (UsuarioListado usuario in _usuarios)
                {
                    int indice = _grillaUsuarios.Rows.Add(
                        usuario.NombreUsuario,
                        usuario.NombreCompleto,
                        usuario.Perfil,
                        usuario.Email ?? "—",
                        usuario.Activo ? Localizacion("sistema.si") : Localizacion("sistema.no"),
                        usuario.IntentosFallidos,
                        usuario.BloqueadoHasta?.ToString("dd/MM/yyyy HH:mm") ?? "—");

                    DataGridViewCell celdaActivo = _grillaUsuarios.Rows[indice].Cells[4];
                    celdaActivo.Style.ForeColor = usuario.Activo ? Color.FromArgb(30, 130, 75) : Color.FromArgb(185, 45, 40);
                    celdaActivo.Style.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                    celdaActivo.Style.SelectionForeColor = celdaActivo.Style.ForeColor;
                }
                _grillaUsuarios.ClearSelection();
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "SistemaControl");
            }
        }

        private void CargarBitacora()
        {
            try
            {
                LogLevel? nivel = _cmbNivel.SelectedIndex switch
                {
                    1 => LogLevel.Info,
                    2 => LogLevel.Warning,
                    3 => LogLevel.Error,
                    _ => null
                };

                List<LogEntry> entradas = BitacoraService.ObtenerUltimos(200, nivel);
                _grillaBitacora.Rows.Clear();
                foreach (LogEntry entrada in entradas)
                {
                    int indice = _grillaBitacora.Rows.Add(
                        entrada.Fecha.ToString("dd/MM/yyyy HH:mm:ss"),
                        entrada.Nivel.ToString(),
                        entrada.Capa ?? "—",
                        entrada.Usuario ?? "—",
                        entrada.Mensaje);

                    DataGridViewCell celdaNivel = _grillaBitacora.Rows[indice].Cells[1];
                    if (entrada.Nivel >= LogLevel.Error)
                    {
                        celdaNivel.Style.ForeColor = Color.FromArgb(185, 45, 40);
                        celdaNivel.Style.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                    }
                    else if (entrada.Nivel == LogLevel.Warning)
                    {
                        celdaNivel.Style.ForeColor = Color.FromArgb(190, 125, 20);
                        celdaNivel.Style.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                    }
                    else
                    {
                        celdaNivel.Style.ForeColor = Color.FromArgb(90, 110, 135);
                    }
                    celdaNivel.Style.SelectionForeColor = celdaNivel.Style.ForeColor;
                }
                _grillaBitacora.ClearSelection();
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "SistemaControl");
            }
        }

        private void CargarCambios()
        {
            try
            {
                string? entidad = _cmbEntidadCambios.SelectedIndex <= 0 ? null : _cmbEntidadCambios.SelectedItem?.ToString();
                int? idRegistro = null;
                if (_txtIdCambios.Text.Trim().Length > 0)
                {
                    if (!int.TryParse(_txtIdCambios.Text.Trim(), out int id))
                    {
                        MessageBox.Show(FindForm(), Localizacion("sistema.cambios.sinId"), Localizacion("sistema.cambios"),
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    idRegistro = id;
                }

                using var contexto = new NegocioDbContext();
                _cambios = new AuditoriaLogic(contexto).ObtenerHistorial(entidad, idRegistro);
                _grillaCambios.Rows.Clear();
                foreach (CambioAuditado cambio in _cambios)
                {
                    int indice = _grillaCambios.Rows.Add(
                        cambio.FechaCambio.ToString("dd/MM/yyyy HH:mm:ss"),
                        cambio.Usuario,
                        cambio.TipoCambio,
                        cambio.Entidad,
                        cambio.IdRegistro);

                    DataGridViewCell celdaTipo = _grillaCambios.Rows[indice].Cells[2];
                    celdaTipo.Style.ForeColor = cambio.TipoCambio switch
                    {
                        "Alta" => Color.FromArgb(30, 130, 75),
                        "Baja" => Color.FromArgb(185, 45, 40),
                        _ => Color.FromArgb(190, 125, 20)
                    };
                    celdaTipo.Style.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                    celdaTipo.Style.SelectionForeColor = celdaTipo.Style.ForeColor;
                }
                _grillaCambios.ClearSelection();
                MostrarDetalleCambio();
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "SistemaControl");
            }
        }

        private void MostrarDetalleCambio()
        {
            if (_grillaCambios.SelectedRows.Count == 0 || _grillaCambios.SelectedRows[0].Index >= _cambios.Count)
            {
                _txtCambioAnterior.Text = string.Empty;
                _txtCambioNuevo.Text = string.Empty;
                return;
            }
            CambioAuditado cambio = _cambios[_grillaCambios.SelectedRows[0].Index];
            _txtCambioAnterior.Text = EmbellecerJson(cambio.DatosAnteriores);
            _txtCambioNuevo.Text = EmbellecerJson(cambio.DatosNuevos);
        }

        /// <summary>Reformatea un JSON de auditoría con sangría para su lectura (sin escapes).</summary>
        private static string EmbellecerJson(string? json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return string.Empty;
            }
            try
            {
                using JsonDocument documento = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(documento.RootElement, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }
            catch (JsonException)
            {
                return json;
            }
        }

        private void RestaurarCambioSeleccionado()
        {
            if (_grillaCambios.SelectedRows.Count == 0 || _grillaCambios.SelectedRows[0].Index >= _cambios.Count)
            {
                MessageBox.Show(FindForm(), Localizacion("sistema.cambios.selCambio"), Localizacion("sistema.cambios"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CambioAuditado cambio = _cambios[_grillaCambios.SelectedRows[0].Index];
            if (MessageBox.Show(FindForm(), Localizacion("sistema.cambios.confirmar"), Localizacion("sistema.cambios"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                string usuario = SesionActual.Instancia.NombreUsuario;
                if (usuario.Length == 0)
                {
                    usuario = "sistema";
                }
                using var contexto = new NegocioDbContext();
                (bool ok, string mensaje) = new AuditoriaLogic(contexto).Restaurar(cambio.Id, usuario);
                MessageBox.Show(FindForm(), mensaje, Localizacion("sistema.cambios"),
                    MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                CargarCambios();
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "SistemaControl");
            }
        }

        // ------------------------------------------------------------------ idiomas (T05)

        private void CargarIdiomas()
        {
            try
            {
                _idiomasEditor = LocalizationService.Idiomas.ToList();
                _grillaIdiomas.Rows.Clear();
                foreach (Idioma idioma in _idiomasEditor)
                {
                    _grillaIdiomas.Rows.Add(idioma.Codigo, idioma.Nombre);
                }
                _grillaIdiomas.ClearSelection();

                int previo = _cmbIdiomaEditor.SelectedIndex;
                _cmbIdiomaEditor.Items.Clear();
                foreach (Idioma idioma in _idiomasEditor)
                {
                    _cmbIdiomaEditor.Items.Add(idioma);
                }
                _cmbIdiomaEditor.SelectedIndex = previo < 0 || previo >= _idiomasEditor.Count ? 0 : previo;
                CargarTextosDelIdioma();
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "SistemaControl");
            }
        }

        private void CargarTextosDelIdioma()
        {
            if (_cmbIdiomaEditor.SelectedIndex < 0 || _cmbIdiomaEditor.SelectedIndex >= _idiomasEditor.Count)
            {
                return;
            }
            try
            {
                string codigo = _idiomasEditor[_cmbIdiomaEditor.SelectedIndex].Codigo;
                _textosEditor = LocalizationService.ObtenerTextosDe(codigo);
                _grillaTextos.Rows.Clear();
                foreach (TextoLocalizacion texto in _textosEditor)
                {
                    _grillaTextos.Rows.Add(texto.Clave, texto.Valor);
                }
                _grillaTextos.ClearSelection();
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "SistemaControl");
            }
        }

        private void GuardarCambiosTextos()
        {
            if (_cmbIdiomaEditor.SelectedIndex < 0 || _cmbIdiomaEditor.SelectedIndex >= _idiomasEditor.Count)
            {
                MessageBox.Show(FindForm(), Localizacion("idiomas.sinSeleccion"), Localizacion("sistema.idiomas"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                string codigo = _idiomasEditor[_cmbIdiomaEditor.SelectedIndex].Codigo;
                int guardados = 0;
                for (int i = 0; i < _grillaTextos.Rows.Count && i < _textosEditor.Count; i++)
                {
                    string clave = _grillaTextos.Rows[i].Cells[0].Value?.ToString() ?? string.Empty;
                    string valor = _grillaTextos.Rows[i].Cells[1].Value?.ToString() ?? string.Empty;
                    if (!string.Equals(valor, _textosEditor[i].Valor, StringComparison.Ordinal))
                    {
                        LocalizationService.GuardarTexto(codigo, clave, valor);
                        guardados++;
                    }
                }
                MessageBox.Show(FindForm(), Localizacion("idiomas.guardadoOk") + $" ({guardados})",
                    Localizacion("sistema.idiomas"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarTextosDelIdioma();
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "SistemaControl");
            }
        }

        private void AgregarLeyendaInline()
        {
            if (_cmbIdiomaEditor.SelectedIndex < 0 || _cmbIdiomaEditor.SelectedIndex >= _idiomasEditor.Count)
            {
                MessageBox.Show(FindForm(), Localizacion("idiomas.sinSeleccion"), Localizacion("sistema.idiomas"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string clave = _txtNuevaClave.Text.Trim();
            if (clave.Length == 0)
            {
                MessageBox.Show(FindForm(), Localizacion("idiomas.claveObligatoria"), Localizacion("sistema.idiomas"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                string codigo = _idiomasEditor[_cmbIdiomaEditor.SelectedIndex].Codigo;
                LocalizationService.GuardarTexto(codigo, clave, _txtNuevoValor.Text);
                _txtNuevaClave.Clear();
                _txtNuevoValor.Clear();
                CargarTextosDelIdioma();
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "SistemaControl");
            }
        }

        private void AbrirAltaIdioma()
        {
            using var formulario = new IdiomaNuevoForm();
            if (formulario.ShowDialog(FindForm()) == DialogResult.OK)
            {
                CargarIdiomas();
            }
        }

        private void EliminarIdiomaSeleccionado()
        {
            if (_grillaIdiomas.SelectedRows.Count == 0)
            {
                MessageBox.Show(FindForm(), Localizacion("idiomas.sinSeleccion"), Localizacion("sistema.idiomas"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string codigo = _grillaIdiomas.SelectedRows[0].Cells[0].Value?.ToString() ?? string.Empty;
            if (MessageBox.Show(FindForm(), Localizacion("idiomas.confirmarEliminar") + $" ({codigo})",
                    Localizacion("sistema.idiomas"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }
            try
            {
                LocalizationService.EliminarIdioma(codigo);
                CargarIdiomas();
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, Localizacion("sistema.idiomas"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ------------------------------------------------------------------ perfiles y permisos (T04)

        private void CargarPerfiles()
        {
            try
            {
                string? previo = _cmbPerfiles.SelectedItem?.ToString();
                _cmbPerfiles.Items.Clear();
                foreach (string perfil in SeguridadService.ObtenerPerfiles())
                {
                    _cmbPerfiles.Items.Add(perfil);
                }
                int indice = previo == null ? 0 : Math.Max(0, _cmbPerfiles.Items.IndexOf(previo));
                _cmbPerfiles.SelectedIndex = _cmbPerfiles.Items.Count > 0 ? indice : -1;
                CargarArbolDelPerfil();
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "SistemaControl");
            }
        }

        private void CargarArbolDelPerfil()
        {
            if (_cmbPerfiles.SelectedIndex < 0)
            {
                return;
            }
            try
            {
                string perfil = _cmbPerfiles.SelectedItem?.ToString() ?? string.Empty;
                _actualizandoArbol = true;
                try
                {
                    _treePermisos.BeginUpdate();
                    _treePermisos.Nodes.Clear();
                    PermisoCompuesto raiz = SeguridadService.ObtenerArbolPermisos();
                    LlenarArbol(_treePermisos.Nodes, raiz);
                    AplicarAsignaciones(_treePermisos.Nodes, SeguridadService.ObtenerPermisosDePerfil(perfil));
                    _treePermisos.ExpandAll();
                }
                finally
                {
                    _treePermisos.EndUpdate();
                    _actualizandoArbol = false;
                }
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "SistemaControl");
            }
        }

        /// <summary>Llena el TreeView con el árbol de permisos (función recursiva, T04).</summary>
        private static void LlenarArbol(TreeNodeCollection destino, PermisoCompuesto raiz)
        {
            foreach (Permiso hijo in raiz.Hijos)
            {
                TreeNode nodo = destino.Add($"{hijo.Codigo} — {hijo.Nombre}");
                nodo.Tag = hijo.Codigo;
                if (hijo is PermisoCompuesto compuesto)
                {
                    LlenarArbol(nodo.Nodes, compuesto);
                }
            }
        }

        /// <summary>Aplica las asignaciones del perfil sobre el árbol (función recursiva, T04).</summary>
        private static void AplicarAsignaciones(TreeNodeCollection nodos, List<string> asignados)
        {
            foreach (TreeNode nodo in nodos)
            {
                nodo.Checked = nodo.Tag is string codigo && asignados.Contains(codigo);
                AplicarAsignaciones(nodo.Nodes, asignados);
            }
        }

        /// <summary>Propaga el estado marcado a todo el subárbol (función recursiva, T04).</summary>
        private static void PropagarASubarbol(TreeNode raiz, bool marcado)
        {
            foreach (TreeNode hijo in raiz.Nodes)
            {
                hijo.Checked = marcado;
                PropagarASubarbol(hijo, marcado);
            }
        }

        /// <summary>Recolecta los códigos marcados del árbol (función recursiva, T04).</summary>
        private static void RecolectarMarcados(TreeNodeCollection nodos, List<string> destino)
        {
            foreach (TreeNode nodo in nodos)
            {
                if (nodo.Checked && nodo.Tag is string codigo)
                {
                    destino.Add(codigo);
                }
                RecolectarMarcados(nodo.Nodes, destino);
            }
        }

        private void TreePermisos_AfterCheck(object? sender, TreeViewEventArgs e)
        {
            if (_actualizandoArbol || e.Node == null)
            {
                return;
            }
            _actualizandoArbol = true;
            try
            {
                PropagarASubarbol(e.Node, e.Node.Checked);
            }
            finally
            {
                _actualizandoArbol = false;
            }
        }

        private void MarcarTodo(bool marcado)
        {
            _actualizandoArbol = true;
            try
            {
                foreach (TreeNode nodo in _treePermisos.Nodes)
                {
                    nodo.Checked = marcado;
                    PropagarASubarbol(nodo, marcado);
                }
            }
            finally
            {
                _actualizandoArbol = false;
            }
        }

        private void GuardarPermisosDelPerfil()
        {
            if (_cmbPerfiles.SelectedIndex < 0)
            {
                MessageBox.Show(FindForm(), Localizacion("perfiles.sinPerfil"), Localizacion("sistema.perfiles"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                string perfil = _cmbPerfiles.SelectedItem?.ToString() ?? string.Empty;
                var codigos = new List<string>();
                RecolectarMarcados(_treePermisos.Nodes, codigos);
                SeguridadService.GuardarPermisosDePerfil(perfil, codigos);
                MessageBox.Show(FindForm(), Localizacion("perfiles.guardado"), Localizacion("sistema.perfiles"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "SistemaControl");
            }
        }

        private void AbrirAltaUsuario()
        {
            using var formulario = new UsuarioNuevoForm();
            if (formulario.ShowDialog(FindForm()) == DialogResult.OK)
            {
                CargarUsuarios();
            }
        }

        private void CambiarEstadoSeleccionado(bool activo)
        {
            if (_grillaUsuarios.SelectedRows.Count == 0)
            {
                MessageBox.Show(FindForm(), Localizacion("sistema.selUsuario"), Localizacion("sistema.usuarios"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int indice = _grillaUsuarios.SelectedRows[0].Index;
            if (indice < 0 || indice >= _usuarios.Count)
            {
                return;
            }
            UsuarioListado seleccionado = _usuarios[indice];

            string pregunta = Localizacion(activo ? "sistema.confirmarHabilitar" : "sistema.confirmarDeshabilitar");
            if (MessageBox.Show(FindForm(), $"{pregunta}{Environment.NewLine}({seleccionado.NombreUsuario})",
                    Localizacion("sistema.usuarios"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                SeguridadService.CambiarEstadoUsuario(seleccionado.NombreUsuario, activo, "Administración del sistema.");
                CargarUsuarios();
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "SistemaControl");
            }
        }
    }
}
