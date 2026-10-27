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
    /// Agenda y turnos (REQ-FUNC-009): vista de la agenda médica por período y panel de
    /// sugerencias de reasignación inteligente (estables con turno → críticos sin turno),
    /// con aceptación (libera + asigna + notifica) o rechazo (escalable al médico).
    /// </summary>
    public class AgendaControl : UserControl
    {
        private readonly string _nombreUsuario;
        private readonly bool _puedeGestionar;

        private readonly Label _lblDesde = new();
        private readonly DateTimePicker _dtpDesde = new();
        private readonly Label _lblHasta = new();
        private readonly DateTimePicker _dtpHasta = new();
        private readonly Button _btnActualizar = new();
        private readonly Button _btnAgendar = new();
        private readonly DataGridView _grillaTurnos = new();
        private readonly Label _lblSinTurnos = new();
        private readonly Panel _panelSugerencias = new();
        private readonly Label _lblSugerenciasTitulo = new();
        private readonly DataGridView _grillaSugerencias = new();
        private readonly Button _btnAceptarSugerencia = new();
        private readonly Button _btnRechazarSugerencia = new();
        private readonly Label _lblInfoSugerencias = new();
        private List<SugerenciaReasignacion> _sugerencias = new();

        public AgendaControl(int idUsuario, string nombreUsuario)
        {
            _nombreUsuario = nombreUsuario;
            _puedeGestionar = SeguridadService.TienePermiso(idUsuario, "GESTION_AGENDA");

            ConstruirInterfaz();
            AplicarTextos();
            CargarTodo();
        }

        private static string Localizacion(string clave) => LocalizationService.ObtenerTexto(clave);

        /// <summary>Recarga agenda y sugerencias (al entrar al módulo).</summary>
        public void Recargar() => CargarTodo();

        /// <summary>Reaplica textos por cambio de idioma y recarga.</summary>
        public void RefrescarTextos()
        {
            AplicarTextos();
            CargarTodo();
        }

        private void ConstruirInterfaz()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(244, 249, 254);
            Padding = new Padding(8, 4, 8, 6);

            var colorEtiqueta = Color.FromArgb(60, 85, 115);

            // ---- Barra superior ----
            var panelBarra = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = BackColor };

            _lblDesde.Location = new Point(0, 20);
            _lblDesde.Size = new Size(50, 20);
            _lblDesde.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _lblDesde.ForeColor = colorEtiqueta;

            _dtpDesde.Location = new Point(54, 16);
            _dtpDesde.Size = new Size(120, 28);
            _dtpDesde.Format = DateTimePickerFormat.Short;
            _dtpDesde.Font = new Font("Segoe UI", 10F);
            _dtpDesde.Value = DateTime.Today;

            _lblHasta.Location = new Point(188, 20);
            _lblHasta.Size = new Size(50, 20);
            _lblHasta.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _lblHasta.ForeColor = colorEtiqueta;

            _dtpHasta.Location = new Point(238, 16);
            _dtpHasta.Size = new Size(120, 28);
            _dtpHasta.Format = DateTimePickerFormat.Short;
            _dtpHasta.Font = new Font("Segoe UI", 10F);
            _dtpHasta.Value = DateTime.Today.AddDays(30);

            _btnActualizar.Location = new Point(374, 15);
            _btnActualizar.Size = new Size(160, 30);
            _btnActualizar.FlatStyle = FlatStyle.Flat;
            _btnActualizar.BackColor = Color.White;
            _btnActualizar.ForeColor = Color.FromArgb(28, 60, 105);
            _btnActualizar.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _btnActualizar.FlatAppearance.BorderColor = Color.FromArgb(202, 220, 240);
            _btnActualizar.Cursor = Cursors.Hand;
            _btnActualizar.Click += (s, e) => CargarTodo();

            _btnAgendar.Location = new Point(546, 15);
            _btnAgendar.Size = new Size(170, 32);
            _btnAgendar.FlatStyle = FlatStyle.Flat;
            _btnAgendar.BackColor = Color.FromArgb(45, 108, 223);
            _btnAgendar.ForeColor = Color.White;
            _btnAgendar.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _btnAgendar.FlatAppearance.BorderSize = 0;
            _btnAgendar.Cursor = Cursors.Hand;
            _btnAgendar.Click += (s, e) => AbrirAgendador();

            panelBarra.Controls.Add(_lblDesde);
            panelBarra.Controls.Add(_dtpDesde);
            panelBarra.Controls.Add(_lblHasta);
            panelBarra.Controls.Add(_dtpHasta);
            panelBarra.Controls.Add(_btnActualizar);
            panelBarra.Controls.Add(_btnAgendar);

            // ---- Grilla de turnos ----
            _grillaTurnos.Dock = DockStyle.Fill;
            EstiloGrilla(_grillaTurnos);
            _grillaTurnos.Columns.Add("fechaHora", "");
            _grillaTurnos.Columns.Add("paciente", "");
            _grillaTurnos.Columns.Add("medico", "");
            _grillaTurnos.Columns.Add("estado", "");
            _grillaTurnos.Columns.Add("observaciones", "");
            _grillaTurnos.Columns["fechaHora"].FillWeight = 18;
            _grillaTurnos.Columns["paciente"].FillWeight = 24;
            _grillaTurnos.Columns["medico"].FillWeight = 22;
            _grillaTurnos.Columns["estado"].FillWeight = 14;
            _grillaTurnos.Columns["observaciones"].FillWeight = 22;
            _grillaTurnos.Columns["estado"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            _lblSinTurnos.Dock = DockStyle.Bottom;
            _lblSinTurnos.Height = 24;
            _lblSinTurnos.Font = new Font("Segoe UI", 9.5F);
            _lblSinTurnos.ForeColor = Color.FromArgb(150, 100, 40);

            // ---- Panel de sugerencias ----
            _panelSugerencias.Dock = DockStyle.Right;
            _panelSugerencias.Width = 480;
            _panelSugerencias.BackColor = Color.White;
            _panelSugerencias.Padding = new Padding(12, 10, 12, 10);

            _lblSugerenciasTitulo.Dock = DockStyle.Top;
            _lblSugerenciasTitulo.Height = 32;
            _lblSugerenciasTitulo.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _lblSugerenciasTitulo.ForeColor = Color.FromArgb(41, 98, 176);

            var panelBotones = new Panel { Dock = DockStyle.Bottom, Height = 96, BackColor = Color.White };
            _btnAceptarSugerencia.Location = new Point(0, 6);
            _btnAceptarSugerencia.Size = new Size(220, 36);
            _btnAceptarSugerencia.FlatStyle = FlatStyle.Flat;
            _btnAceptarSugerencia.BackColor = Color.FromArgb(45, 108, 223);
            _btnAceptarSugerencia.ForeColor = Color.White;
            _btnAceptarSugerencia.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _btnAceptarSugerencia.FlatAppearance.BorderSize = 0;
            _btnAceptarSugerencia.Cursor = Cursors.Hand;
            _btnAceptarSugerencia.Click += (s, e) => AceptarSugerenciaSeleccionada();

            _btnRechazarSugerencia.Location = new Point(232, 6);
            _btnRechazarSugerencia.Size = new Size(220, 36);
            _btnRechazarSugerencia.FlatStyle = FlatStyle.Flat;
            _btnRechazarSugerencia.BackColor = Color.White;
            _btnRechazarSugerencia.ForeColor = Color.FromArgb(28, 60, 105);
            _btnRechazarSugerencia.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _btnRechazarSugerencia.FlatAppearance.BorderColor = Color.FromArgb(202, 220, 240);
            _btnRechazarSugerencia.Cursor = Cursors.Hand;
            _btnRechazarSugerencia.Click += (s, e) => RechazarSugerenciaSeleccionada();

            _lblInfoSugerencias.Location = new Point(0, 52);
            _lblInfoSugerencias.Size = new Size(452, 36);
            _lblInfoSugerencias.Font = new Font("Segoe UI", 9F);
            _lblInfoSugerencias.ForeColor = Color.FromArgb(150, 100, 40);

            panelBotones.Controls.Add(_btnAceptarSugerencia);
            panelBotones.Controls.Add(_btnRechazarSugerencia);
            panelBotones.Controls.Add(_lblInfoSugerencias);

            _grillaSugerencias.Dock = DockStyle.Fill;
            EstiloGrilla(_grillaSugerencias);
            _grillaSugerencias.Columns.Add("turno", "");
            _grillaSugerencias.Columns.Add("de", "");
            _grillaSugerencias.Columns.Add("a", "");
            _grillaSugerencias.Columns["turno"].FillWeight = 26;
            _grillaSugerencias.Columns["de"].FillWeight = 37;
            _grillaSugerencias.Columns["a"].FillWeight = 37;

            _panelSugerencias.Controls.Add(_grillaSugerencias);
            _panelSugerencias.Controls.Add(panelBotones);
            _panelSugerencias.Controls.Add(_lblSugerenciasTitulo);

            Controls.Add(_grillaTurnos);
            Controls.Add(_lblSinTurnos);
            Controls.Add(_panelSugerencias);
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
            _lblDesde.Text = Localizacion("agenda.desde");
            _lblHasta.Text = Localizacion("agenda.hasta");
            _btnActualizar.Text = Localizacion("agenda.actualizar");
            _btnAgendar.Text = Localizacion("agenda.agendar");
            _lblSugerenciasTitulo.Text = Localizacion("agenda.sugerencias");
            _btnAceptarSugerencia.Text = Localizacion("comun.aceptar");
            _btnRechazarSugerencia.Text = Localizacion("agenda.rechazar");

            _grillaTurnos.Columns["fechaHora"].HeaderText = Localizacion("agenda.columna.fechaHora");
            _grillaTurnos.Columns["paciente"].HeaderText = Localizacion("panel.columna.paciente");
            _grillaTurnos.Columns["medico"].HeaderText = Localizacion("agenda.columna.medico");
            _grillaTurnos.Columns["estado"].HeaderText = Localizacion("pacientes.estado");
            _grillaTurnos.Columns["observaciones"].HeaderText = Localizacion("agenda.columna.observaciones");

            _grillaSugerencias.Columns["turno"].HeaderText = Localizacion("agenda.columna.fechaHora");
            _grillaSugerencias.Columns["de"].HeaderText = Localizacion("agenda.sug.columna.de");
            _grillaSugerencias.Columns["a"].HeaderText = Localizacion("agenda.sug.columna.a");
        }

        private string EstadoTexto(EstadoTurno estado) => estado switch
        {
            EstadoTurno.Pendiente => Localizacion("agenda.estado.pendiente"),
            EstadoTurno.Confirmado => Localizacion("agenda.estado.confirmado"),
            EstadoTurno.Cancelado => Localizacion("agenda.estado.cancelado"),
            EstadoTurno.Atendido => Localizacion("agenda.estado.atendido"),
            _ => Localizacion("agenda.estado.reasignacion")
        };

        private void CargarTodo()
        {
            try
            {
                using var contexto = new NegocioDbContext();
                var turnoLogic = new TurnoLogic(contexto);

                Dictionary<int, string> medicos = contexto.Usuarios.AsNoTracking()
                    .ToDictionary(u => u.Id, u => u.NombreCompleto);

                DateTime desde = _dtpDesde.Value.Date;
                DateTime hasta = _dtpHasta.Value.Date.AddDays(1).AddSeconds(-1);
                var turnos = turnoLogic.ObtenerAgenda(desde, hasta);

                _grillaTurnos.Rows.Clear();
                foreach (Turno turno in turnos)
                {
                    string medico = medicos.TryGetValue(turno.IdUsuarioMedico, out string? nombreMedico) ? nombreMedico : "—";
                    int indice = _grillaTurnos.Rows.Add(
                        turno.FechaHora.ToString("dd/MM/yyyy HH:mm"),
                        turno.Paciente?.NombreCompleto ?? "—",
                        medico,
                        EstadoTexto(turno.Estado),
                        turno.Observaciones ?? string.Empty);

                    if (turno.Estado == EstadoTurno.Cancelado)
                    {
                        _grillaTurnos.Rows[indice].DefaultCellStyle.ForeColor = Color.FromArgb(138, 148, 158);
                    }
                }
                _lblSinTurnos.Text = turnos.Count == 0 ? Localizacion("agenda.sinTurnos") : string.Empty;

                _sugerencias = turnoLogic.GenerarSugerencias();
                _grillaSugerencias.Rows.Clear();
                foreach (SugerenciaReasignacion sugerencia in _sugerencias)
                {
                    _grillaSugerencias.Rows.Add(
                        sugerencia.FechaHoraTurno.ToString("dd/MM/yyyy HH:mm"),
                        sugerencia.NombrePacienteEstable,
                        sugerencia.NombrePacienteCritico);
                }
                if (_grillaSugerencias.Rows.Count > 0)
                {
                    _grillaSugerencias.Rows[0].Selected = true; // pre-selección de la primera sugerencia
                }
                _lblInfoSugerencias.Text = _sugerencias.Count == 0 ? Localizacion("agenda.sugerenciasVacias") : string.Empty;
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "AgendaControl", mostrarMensaje: false);
            }
        }

        private void AbrirAgendador()
        {
            if (!_puedeGestionar)
            {
                return;
            }
            using var formulario = new AgendarTurnoForm(_nombreUsuario);
            if (formulario.ShowDialog(FindForm()) == DialogResult.OK)
            {
                CargarTodo();
            }
        }

        private void AceptarSugerenciaSeleccionada()
        {
            if (!_puedeGestionar)
            {
                return;
            }
            if (_grillaSugerencias.SelectedRows.Count == 0)
            {
                MostrarAviso(Localizacion("agenda.selSugerencia"));
                return;
            }
            int indice = _grillaSugerencias.SelectedRows[0].Index;
            if (indice < 0 || indice >= _sugerencias.Count)
            {
                return;
            }

            try
            {
                using var contexto = new NegocioDbContext();
                ResultadoReasignacion resultado = new TurnoLogic(contexto).AceptarSugerencia(_sugerencias[indice], _nombreUsuario);
                MessageBox.Show(FindForm(),
                    Localizacion("agenda.reasignacionOk") + Environment.NewLine + Environment.NewLine +
                    string.Join(Environment.NewLine, resultado.Notificaciones),
                    Localizacion("agenda.sugerencias"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarTodo();
            }
            catch (ValidacionNegocioException ex)
            {
                MostrarAviso(string.Join(Environment.NewLine, ex.Errores));
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "AgendaControl");
            }
        }

        private void RechazarSugerenciaSeleccionada()
        {
            if (!_puedeGestionar)
            {
                return;
            }
            if (_grillaSugerencias.SelectedRows.Count == 0)
            {
                MostrarAviso(Localizacion("agenda.selSugerencia"));
                return;
            }
            int indice = _grillaSugerencias.SelectedRows[0].Index;
            if (indice < 0 || indice >= _sugerencias.Count)
            {
                return;
            }

            using var dialogo = new TextoDialogoForm(Localizacion("agenda.sugerencias"), Localizacion("agenda.motivoRechazo"));
            if (dialogo.ShowDialog(FindForm()) != DialogResult.OK || dialogo.Valor.Length == 0)
            {
                return;
            }

            bool escalar = MessageBox.Show(FindForm(), Localizacion("agenda.escalar"), Localizacion("agenda.rechazar"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

            try
            {
                using var contexto = new NegocioDbContext();
                new TurnoLogic(contexto).RechazarSugerencia(_sugerencias[indice], dialogo.Valor, escalar, _nombreUsuario);
                CargarTodo();
            }
            catch (ValidacionNegocioException ex)
            {
                MostrarAviso(string.Join(Environment.NewLine, ex.Errores));
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "AgendaControl");
            }
        }

        private void MostrarAviso(string mensaje)
        {
            MessageBox.Show(FindForm(), mensaje, Localizacion("error.titulo"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
