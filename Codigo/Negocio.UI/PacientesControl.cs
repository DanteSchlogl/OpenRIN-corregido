using Microsoft.EntityFrameworkCore;
using Negocio.BLL;
using Negocio.DAL.Context;
using Negocio.DomainModel;
using Negocio.DomainModel.Enums;
using Services.Facade;

namespace Negocio.UI
{
    /// <summary>
    /// Pantalla de gestión de pacientes e historias clínicas (REQ-FUNC-001/002): búsqueda por
    /// nombre/documento, alta con credenciales del portal, modificación, baja lógica con motivo
    /// y acceso a la historia clínica. Los botones de gestión se habilitan según los permisos
    /// del perfil (REQ-ARQ-006).
    /// </summary>
    public class PacientesControl : UserControl
    {
        private readonly string _nombreUsuario;
        private readonly bool _puedeGestionar;
        private readonly bool _puedeEditarHistoria;

        private readonly TextBox _txtBusqueda = new();
        private readonly Button _btnBuscar = new();
        private readonly CheckBox _chkInactivos = new();
        private readonly Label _lblBuscar = new();
        private readonly Button _btnNuevo = new();
        private readonly Button _btnModificar = new();
        private readonly Button _btnBaja = new();
        private readonly Button _btnHistoria = new();
        private readonly DataGridView _grilla = new();
        private readonly Label _lblResultados = new();
        private List<Paciente> _pacientes = new();

        public PacientesControl(int idUsuario, string nombreUsuario)
        {
            _nombreUsuario = nombreUsuario;
            _puedeGestionar = SeguridadService.TienePermiso(idUsuario, "GESTION_PACIENTES");
            _puedeEditarHistoria = SeguridadService.TienePermiso(idUsuario, "HISTORIA_CLINICA");

            ConstruirInterfaz();
            AplicarTextos();
            CargarDatos();
        }

        private static string Texto(string clave) => LocalizationService.ObtenerTexto(clave);

        /// <summary>Recarga la lista desde la base (al entrar al módulo).</summary>
        public void Recargar() => CargarDatos();

        /// <summary>Reaplica textos por cambio de idioma y recarga.</summary>
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

            // ---- Barra superior ----
            var panelBarra = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = BackColor };

            _lblBuscar.Location = new Point(0, 18);
            _lblBuscar.Size = new Size(60, 20);
            _lblBuscar.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _lblBuscar.ForeColor = Color.FromArgb(60, 85, 115);

            _txtBusqueda.Location = new Point(62, 14);
            _txtBusqueda.Size = new Size(220, 28);
            _txtBusqueda.Font = new Font("Segoe UI", 10F);
            _txtBusqueda.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    CargarDatos();
                }
            };

            _btnBuscar.Location = new Point(290, 13);
            _btnBuscar.Size = new Size(90, 30);
            _btnBuscar.FlatStyle = FlatStyle.Flat;
            _btnBuscar.BackColor = Color.FromArgb(45, 108, 223);
            _btnBuscar.ForeColor = Color.White;
            _btnBuscar.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _btnBuscar.FlatAppearance.BorderSize = 0;
            _btnBuscar.Cursor = Cursors.Hand;
            _btnBuscar.Click += (s, e) => CargarDatos();

            _chkInactivos.Location = new Point(392, 17);
            _chkInactivos.Size = new Size(150, 22);
            _chkInactivos.Font = new Font("Segoe UI", 9.5F);
            _chkInactivos.CheckedChanged += (s, e) => CargarDatos();

            var flowAcciones = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 600,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = BackColor,
                Padding = new Padding(0, 9, 0, 0)
            };

            EstiloBoton(_btnHistoria, Color.White, Color.FromArgb(28, 60, 105), borde: true);
            EstiloBoton(_btnBaja, Color.White, Color.FromArgb(28, 60, 105), borde: true);
            EstiloBoton(_btnModificar, Color.White, Color.FromArgb(28, 60, 105), borde: true);
            EstiloBoton(_btnNuevo, Color.FromArgb(45, 108, 223), Color.White, borde: false);

            _btnNuevo.Click += (s, e) => AbrirEditor(null, esModificacion: false);
            _btnModificar.Click += (s, e) => AbrirEditor(PacienteSeleccionado(), esModificacion: true);
            _btnBaja.Click += (s, e) => DarDeBajaSeleccionado();
            _btnHistoria.Click += (s, e) => AbrirHistoriaClinica(PacienteSeleccionado());

            flowAcciones.Controls.Add(_btnHistoria);
            flowAcciones.Controls.Add(_btnBaja);
            flowAcciones.Controls.Add(_btnModificar);
            flowAcciones.Controls.Add(_btnNuevo);

            panelBarra.Controls.Add(_lblBuscar);
            panelBarra.Controls.Add(_txtBusqueda);
            panelBarra.Controls.Add(_btnBuscar);
            panelBarra.Controls.Add(_chkInactivos);
            panelBarra.Controls.Add(flowAcciones);

            // ---- Grilla ----
            _grilla.Dock = DockStyle.Fill;
            _grilla.ReadOnly = true;
            _grilla.AllowUserToAddRows = false;
            _grilla.AllowUserToDeleteRows = false;
            _grilla.AllowUserToResizeRows = false;
            _grilla.RowHeadersVisible = false;
            _grilla.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grilla.MultiSelect = false;
            _grilla.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
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
            _grilla.DefaultCellStyle.Font = new Font("Segoe UI", 10.5F);
            _grilla.DefaultCellStyle.SelectionBackColor = Color.FromArgb(210, 230, 250);
            _grilla.DefaultCellStyle.SelectionForeColor = Color.Black;
            _grilla.DefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
            _grilla.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 251, 255);
            _grilla.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    AbrirEditor(PacienteSeleccionado(), esModificacion: true);
                }
            };
            _grilla.SelectionChanged += (s, e) => ActualizarBotones();

            _grilla.Columns.Add("nombre", "");
            _grilla.Columns.Add("documento", "");
            _grilla.Columns.Add("telefono", "");
            _grilla.Columns.Add("email", "");
            _grilla.Columns.Add("obra", "");
            _grilla.Columns.Add("estado", "");
            _grilla.Columns.Add("fechaAlta", "");
            _grilla.Columns["nombre"].FillWeight = 26;
            _grilla.Columns["documento"].FillWeight = 13;
            _grilla.Columns["telefono"].FillWeight = 14;
            _grilla.Columns["email"].FillWeight = 20;
            _grilla.Columns["obra"].FillWeight = 15;
            _grilla.Columns["estado"].FillWeight = 9;
            _grilla.Columns["fechaAlta"].FillWeight = 12;
            _grilla.Columns["estado"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // ---- Pie: resultados ----
            _lblResultados.Dock = DockStyle.Bottom;
            _lblResultados.Height = 26;
            _lblResultados.Font = new Font("Segoe UI", 9.5F);
            _lblResultados.ForeColor = Color.FromArgb(90, 110, 135);
            _lblResultados.TextAlign = ContentAlignment.MiddleLeft;

            Controls.Add(_grilla);
            Controls.Add(panelBarra);
            Controls.Add(_lblResultados);
        }

        private static void EstiloBoton(Button boton, Color fondo, Color texto, bool borde)
        {
            boton.Size = new Size(136, 34);
            boton.FlatStyle = FlatStyle.Flat;
            boton.BackColor = fondo;
            boton.ForeColor = texto;
            boton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            boton.Margin = new Padding(8, 0, 0, 0);
            boton.Cursor = Cursors.Hand;
            if (borde)
            {
                boton.FlatAppearance.BorderColor = Color.FromArgb(202, 220, 240);
            }
            else
            {
                boton.FlatAppearance.BorderSize = 0;
            }
        }

        private void AplicarTextos()
        {
            _lblBuscar.Text = Texto("pacientes.buscar");
            _txtBusqueda.PlaceholderText = Texto("pacientes.buscarPlaceholder");
            _btnBuscar.Text = Texto("comun.buscar");
            _chkInactivos.Text = Texto("pacientes.verInactivos");
            _btnNuevo.Text = Texto("pacientes.alta");
            _btnModificar.Text = Texto("comun.modificar");
            _btnBaja.Text = Texto("pacientes.baja");
            _btnHistoria.Text = Texto("pacientes.historia");

            _grilla.Columns["nombre"].HeaderText = Texto("pacientes.nombre");
            _grilla.Columns["documento"].HeaderText = Texto("pacientes.documento");
            _grilla.Columns["telefono"].HeaderText = Texto("pacientes.telefono");
            _grilla.Columns["email"].HeaderText = Texto("pacientes.email");
            _grilla.Columns["obra"].HeaderText = Texto("pacientes.obraSocial");
            _grilla.Columns["estado"].HeaderText = Texto("pacientes.estado");
            _grilla.Columns["fechaAlta"].HeaderText = Texto("pacientes.fechaAlta");
        }

        private void CargarDatos()
        {
            try
            {
                using var contexto = new NegocioDbContext();
                _pacientes = new PacienteLogic(contexto).Buscar(_txtBusqueda.Text, _chkInactivos.Checked);
                Dictionary<int, string> obras = contexto.ObrasSociales.AsNoTracking()
                    .ToDictionary(o => o.Id, o => o.Nombre);

                _grilla.Rows.Clear();
                foreach (Paciente paciente in _pacientes)
                {
                    string obra = paciente.IdObraSocial.HasValue && obras.TryGetValue(paciente.IdObraSocial.Value, out string? nombreObra)
                        ? nombreObra
                        : Texto("pacientes.sinObraSocial");
                    string estado = paciente.Estado == EstadoPaciente.Activo
                        ? Texto("pacientes.estadoActivo")
                        : Texto("pacientes.estadoInactivo");

                    int indice = _grilla.Rows.Add(
                        paciente.NombreCompleto,
                        paciente.DNI,
                        paciente.Telefono,
                        paciente.Email,
                        obra,
                        estado,
                        paciente.FechaAlta.ToString("dd/MM/yyyy"));

                    if (paciente.Estado != EstadoPaciente.Activo)
                    {
                        _grilla.Rows[indice].DefaultCellStyle.ForeColor = Color.FromArgb(138, 148, 158);
                    }
                }

                _lblResultados.Text = string.Format(Texto("pacientes.resultados"), _pacientes.Count);
                ActualizarBotones();
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "PacientesControl", mostrarMensaje: false);
                _lblResultados.Text = Texto("error.generico");
            }
        }

        private void ActualizarBotones()
        {
            Paciente? seleccionado = PacienteSeleccionado();
            _btnNuevo.Enabled = _puedeGestionar;
            _btnModificar.Enabled = _puedeGestionar && seleccionado != null && seleccionado.Estado == EstadoPaciente.Activo;
            _btnBaja.Enabled = _puedeGestionar && seleccionado != null && seleccionado.Estado == EstadoPaciente.Activo;
            _btnHistoria.Enabled = seleccionado != null;
        }

        private Paciente? PacienteSeleccionado()
        {
            if (_grilla.SelectedRows.Count == 0)
            {
                return null;
            }
            int indice = _grilla.SelectedRows[0].Index;
            return indice >= 0 && indice < _pacientes.Count ? _pacientes[indice] : null;
        }

        private void AbrirEditor(Paciente? paciente, bool esModificacion)
        {
            if (!_puedeGestionar)
            {
                return;
            }
            if (esModificacion && paciente == null)
            {
                MostrarAviso(Texto("pacientes.sinSeleccion"));
                return;
            }

            using var editor = new PacienteEditorForm(paciente, _nombreUsuario);
            if (editor.ShowDialog(FindForm()) == DialogResult.OK)
            {
                CargarDatos();
            }
        }

        private void DarDeBajaSeleccionado()
        {
            Paciente? paciente = PacienteSeleccionado();
            if (!_puedeGestionar)
            {
                return;
            }
            if (paciente == null)
            {
                MostrarAviso(Texto("pacientes.sinSeleccion"));
                return;
            }

            DialogResult confirmacion = MessageBox.Show(FindForm(),
                string.Format(Texto("pacientes.baja.confirmar"), paciente.NombreCompleto),
                Texto("pacientes.baja"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirmacion != DialogResult.Yes)
            {
                return;
            }

            using var dialogo = new TextoDialogoForm(Texto("pacientes.baja"), Texto("pacientes.baja.motivo"));
            if (dialogo.ShowDialog(FindForm()) != DialogResult.OK || dialogo.Valor.Length == 0)
            {
                return;
            }

            try
            {
                using var contexto = new NegocioDbContext();
                new PacienteLogic(contexto).DarDeBajaPaciente(paciente.Id, dialogo.Valor, _nombreUsuario);
                CargarDatos();
            }
            catch (Negocio.DomainModel.Exceptions.ValidacionNegocioException ex)
            {
                MostrarAviso(string.Join(Environment.NewLine, ex.Errores));
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "PacientesControl");
            }
        }

        private void AbrirHistoriaClinica(Paciente? paciente)
        {
            if (paciente == null)
            {
                MostrarAviso(Texto("pacientes.sinSeleccion"));
                return;
            }

            using var historia = new HistoriaClinicaForm(paciente, _nombreUsuario, _puedeEditarHistoria);
            historia.ShowDialog(FindForm());
        }

        private void MostrarAviso(string mensaje)
        {
            MessageBox.Show(FindForm(), mensaje, Texto("error.titulo"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
