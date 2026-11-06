using System.Drawing;
using System.Windows.Forms;
using Services.DomainModel;
using Services.Facade;

namespace Negocio.UI
{
    /// <summary>
    /// Pestaña «Respaldos» de Sistema y Bitácora: elegir la carpeta, respaldar ahora (COPY_ONLY + CHECKSUM, verificado),
    /// verificar un archivo, ver la tabla Respaldos (origen, estado sano o no, tamaño y usuario) y restaurar con copia
    /// previa de seguridad. Requiere el permiso SI004; la restauración de la base de negocio queda en la bitácora.
    /// </summary>
    internal sealed class RespaldosPanel : Panel
    {
        private readonly Label _lblCarpeta = new();
        private readonly TextBox _txtCarpeta = new();
        private readonly Button _btnElegir = new();
        private readonly Button _btnAhora = new();
        private readonly Button _btnVerificar = new();
        private readonly Button _btnRestaurar = new();
        private readonly Button _btnRestaurarArchivo = new();
        private readonly Button _btnActualizar = new();
        private readonly DataGridView _grilla = new();

        private static string T(string clave) => LocalizationService.ObtenerTexto(clave);

        public RespaldosPanel()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.White;

            _lblCarpeta.SetBounds(14, 16, 150, 22);
            _lblCarpeta.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _txtCarpeta.SetBounds(166, 13, 420, 26);
            _txtCarpeta.Text = CarpetaPorDefecto();
            Estilo(_btnElegir, Color.White, Color.FromArgb(28, 60, 105));
            _btnElegir.SetBounds(596, 11, 110, 30);
            _btnElegir.Click += (s, e) => ElegirCarpeta();
            Estilo(_btnAhora, Color.FromArgb(45, 108, 223), Color.White);
            _btnAhora.SetBounds(716, 11, 160, 30);
            _btnAhora.Click += (s, e) => RespaldarAhora();

            Estilo(_btnVerificar, Color.White, Color.FromArgb(28, 60, 105));
            _btnVerificar.SetBounds(14, 52, 160, 30);
            _btnVerificar.Click += (s, e) => VerificarArchivo();
            Estilo(_btnRestaurar, Color.FromArgb(46, 125, 50), Color.White);
            _btnRestaurar.SetBounds(184, 52, 200, 30);
            _btnRestaurar.Click += (s, e) => RestaurarSeleccionado();
            Estilo(_btnRestaurarArchivo, Color.White, Color.FromArgb(28, 60, 105));
            _btnRestaurarArchivo.SetBounds(394, 52, 200, 30);
            _btnRestaurarArchivo.Click += (s, e) => RestaurarDesdeArchivo();
            Estilo(_btnActualizar, Color.White, Color.FromArgb(28, 60, 105));
            _btnActualizar.SetBounds(604, 52, 110, 30);
            _btnActualizar.Click += (s, e) => Recargar();

            var host = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 96, 14, 14) };
            _grilla.Dock = DockStyle.Fill;
            _grilla.ReadOnly = true;
            _grilla.AllowUserToAddRows = false;
            _grilla.AllowUserToDeleteRows = false;
            _grilla.RowHeadersVisible = false;
            _grilla.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grilla.MultiSelect = false;
            _grilla.BackgroundColor = Color.White;
            _grilla.BorderStyle = BorderStyle.None;
            _grilla.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grilla.EnableHeadersVisualStyles = false;
            _grilla.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(28, 60, 105);
            _grilla.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _grilla.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _grilla.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            foreach ((string nombre, float peso) in new[] { ("fecha", 18f), ("base", 15f), ("origen", 10f), ("estado", 12f), ("tamano", 9f), ("usuario", 12f), ("archivo", 40f) })
            {
                _grilla.Columns.Add(new DataGridViewTextBoxColumn { Name = nombre, FillWeight = peso });
            }
            host.Controls.Add(_grilla);

            Controls.Add(host);
            Controls.AddRange(new Control[] { _lblCarpeta, _txtCarpeta, _btnElegir, _btnAhora, _btnVerificar, _btnRestaurar, _btnRestaurarArchivo, _btnActualizar });
            foreach (Control c in new Control[] { _lblCarpeta, _txtCarpeta, _btnElegir, _btnAhora, _btnVerificar, _btnRestaurar, _btnRestaurarArchivo, _btnActualizar })
            {
                c.BringToFront();
            }
            AplicarTextos();
        }

        /// <summary>Reaplica los textos del idioma activo.</summary>
        public void AplicarTextos()
        {
            _lblCarpeta.Text = T("sistema.respaldos.carpeta");
            _btnElegir.Text = T("sistema.respaldos.elegir");
            _btnAhora.Text = T("sistema.respaldos.ahora");
            _btnVerificar.Text = T("sistema.respaldos.verificar");
            _btnRestaurar.Text = T("sistema.respaldos.restaurar");
            _btnRestaurarArchivo.Text = T("sistema.respaldos.restaurarArchivo");
            _btnActualizar.Text = T("sistema.respaldos.actualizar");
            foreach (string c in new[] { "fecha", "base", "origen", "estado", "tamano", "usuario", "archivo" })
            {
                _grilla.Columns[c].HeaderText = T("sistema.respaldos.col." + c);
            }
        }

        /// <summary>Vuelve a leer la tabla Respaldos.</summary>
        public void Recargar()
        {
            _grilla.Rows.Clear();
            try
            {
                foreach (RespaldoRegistro r in BackupService.ListarRespaldos())
                {
                    string estado = !r.ArchivoExiste ? T("integridad.rec.noExiste") : r.Sano ? T("integridad.rec.sano") : T("integridad.rec.noSano");
                    int fila = _grilla.Rows.Add(r.Fecha.ToString("yyyy-MM-dd HH:mm:ss"), r.BaseDatos, r.Origen, estado,
                        (r.TamanoBytes / 1048576.0).ToString("0.0"), r.Usuario, r.Archivo);
                    _grilla.Rows[fila].Tag = r;
                }
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "Listado de respaldos");
            }
        }

        private static string CarpetaPorDefecto()
        {
            string? configurada = System.Configuration.ConfigurationManager.AppSettings["BackupFolder"];
            return string.IsNullOrWhiteSpace(configurada)
                ? Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\", "OpenRIN", "Backups")
                : configurada.Trim();
        }

        private void ElegirCarpeta()
        {
            using var dialogo = new FolderBrowserDialog { SelectedPath = _txtCarpeta.Text, UseDescriptionForTitle = true, Description = T("sistema.respaldos.carpeta") };
            if (dialogo.ShowDialog(FindForm()) == DialogResult.OK)
            {
                _txtCarpeta.Text = dialogo.SelectedPath;
            }
        }

        private void RespaldarAhora()
        {
            Ejecutar(() =>
            {
                List<string> archivos = BackupService.RealizarBackupDeLaPlataforma(
                    TipoBackup.Completo, _txtCarpeta.Text.Trim(), SesionActual.Instancia.NombreUsuario);
                Informar(string.Format(T("sistema.respaldos.generado"), string.Join("\n", archivos)), MessageBoxIcon.Information);
            });
            Recargar();
        }

        private void VerificarArchivo()
        {
            string? archivo = SeleccionadoOElegir();
            if (archivo == null)
            {
                return;
            }
            Ejecutar(() =>
            {
                bool ok = BackupService.VerificarIntegridadBackup(archivo);
                Informar(ok ? T("sistema.respaldos.verificado") : T("sistema.respaldos.noVerificado"),
                    ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            });
        }

        private void RestaurarSeleccionado()
        {
            if (_grilla.CurrentRow?.Tag is not RespaldoRegistro registro)
            {
                Informar(T("sistema.respaldos.seleccione"), MessageBoxIcon.Information);
                return;
            }
            Restaurar(registro.Archivo, registro.BaseDatos);
        }

        private void RestaurarDesdeArchivo()
        {
            using var dialogo = new OpenFileDialog { Filter = T("integridad.rec.elegirArchivo"), CheckFileExists = true };
            if (dialogo.ShowDialog(FindForm()) != DialogResult.OK)
            {
                return;
            }
            Ejecutar(() =>
            {
                string baseDatos = BackupService.ObtenerBaseDelArchivo(dialogo.FileName);
                BackupService.RegistrarImportado(dialogo.FileName, SesionActual.Instancia.NombreUsuario);
                Restaurar(dialogo.FileName, baseDatos);
            });
        }

        private void Restaurar(string archivo, string baseDatos)
        {
            if (MessageBox.Show(FindForm(), string.Format(T("sistema.respaldos.confirmarRestaurar"), baseDatos, archivo), "OpenRIN",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }
            Ejecutar(() =>
            {
                BackupService.RestaurarBackup(archivo, SesionActual.Instancia.NombreUsuario);
                Informar(T("sistema.respaldos.restaurado"), MessageBoxIcon.Information);
            });
            Recargar();
        }

        private string? SeleccionadoOElegir()
        {
            if (_grilla.CurrentRow?.Tag is RespaldoRegistro registro)
            {
                return registro.Archivo;
            }
            using var dialogo = new OpenFileDialog { Filter = T("integridad.rec.elegirArchivo"), CheckFileExists = true };
            return dialogo.ShowDialog(FindForm()) == DialogResult.OK ? dialogo.FileName : null;
        }

        private void Ejecutar(Action accion)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                accion();
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "Respaldos");
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void Informar(string texto, MessageBoxIcon icono)
            => MessageBox.Show(FindForm(), texto, "OpenRIN", MessageBoxButtons.OK, icono);

        private static void Estilo(Button boton, Color fondo, Color texto)
        {
            boton.FlatStyle = FlatStyle.Flat;
            boton.FlatAppearance.BorderColor = Color.FromArgb(202, 220, 240);
            boton.BackColor = fondo;
            boton.ForeColor = texto;
            boton.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            boton.Cursor = Cursors.Hand;
        }
    }
}
