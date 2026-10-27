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
    /// Registro de eventos adversos (REQ-FUNC-003): búsqueda del paciente por documento,
    /// consulta de sus eventos registrados (o de los últimos eventos de todos los pacientes)
    /// y alta de nuevos incidentes con tipo, gravedad, descripción y acción tomada.
    /// </summary>
    public class EventosAdversosControl : UserControl
    {
        private readonly string _nombreUsuario;

        private readonly Label _lblDni = new();
        private readonly TextBox _txtDni = new();
        private readonly Button _btnBuscar = new();
        private readonly Label _lblPaciente = new();
        private readonly Button _btnRegistrar = new();
        private readonly DataGridView _grilla = new();
        private readonly Label _lblSinEventos = new();

        private Paciente? _pacienteActual;

        public EventosAdversosControl(string nombreUsuario)
        {
            _nombreUsuario = nombreUsuario;
            ConstruirInterfaz();
            AplicarTextos();
            Recargar();
        }

        private static string Localizacion(string clave) => LocalizationService.ObtenerTexto(clave);

        /// <summary>Recarga la vista actual (recientes o del paciente seleccionado).</summary>
        public void Recargar()
        {
            if (_pacienteActual == null)
            {
                CargarRecientes();
            }
            else
            {
                CargarDePaciente(_pacienteActual.Id);
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
            var panelBarra = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = BackColor };

            _lblDni.Location = new Point(0, 4);
            _lblDni.Size = new Size(320, 18);
            _lblDni.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _lblDni.ForeColor = Color.FromArgb(60, 85, 115);

            _txtDni.Name = "txtDniEvento";
            _txtDni.Location = new Point(0, 24);
            _txtDni.Size = new Size(180, 28);
            _txtDni.Font = new Font("Segoe UI", 11F);
            _txtDni.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; BuscarPaciente(); } };

            _btnBuscar.Location = new Point(190, 23);
            _btnBuscar.Size = new Size(110, 30);
            _btnBuscar.FlatStyle = FlatStyle.Flat;
            _btnBuscar.BackColor = Color.FromArgb(45, 108, 223);
            _btnBuscar.ForeColor = Color.White;
            _btnBuscar.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _btnBuscar.FlatAppearance.BorderSize = 0;
            _btnBuscar.Cursor = Cursors.Hand;
            _btnBuscar.Click += (s, e) => BuscarPaciente();

            _lblPaciente.Location = new Point(314, 28);
            _lblPaciente.Size = new Size(205, 24);
            _lblPaciente.AutoEllipsis = true;
            _lblPaciente.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            _lblPaciente.ForeColor = Color.FromArgb(30, 66, 120);

            _btnRegistrar.Location = new Point(530, 15);
            _btnRegistrar.Size = new Size(230, 34);
            _btnRegistrar.FlatStyle = FlatStyle.Flat;
            _btnRegistrar.BackColor = Color.FromArgb(45, 108, 223);
            _btnRegistrar.ForeColor = Color.White;
            _btnRegistrar.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _btnRegistrar.FlatAppearance.BorderSize = 0;
            _btnRegistrar.Cursor = Cursors.Hand;
            _btnRegistrar.Click += (s, e) => RegistrarEvento();

            panelBarra.Controls.Add(_lblDni);
            panelBarra.Controls.Add(_txtDni);
            panelBarra.Controls.Add(_btnBuscar);
            panelBarra.Controls.Add(_lblPaciente);
            panelBarra.Controls.Add(_btnRegistrar);
            _btnRegistrar.BringToFront();

            // ---- Grilla de eventos ----
            _grilla.Dock = DockStyle.Fill;
            EstiloGrilla(_grilla);
            _grilla.Columns.Add("fecha", "");
            _grilla.Columns.Add("paciente", "");
            _grilla.Columns.Add("tipo", "");
            _grilla.Columns.Add("gravedad", "");
            _grilla.Columns.Add("descripcion", "");
            _grilla.Columns.Add("accion", "");
            _grilla.Columns["fecha"].FillWeight = 12;
            _grilla.Columns["paciente"].FillWeight = 20;
            _grilla.Columns["tipo"].FillWeight = 13;
            _grilla.Columns["gravedad"].FillWeight = 11;
            _grilla.Columns["descripcion"].FillWeight = 22;
            _grilla.Columns["accion"].FillWeight = 22;
            foreach (string columna in new[] { "fecha", "tipo", "gravedad" })
            {
                _grilla.Columns[columna].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            // Las celdas de texto largo crecen en alto para mostrar el contenido completo.
            _grilla.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            _grilla.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            _lblSinEventos.Dock = DockStyle.Bottom;
            _lblSinEventos.Height = 24;
            _lblSinEventos.Font = new Font("Segoe UI", 9.5F);
            _lblSinEventos.ForeColor = Color.FromArgb(150, 100, 40);

            Controls.Add(_grilla);
            Controls.Add(_lblSinEventos);
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
            grilla.RowTemplate.Height = 32;
            grilla.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 251, 255);
        }

        private void AplicarTextos()
        {
            _lblDni.Text = Localizacion("rin.identificacion");
            _btnBuscar.Text = Localizacion("comun.buscar");
            _btnRegistrar.Text = Localizacion("eventos.registrar");
            _grilla.Columns["fecha"].HeaderText = Localizacion("rin.columna.fecha");
            _grilla.Columns["paciente"].HeaderText = Localizacion("panel.columna.paciente");
            _grilla.Columns["tipo"].HeaderText = Localizacion("eventos.columna.tipo");
            _grilla.Columns["gravedad"].HeaderText = Localizacion("eventos.columna.gravedad");
            _grilla.Columns["descripcion"].HeaderText = Localizacion("reportes.columna.descripcion");
            _grilla.Columns["accion"].HeaderText = Localizacion("eventos.columna.accion");
            if (_pacienteActual == null)
            {
                _lblPaciente.Text = Localizacion("eventos.recientes");
            }
        }

        private string TipoTexto(TipoEventoAdverso tipo) => tipo switch
        {
            TipoEventoAdverso.Hemorragia => Localizacion("eventos.tipo.hemorragia"),
            TipoEventoAdverso.Trombosis => Localizacion("eventos.tipo.trombosis"),
            TipoEventoAdverso.Reaccion => Localizacion("eventos.tipo.reaccion"),
            _ => Localizacion("eventos.tipo.otro")
        };

        private string GravedadTexto(GravedadEvento gravedad) => gravedad switch
        {
            GravedadEvento.Leve => Localizacion("eventos.gravedad.leve"),
            GravedadEvento.Moderada => Localizacion("eventos.gravedad.moderada"),
            _ => Localizacion("eventos.gravedad.grave")
        };

        private static Color ColorGravedad(GravedadEvento gravedad) => gravedad switch
        {
            GravedadEvento.Leve => Color.FromArgb(30, 130, 75),
            GravedadEvento.Moderada => Color.FromArgb(190, 125, 20),
            _ => Color.FromArgb(185, 45, 40)
        };

        private void AgregarFila(EventoAdverso evento, string nombrePaciente)
        {
            int indice = _grilla.Rows.Add(
                evento.Fecha.ToString("dd/MM/yyyy"),
                nombrePaciente,
                TipoTexto(evento.Tipo),
                GravedadTexto(evento.Gravedad),
                evento.Descripcion,
                evento.AccionTomada);

            DataGridViewCell celda = _grilla.Rows[indice].Cells[3];
            Color color = ColorGravedad(evento.Gravedad);
            celda.Style.BackColor = color;
            celda.Style.ForeColor = Color.White;
            celda.Style.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            celda.Style.SelectionBackColor = color;
            celda.Style.SelectionForeColor = Color.White;
        }

        private void CargarRecientes()
        {
            try
            {
                _pacienteActual = null;
                if (_lblPaciente.Text.Length == 0 || _lblPaciente.Text == Localizacion("rin.pacienteNoEncontrado"))
                {
                    _lblPaciente.Text = Localizacion("eventos.recientes");
                }

                using var contexto = new NegocioDbContext();
                Dictionary<int, string> nombres = contexto.Pacientes.AsNoTracking()
                    .ToDictionary(p => p.Id, p => p.NombreCompleto);

                var eventos = contexto.EventosAdversos.AsNoTracking()
                    .OrderByDescending(e => e.Fecha).ThenByDescending(e => e.Id)
                    .Take(50)
                    .ToList();

                _grilla.Rows.Clear();
                foreach (EventoAdverso evento in eventos)
                {
                    AgregarFila(evento, nombres.GetValueOrDefault(evento.IdPaciente, "—"));
                }
                _grilla.ClearSelection();
                _lblSinEventos.Text = eventos.Count == 0 ? Localizacion("eventos.sinEventos") : string.Empty;
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "EventosAdversosControl", mostrarMensaje: false);
            }
        }

        private void CargarDePaciente(int idPaciente)
        {
            try
            {
                using var contexto = new NegocioDbContext();
                List<EventoAdverso> eventos = new EventoAdversoLogic(contexto).ObtenerPorPaciente(idPaciente);

                string nombre = _pacienteActual?.NombreCompleto ?? string.Empty;
                _grilla.Rows.Clear();
                foreach (EventoAdverso evento in eventos)
                {
                    AgregarFila(evento, nombre);
                }
                _grilla.ClearSelection();
                _lblSinEventos.Text = eventos.Count == 0 ? Localizacion("eventos.sinEventos") : string.Empty;
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "EventosAdversosControl", mostrarMensaje: false);
            }
        }

        private void BuscarPaciente()
        {
            try
            {
                using var contexto = new NegocioDbContext();
                Paciente? paciente = new PacienteLogic(contexto).BuscarPorDNI(_txtDni.Text.Trim());
                if (paciente == null)
                {
                    _lblPaciente.Text = Localizacion("rin.pacienteNoEncontrado");
                    CargarRecientes();
                    return;
                }

                _pacienteActual = paciente;
                _lblPaciente.Text = $"{paciente.NombreCompleto} ({paciente.DNI})";
                CargarDePaciente(paciente.Id);
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "EventosAdversosControl");
            }
        }

        private void RegistrarEvento()
        {
            if (_pacienteActual == null)
            {
                MessageBox.Show(FindForm(), Localizacion("eventos.selPaciente"), Localizacion("eventos.registrar"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var formulario = new EventoAdversoForm(_pacienteActual, _nombreUsuario);
            if (formulario.ShowDialog(FindForm()) == DialogResult.OK)
            {
                CargarDePaciente(_pacienteActual.Id);
            }
        }
    }
}
