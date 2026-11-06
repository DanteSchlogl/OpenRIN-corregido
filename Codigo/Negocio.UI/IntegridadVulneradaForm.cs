using System.Drawing;
using System.Windows.Forms;
using Negocio.BLL;
using Negocio.DAL.Context;
using Services.DomainModel;
using Services.DomainModel.Exceptions;
using Services.Facade;

namespace Negocio.UI
{
    /// <summary>
    /// Pantalla de bloqueo por integridad (T08): reemplaza a la ventana de log-in cuando la verificación de
    /// arranque detecta alteraciones en la base realizadas por fuera del sistema. Muestra la lista de registros
    /// alterados y permite recuperar la base: un usuario con permiso de respaldos (SI004) inicia sesión, elige un
    /// respaldo sano de la lista (o otro archivo .bak) y lo restaura, o recalcula los dígitos con confirmación.
    /// Todo queda en la bitácora (capa «Integridad»). Devuelve <see cref="DialogResult.Retry"/> si la base quedó sana.
    /// </summary>
    public sealed class IntegridadVulneradaForm : Form
    {
        /// <summary>Código del permiso que habilita la recuperación.</summary>
        public const string PermisoRespaldos = "SI004";
        private const string BaseNegocio = "OpenRIN_Negocio";

        private readonly ListView _lstAlterados = new();
        private readonly TextBox _txtUsuario = new();
        private readonly TextBox _txtClave = new();
        private readonly Button _btnEntrar = new();
        private readonly Label _lblEstadoSesion = new();
        private readonly Panel _panelRecuperacion = new();
        private readonly ListView _lstRespaldos = new();
        private readonly Button _btnRestaurarSel = new();
        private readonly Button _btnRestaurarArchivo = new();
        private readonly Button _btnRecalcular = new();
        private readonly Button _btnSalir = new();
        private UsuarioAutenticado? _sesion;

        private static string T(string clave) => LocalizationService.ObtenerTexto(clave);

        /// <summary>Crea la pantalla a partir del resultado de la auditoría (nulo si no pudo ejecutarse).</summary>
        public IntegridadVulneradaForm(ResultadoAuditoria? auditoria, IReadOnlyList<string> problemas)
        {
            Text = T("integridad.vulnerada.titulo");
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            ClientSize = new Size(760, 690);
            BackColor = Color.White;
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            Controls.Add(new Label
            {
                Text = "⚠", Font = new Font("Segoe UI", 36F, FontStyle.Bold), ForeColor = Color.FromArgb(200, 40, 40),
                Location = new Point(20, 10), Size = new Size(70, 64), TextAlign = ContentAlignment.MiddleCenter
            });
            Controls.Add(new Label
            {
                Text = T("integridad.vulnerada.titulo"), Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 25, 25), Location = new Point(96, 14), Size = new Size(640, 30)
            });
            Controls.Add(new Label
            {
                Text = T("integridad.vulnerada.mensaje"), Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(55, 55, 55), Location = new Point(98, 46), Size = new Size(640, 48)
            });

            Controls.Add(Etiqueta(T("integridad.vulnerada.registros"), 24, 100, 700));
            ConfigurarLista(_lstAlterados, new Point(24, 122), new Size(712, 150),
                (T("integridad.col.tabla"), 150), (T("integridad.col.id"), 60), (T("integridad.col.motivo"), 480));
            _lstAlterados.BackColor = Color.FromArgb(252, 245, 245);
            if (auditoria != null && auditoria.Alterados.Count > 0)
            {
                foreach (RegistroAlterado r in auditoria.Alterados)
                {
                    _lstAlterados.Items.Add(new ListViewItem(new[] { r.Tabla, r.Id == 0 ? "—" : r.Id.ToString(), r.Motivo }));
                }
            }
            else
            {
                foreach (string p in problemas)
                {
                    _lstAlterados.Items.Add(new ListViewItem(new[] { "—", "—", p }));
                }
            }
            Controls.Add(_lstAlterados);

            // ---- recuperación ----
            Controls.Add(new Label
            {
                Text = T("integridad.rec.titulo"), Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 79, 138), Location = new Point(24, 286), Size = new Size(700, 26)
            });
            Controls.Add(Etiqueta(T("integridad.rec.explicacion"), 24, 314, 712));

            Controls.Add(Etiqueta(T("integridad.rec.usuario"), 24, 342, 120));
            _txtUsuario.SetBounds(24, 362, 190, 26);
            Controls.Add(_txtUsuario);
            Controls.Add(Etiqueta(T("integridad.rec.clave"), 230, 342, 120));
            _txtClave.SetBounds(230, 362, 190, 26);
            _txtClave.UseSystemPasswordChar = true;
            Controls.Add(_txtClave);
            EstilizarBoton(_btnEntrar, T("integridad.rec.entrar"), Color.FromArgb(45, 108, 223));
            _btnEntrar.SetBounds(436, 358, 150, 34);
            _btnEntrar.Click += (s, e) => IniciarSesion();
            Controls.Add(_btnEntrar);
            AcceptButton = _btnEntrar;

            _lblEstadoSesion.SetBounds(24, 396, 712, 20);
            _lblEstadoSesion.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            Controls.Add(_lblEstadoSesion);

            _panelRecuperacion.SetBounds(24, 420, 712, 210);
            _panelRecuperacion.Enabled = false;
            _panelRecuperacion.Controls.Add(Etiqueta(T("integridad.rec.listaRespaldos"), 0, 0, 700));
            ConfigurarLista(_lstRespaldos, new Point(0, 22), new Size(712, 110),
                (T("integridad.rec.col.fecha"), 130), (T("integridad.rec.col.origen"), 80), (T("integridad.rec.col.estado"), 110),
                (T("integridad.rec.col.tamano"), 70), (T("integridad.rec.col.archivo"), 300));
            _panelRecuperacion.Controls.Add(_lstRespaldos);

            EstilizarBoton(_btnRestaurarSel, T("integridad.rec.restaurarSel"), Color.FromArgb(46, 125, 50));
            _btnRestaurarSel.SetBounds(0, 144, 220, 36);
            _btnRestaurarSel.Click += (s, e) => RestaurarSeleccionado();
            EstilizarBoton(_btnRestaurarArchivo, T("integridad.rec.restaurarArchivo"), Color.FromArgb(45, 108, 223));
            _btnRestaurarArchivo.SetBounds(232, 144, 240, 36);
            _btnRestaurarArchivo.Click += (s, e) => RestaurarDesdeArchivo();
            EstilizarBoton(_btnRecalcular, T("integridad.rec.recalcular"), Color.FromArgb(180, 120, 0));
            _btnRecalcular.SetBounds(484, 144, 228, 36);
            _btnRecalcular.Click += (s, e) => Recalcular();
            _panelRecuperacion.Controls.AddRange(new Control[] { _btnRestaurarSel, _btnRestaurarArchivo, _btnRecalcular });
            Controls.Add(_panelRecuperacion);

            EstilizarBoton(_btnSalir, T("integridad.vulnerada.salir"), Color.FromArgb(200, 40, 40));
            _btnSalir.SetBounds(606, 642, 130, 36);
            _btnSalir.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(_btnSalir);

            BitacoraService.Registrar(LogLevel.Fatal,
                $"Integridad — Alteración detectada: {(auditoria?.Alterados.Count ?? problemas.Count)} registro(s)/tabla(s) con dígitos verificadores inválidos. Ingreso bloqueado.",
                capa: "Integridad");
        }

        private void IniciarSesion()
        {
            try
            {
                UsuarioAutenticado usuario = SeguridadService.Autenticar(_txtUsuario.Text.Trim(), _txtClave.Text);
                if (!SeguridadService.TienePermiso(usuario.Id, PermisoRespaldos))
                {
                    BitacoraService.Warning($"Integridad — {usuario.NombreUsuario} intentó recuperar la base sin permiso de respaldos.", usuario.NombreUsuario);
                    Aviso(T("integridad.rec.sinPermiso"), MessageBoxIcon.Warning);
                    return;
                }

                _sesion = usuario;
                _txtClave.Clear();
                _lblEstadoSesion.ForeColor = Color.FromArgb(46, 125, 50);
                _lblEstadoSesion.Text = string.Format(T("integridad.rec.sesionDe"), usuario.NombreCompleto);
                _panelRecuperacion.Enabled = true;
                foreach (Button boton in new[] { _btnRestaurarSel, _btnRestaurarArchivo, _btnRecalcular })
                {
                    boton.ForeColor = Color.White; // al habilitar el panel, el botón vuelve a pintarse con su color
                    boton.Invalidate();
                }
                BitacoraService.Info("Integridad — inició sesión para recuperar la base.", usuario.NombreUsuario);
                CargarRespaldos();
            }
            catch (Exception ex)
            {
                Aviso(ex is ReglaNegocioException || ex is CredencialesInvalidasException || ex is UsuarioBloqueadoException
                    ? ex.Message : string.Format(T("integridad.rec.error"), ex.Message), MessageBoxIcon.Warning);
            }
        }

        private void CargarRespaldos()
        {
            _lstRespaldos.Items.Clear();
            try
            {
                foreach (RespaldoRegistro r in BackupService.ListarRespaldos().Where(x => x.BaseDatos == BaseNegocio))
                {
                    string estado = !r.ArchivoExiste ? T("integridad.rec.noExiste") : r.Sano ? T("integridad.rec.sano") : T("integridad.rec.noSano");
                    var item = new ListViewItem(new[]
                    {
                        r.Fecha.ToString("yyyy-MM-dd HH:mm:ss"), r.Origen, estado,
                        (r.TamanoBytes / 1048576.0).ToString("0.0") + " MB", r.Archivo
                    }) { Tag = r };
                    if (!r.ArchivoExiste || !r.Sano)
                    {
                        item.ForeColor = Color.Gray;
                    }
                    _lstRespaldos.Items.Add(item);
                }
                if (_lstRespaldos.Items.Count == 0)
                {
                    _lstRespaldos.Items.Add(new ListViewItem(new[] { "", "", T("integridad.rec.sinRespaldos"), "", "" }));
                }
            }
            catch (Exception ex)
            {
                Aviso(string.Format(T("integridad.rec.error"), ex.Message), MessageBoxIcon.Error);
            }
        }

        private void RestaurarSeleccionado()
        {
            if (_lstRespaldos.SelectedItems.Count == 0 || _lstRespaldos.SelectedItems[0].Tag is not RespaldoRegistro registro)
            {
                Aviso(T("sistema.respaldos.seleccione"), MessageBoxIcon.Information);
                return;
            }
            Restaurar(registro.Archivo);
        }

        private void RestaurarDesdeArchivo()
        {
            using var dialogo = new OpenFileDialog { Filter = T("integridad.rec.elegirArchivo"), CheckFileExists = true };
            if (dialogo.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }
            try
            {
                if (!string.Equals(BackupService.ObtenerBaseDelArchivo(dialogo.FileName), BaseNegocio, StringComparison.OrdinalIgnoreCase))
                {
                    Aviso(T("integridad.rec.soloNegocio"), MessageBoxIcon.Warning);
                    return;
                }
            }
            catch (Exception ex)
            {
                Aviso(string.Format(T("integridad.rec.error"), ex.Message), MessageBoxIcon.Error);
                return;
            }
            Restaurar(dialogo.FileName);
        }

        private void Restaurar(string archivo)
        {
            if (_sesion == null || !Confirmar(string.Format(T("integridad.rec.confirmarRestaurar"), archivo)))
            {
                return;
            }

            Cursor = Cursors.WaitCursor;
            try
            {
                BackupService.RestaurarBackup(archivo, _sesion.NombreUsuario);
                BitacoraService.Warning($"Integridad — la base de negocio se restauró desde {archivo}.", _sesion.NombreUsuario);
                TerminarSiEstaSana(T("integridad.rec.restaurada"));
            }
            catch (Exception ex)
            {
                BitacoraService.Error($"Integridad — falló la restauración desde {archivo}.", ex, _sesion.NombreUsuario);
                Aviso(string.Format(T("integridad.rec.error"), ex.Message), MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void Recalcular()
        {
            if (_sesion == null || !Confirmar(T("integridad.rec.confirmarRecalcular")))
            {
                return;
            }

            Cursor = Cursors.WaitCursor;
            try
            {
                int filas;
                using (var contexto = new NegocioDbContext())
                {
                    filas = new IntegridadLogic(contexto).RecalcularTodo();
                }
                BitacoraService.Warning($"Integridad — dígitos verificadores recalculados ({filas} filas): se aceptó el estado actual de los datos.", _sesion.NombreUsuario);
                TerminarSiEstaSana(string.Format(T("integridad.rec.recalculada"), filas));
            }
            catch (Exception ex)
            {
                BitacoraService.Error("Integridad — falló el recálculo de dígitos.", ex, _sesion.NombreUsuario);
                Aviso(string.Format(T("integridad.rec.error"), ex.Message), MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        /// <summary>Vuelve a auditar: si quedó sana cierra la pantalla y deja seguir al ingreso; si no, actualiza la lista.</summary>
        private void TerminarSiEstaSana(string mensajeOk)
        {
            ResultadoAuditoria? nueva = VerificacionArranque.Auditar();
            if (nueva != null && nueva.Problemas.Count == 0)
            {
                BitacoraService.Info("Integridad — verificación posterior correcta: se habilita el ingreso.", _sesion?.NombreUsuario ?? "");
                Aviso(mensajeOk, MessageBoxIcon.Information);
                DialogResult = DialogResult.Retry;
                Close();
                return;
            }

            _lstAlterados.Items.Clear();
            foreach (RegistroAlterado r in nueva?.Alterados ?? new List<RegistroAlterado>())
            {
                _lstAlterados.Items.Add(new ListViewItem(new[] { r.Tabla, r.Id == 0 ? "—" : r.Id.ToString(), r.Motivo }));
            }
            Aviso(T("integridad.rec.sigueVulnerada"), MessageBoxIcon.Warning);
            CargarRespaldos();
        }

        private static Label Etiqueta(string texto, int x, int y, int ancho) => new()
        {
            Text = texto, Location = new Point(x, y), Size = new Size(ancho, 20),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(70, 70, 70)
        };

        private static void ConfigurarLista(ListView lista, Point ubicacion, Size tamano, params (string Titulo, int Ancho)[] columnas)
        {
            lista.Location = ubicacion;
            lista.Size = tamano;
            lista.View = View.Details;
            lista.FullRowSelect = true;
            lista.GridLines = true;
            lista.MultiSelect = false;
            lista.HideSelection = false;
            lista.Font = new Font("Segoe UI", 9F);
            foreach ((string titulo, int ancho) in columnas)
            {
                lista.Columns.Add(titulo, ancho);
            }
        }

        private static void EstilizarBoton(Button boton, string texto, Color fondo)
        {
            boton.Text = texto;
            boton.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            boton.BackColor = fondo;
            boton.ForeColor = Color.White;
            boton.FlatStyle = FlatStyle.Flat;
            boton.FlatAppearance.BorderSize = 0;
            boton.Cursor = Cursors.Hand;
        }

        private bool Confirmar(string texto)
            => MessageBox.Show(this, texto, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

        private void Aviso(string texto, MessageBoxIcon icono) => MessageBox.Show(this, texto, Text, MessageBoxButtons.OK, icono);
    }
}
