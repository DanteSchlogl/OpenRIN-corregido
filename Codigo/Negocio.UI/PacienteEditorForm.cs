using Microsoft.EntityFrameworkCore;
using Negocio.BLL;
using Negocio.DAL.Context;
using Negocio.DomainModel;
using Negocio.DomainModel.Exceptions;
using Services.Facade;

namespace Negocio.UI
{
    /// <summary>
    /// Alta (REQ-FUNC-001) y modificación de pacientes. En el alta se generan las credenciales
    /// únicas del portal y se muestran una única vez para entregarlas al paciente.
    /// </summary>
    public class PacienteEditorForm : Form
    {
        private sealed class OpcionObra
        {
            public int? Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public override string ToString() => Nombre;
        }

        private readonly Paciente? _pacienteExistente;
        private readonly string _usuarioResponsable;
        private readonly bool _esAlta;

        private readonly TextBox _txtNombre = new();
        private readonly TextBox _txtDni = new();
        private readonly TextBox _txtTelefono = new();
        private readonly TextBox _txtEmail = new();
        private readonly ComboBox _cmbObraSocial = new();
        private readonly TextBox _txtAfiliado = new();

        public PacienteEditorForm(Paciente? paciente, string usuarioResponsable)
        {
            _pacienteExistente = paciente;
            _usuarioResponsable = usuarioResponsable;
            _esAlta = paciente == null;

            Text = $"OpenRIN — {LocalizationService.ObtenerTexto(_esAlta ? "pacientes.alta" : "comun.modificar")}";
            ClientSize = new Size(480, 380);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Color.White;

            ConstruirInterfaz();
            CargarObras();
            if (paciente != null)
            {
                Prefil(paciente);
            }
        }

        private static string Texto(string clave) => LocalizationService.ObtenerTexto(clave);

        private static Label Etiqueta(string clave, int x, int y)
            => new()
            {
                Text = Texto(clave),
                Location = new Point(x, y),
                Size = new Size(210, 18),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

        private void ConstruirInterfaz()
        {
            Controls.Add(Etiqueta("pacientes.nombre", 18, 14));
            _txtNombre.Location = new Point(18, 34);
            _txtNombre.Size = new Size(444, 28);
            _txtNombre.Font = new Font("Segoe UI", 10.5F);

            Controls.Add(Etiqueta("pacientes.documento", 18, 70));
            _txtDni.Location = new Point(18, 90);
            _txtDni.Size = new Size(200, 28);
            _txtDni.Font = new Font("Segoe UI", 10.5F);

            Controls.Add(Etiqueta("pacientes.telefono", 234, 70));
            _txtTelefono.Location = new Point(234, 90);
            _txtTelefono.Size = new Size(228, 28);
            _txtTelefono.Font = new Font("Segoe UI", 10.5F);

            Controls.Add(Etiqueta("pacientes.email", 18, 126));
            _txtEmail.Location = new Point(18, 146);
            _txtEmail.Size = new Size(444, 28);
            _txtEmail.Font = new Font("Segoe UI", 10.5F);

            Controls.Add(Etiqueta("pacientes.obraSocial", 18, 182));
            _cmbObraSocial.Location = new Point(18, 202);
            _cmbObraSocial.Size = new Size(280, 28);
            _cmbObraSocial.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbObraSocial.Font = new Font("Segoe UI", 10F);

            Controls.Add(Etiqueta("pacientes.numeroAfiliado", 310, 182));
            _txtAfiliado.Location = new Point(310, 202);
            _txtAfiliado.Size = new Size(152, 28);
            _txtAfiliado.Font = new Font("Segoe UI", 10.5F);

            var btnGuardar = new Button
            {
                Text = Texto("comun.guardar"),
                Location = new Point(276, 266),
                Size = new Size(90, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 108, 223),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            var btnCancelar = new Button
            {
                Text = Texto("comun.cancelar"),
                Location = new Point(374, 266),
                Size = new Size(90, 36),
                FlatStyle = FlatStyle.Flat
            };
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; };

            AcceptButton = btnGuardar;
            CancelButton = btnCancelar;

            Controls.AddRange(new Control[]
            {
                _txtNombre, _txtDni, _txtTelefono, _txtEmail, _cmbObraSocial, _txtAfiliado, btnGuardar, btnCancelar
            });
        }

        private void CargarObras()
        {
            using var contexto = new NegocioDbContext();
            var obras = contexto.ObrasSociales.AsNoTracking()
                .Where(o => o.Activo)
                .OrderBy(o => o.Nombre)
                .ToList();

            _cmbObraSocial.Items.Add(new OpcionObra { Id = null, Nombre = Texto("pacientes.sinObraSocial") });
            foreach (ObraSocial obra in obras)
            {
                _cmbObraSocial.Items.Add(new OpcionObra { Id = obra.Id, Nombre = obra.Nombre });
            }
            _cmbObraSocial.SelectedIndex = 0;
        }

        private void Prefil(Paciente paciente)
        {
            _txtNombre.Text = paciente.NombreCompleto;
            _txtDni.Text = paciente.DNI;
            _txtTelefono.Text = paciente.Telefono;
            _txtEmail.Text = paciente.Email;
            _txtAfiliado.Text = paciente.NumeroAfiliado ?? string.Empty;

            foreach (OpcionObra? opcion in _cmbObraSocial.Items)
            {
                if (opcion != null && opcion.Id == paciente.IdObraSocial)
                {
                    _cmbObraSocial.SelectedItem = opcion;
                    break;
                }
            }
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            try
            {
                using var contexto = new NegocioDbContext();
                var logica = new PacienteLogic(contexto);
                OpcionObra? obra = _cmbObraSocial.SelectedItem as OpcionObra;

                if (_esAlta)
                {
                    var nuevo = new Paciente
                    {
                        NombreCompleto = _txtNombre.Text.Trim(),
                        DNI = _txtDni.Text.Trim(),
                        Telefono = _txtTelefono.Text.Trim(),
                        Email = _txtEmail.Text.Trim(),
                        IdObraSocial = obra?.Id,
                        NumeroAfiliado = string.IsNullOrWhiteSpace(_txtAfiliado.Text) ? null : _txtAfiliado.Text.Trim()
                    };

                    CredencialesPortal credenciales = logica.RegistrarPaciente(nuevo, _usuarioResponsable);
                    using (var credencialesForm = new CredencialesPortalForm(credenciales))
                    {
                        credencialesForm.ShowDialog(this);
                    }
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    var editado = new Paciente
                    {
                        Id = _pacienteExistente!.Id,
                        NombreCompleto = _txtNombre.Text.Trim(),
                        DNI = _txtDni.Text.Trim(),
                        Telefono = _txtTelefono.Text.Trim(),
                        Email = _txtEmail.Text.Trim(),
                        IdObraSocial = obra?.Id,
                        NumeroAfiliado = string.IsNullOrWhiteSpace(_txtAfiliado.Text) ? null : _txtAfiliado.Text.Trim()
                    };

                    logica.ModificarPaciente(editado, _usuarioResponsable);
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            catch (ValidacionNegocioException ex)
            {
                MessageBox.Show(this, string.Join(Environment.NewLine, ex.Errores),
                    Texto("error.titulo"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                ExceptionManager.ManejarExcepcion(ex, "PacienteEditorForm");
            }
        }
    }
}
