using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Negocio.DAL.Context;
using Negocio.DAL.Repositorios;
using Negocio.DomainModel;
using Negocio.DomainModel.Enums;
using Negocio.DomainModel.Exceptions;
using Services.DomainModel;
using Services.Facade;
using Usuario = Negocio.DomainModel.Usuario;

namespace Negocio.BLL
{
    /// <summary>Credenciales generadas para el portal de pacientes al dar el alta.</summary>
    public record CredencialesPortal(string Usuario, string PasswordTemporal);

    /// <summary>
    /// Lógica del ciclo de vida de pacientes (REQ-FUNC-001): alta con validaciones y
    /// credenciales únicas del portal (usuario = DNI), modificación con control de duplicidad
    /// y baja lógica con deshabilitación de credenciales sin borrar el historial clínico.
    /// </summary>
    public class PacienteLogic
    {
        private static readonly Regex FormatoDNI = new(@"^\d{7,8}$");
        private static readonly Regex FormatoEmail = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        private readonly NegocioDbContext _contexto;
        private readonly PacienteRepositorio _repositorio;

        public PacienteLogic(NegocioDbContext contexto)
        {
            _contexto = contexto;
            _repositorio = new PacienteRepositorio(contexto);
        }

        /// <summary>Pacientes activos ordenados por nombre.</summary>
        public List<Paciente> BuscarActivos() => _repositorio.BuscarActivos();

        /// <summary>Busca un paciente por documento, sin importar su estado.</summary>
        public Paciente? BuscarPorDNI(string dni) => _repositorio.BuscarPorDNI(dni);

        /// <summary>
        /// Pacientes según filtro de nombre/documento (vacío = todos), ordenados por nombre,
        /// con o sin inactivos (para la pantalla de gestión, REQ-FUNC-001).
        /// </summary>
        public List<Paciente> Buscar(string? filtro, bool incluirInactivos)
        {
            string patron = (filtro ?? string.Empty).Trim();
            IQueryable<Paciente> consulta = _contexto.Pacientes.AsNoTracking();
            if (!incluirInactivos)
            {
                consulta = consulta.Where(p => p.Estado == EstadoPaciente.Activo);
            }
            if (patron.Length > 0)
            {
                consulta = consulta.Where(p => p.NombreCompleto.Contains(patron) || p.DNI.Contains(patron));
            }
            return consulta.OrderBy(p => p.NombreCompleto).ToList();
        }

        /// <summary>
        /// Da de alta un paciente activo: valida los datos, verifica que no exista otro
        /// paciente activo con el mismo documento y genera credenciales únicas del portal.
        /// </summary>
        public CredencialesPortal RegistrarPaciente(Paciente paciente, string usuarioResponsable = "")
        {
            if (paciente == null)
            {
                throw new ArgumentNullException(nameof(paciente));
            }

            List<string> errores = ValidarDatosBasicos(paciente);
            if (_repositorio.ExisteDNIActivo(paciente.DNI))
            {
                errores.Add($"Ya existe un paciente activo con el documento '{paciente.DNI}'.");
            }
            if (paciente.IdObraSocial.HasValue && !_contexto.ObrasSociales.Any(o => o.Id == paciente.IdObraSocial.Value))
            {
                errores.Add("La obra social indicada no existe.");
            }
            if (errores.Count > 0)
            {
                throw ReglasNegocio.Rechazar(errores, $"Alta de paciente DNI {paciente.DNI}", usuarioResponsable);
            }

            string nombreUsuario = GenerarNombreUsuarioUnico(paciente.DNI);
            string passwordTemporal = GenerarPasswordTemporal();

            // Las credenciales viven en el módulo Services (fuente única de autenticación).
            SeguridadService.RegistrarUsuario(nombreUsuario, paciente.NombreCompleto, passwordTemporal, "paciente", paciente.Email);

            try
            {
                using var transaccion = _contexto.Database.BeginTransaction();

                paciente.Estado = EstadoPaciente.Activo;
                paciente.FechaAlta = DateTime.Now;
                _contexto.Pacientes.Add(paciente);
                _contexto.SaveChanges();

                var usuarioPortal = new Usuario
                {
                    NombreUsuario = nombreUsuario,
                    NombreCompleto = paciente.NombreCompleto,
                    Perfil = "paciente",
                    Email = paciente.Email,
                    Telefono = paciente.Telefono,
                    Activo = true
                };
                _contexto.Usuarios.Add(usuarioPortal);
                _contexto.SaveChanges();

                paciente.IdUsuarioPortal = usuarioPortal.Id;
                _contexto.SaveChanges();

                transaccion.Commit();
            }
            catch (Exception ex)
            {
                // Si la escritura local falla, la credencial recién creada queda auditada como huérfana.
                BitacoraService.Registrar(LogLevel.Error,
                    $"Alta de paciente '{paciente.NombreCompleto}' fallida tras crear la credencial del portal '{nombreUsuario}': {ex.Message}",
                    ex, usuarioResponsable, ReglasNegocio.Capa);
                throw;
            }

            BitacoraService.Registrar(LogLevel.Info,
                $"Paciente registrado: '{paciente.NombreCompleto}' (DNI {paciente.DNI}); credenciales del portal '{nombreUsuario}' generadas.",
                null, usuarioResponsable, ReglasNegocio.Capa);

            return new CredencialesPortal(nombreUsuario, passwordTemporal);
        }

        /// <summary>
        /// Modifica los datos personales de un paciente activo, verificando que no se
        /// generen duplicidades, y registra la modificación en la auditoría.
        /// </summary>
        public void ModificarPaciente(Paciente paciente, string usuarioResponsable = "")
        {
            if (paciente == null)
            {
                throw new ArgumentNullException(nameof(paciente));
            }

            Paciente existente = _repositorio.ObtenerPorId(paciente.Id)
                ?? throw new ValidacionNegocioException($"No existe el paciente con Id {paciente.Id}.");
            if (existente.Estado != EstadoPaciente.Activo)
            {
                throw new ValidacionNegocioException("Solo pueden modificarse pacientes activos.");
            }

            List<string> errores = ValidarDatosBasicos(paciente);
            if (_repositorio.ExisteDNIActivo(paciente.DNI, paciente.Id))
            {
                errores.Add($"Ya existe otro paciente activo con el documento '{paciente.DNI}'.");
            }
            if (errores.Count > 0)
            {
                throw ReglasNegocio.Rechazar(errores, $"Modificación de paciente Id {paciente.Id}", usuarioResponsable);
            }

            existente.NombreCompleto = paciente.NombreCompleto;
            existente.DNI = paciente.DNI;
            existente.Telefono = paciente.Telefono;
            existente.Email = paciente.Email;
            existente.IdObraSocial = paciente.IdObraSocial;
            existente.NumeroAfiliado = paciente.NumeroAfiliado;
            _repositorio.Modificar(existente);

            BitacoraService.Registrar(LogLevel.Info,
                $"Paciente modificado: '{existente.NombreCompleto}' (Id {existente.Id}, DNI {existente.DNI}).",
                null, usuarioResponsable, ReglasNegocio.Capa);
        }

        /// <summary>
        /// Da de baja lógica a un paciente: registra el motivo, pasa el estado a Inactivo
        /// y deshabilita sus credenciales del portal, conservando su historial clínico.
        /// </summary>
        public void DarDeBajaPaciente(int idPaciente, string motivo, string usuarioResponsable = "")
        {
            Paciente paciente = _repositorio.ObtenerPorId(idPaciente)
                ?? throw new ValidacionNegocioException($"No existe el paciente con Id {idPaciente}.");
            if (paciente.Estado != EstadoPaciente.Activo)
            {
                throw new ValidacionNegocioException("El paciente ya se encuentra dado de baja.");
            }
            if (string.IsNullOrWhiteSpace(motivo))
            {
                throw ReglasNegocio.Rechazar(
                    new List<string> { "El motivo de la baja es obligatorio." },
                    $"Baja de paciente Id {idPaciente}", usuarioResponsable);
            }

            paciente.Estado = EstadoPaciente.Inactivo;
            paciente.MotivoBaja = motivo.Trim();
            paciente.FechaBaja = DateTime.Now;
            _repositorio.Modificar(paciente);

            if (paciente.IdUsuarioPortal.HasValue)
            {
                Usuario? usuarioPortal = _contexto.Usuarios.Find(paciente.IdUsuarioPortal.Value);
                if (usuarioPortal != null)
                {
                    usuarioPortal.Activo = false;
                    _contexto.SaveChanges();
                    SeguridadService.CambiarEstadoUsuario(usuarioPortal.NombreUsuario, false, $"Baja del paciente (Id {paciente.Id}).");
                }
            }

            BitacoraService.Registrar(LogLevel.Info,
                $"Paciente dado de baja: '{paciente.NombreCompleto}' (Id {paciente.Id}). Motivo: {paciente.MotivoBaja}. Credenciales del portal deshabilitadas.",
                null, usuarioResponsable, ReglasNegocio.Capa);
        }

        private List<string> ValidarDatosBasicos(Paciente paciente)
        {
            var errores = new List<string>();
            if (!ConsistenciaService.ValidarEntidad(paciente, out List<string> erroresAtributos))
            {
                errores.AddRange(erroresAtributos);
            }
            if (!string.IsNullOrWhiteSpace(paciente.DNI) && !FormatoDNI.IsMatch(paciente.DNI.Trim()))
            {
                errores.Add("El documento debe contener solo números (7 u 8 dígitos).");
            }
            if (!string.IsNullOrWhiteSpace(paciente.Email) && !FormatoEmail.IsMatch(paciente.Email.Trim()))
            {
                errores.Add("El correo electrónico no tiene un formato válido.");
            }
            return errores;
        }

        /// <summary>Usuario del portal = DNI; si ese nombre ya existe se agrega un sufijo numérico.</summary>
        private static string GenerarNombreUsuarioUnico(string dni)
        {
            string baseNombre = dni.Trim();
            string candidato = baseNombre;
            int sufijo = 1;

            while (SeguridadService.ExisteNombreUsuario(candidato))
            {
                sufijo++;
                if (sufijo > 99)
                {
                    throw new ValidacionNegocioException(
                        $"No fue posible generar un nombre de usuario único para el documento '{dni}'.");
                }
                candidato = $"{baseNombre}_{sufijo}";
            }
            return candidato;
        }

        /// <summary>Contraseña temporal de 10 caracteres con mayúsculas, minúsculas, dígitos y símbolos.</summary>
        private static string GenerarPasswordTemporal()
        {
            const string mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string minusculas = "abcdefghijkmnpqrstuvwxyz";
            const string digitos = "23456789";
            const string simbolos = "!@#$%*";
            string universo = mayusculas + minusculas + digitos + simbolos;

            var partes = new List<char>
            {
                mayusculas[RandomNumberGenerator.GetInt32(mayusculas.Length)],
                mayusculas[RandomNumberGenerator.GetInt32(mayusculas.Length)],
                minusculas[RandomNumberGenerator.GetInt32(minusculas.Length)],
                minusculas[RandomNumberGenerator.GetInt32(minusculas.Length)],
                digitos[RandomNumberGenerator.GetInt32(digitos.Length)],
                digitos[RandomNumberGenerator.GetInt32(digitos.Length)],
                simbolos[RandomNumberGenerator.GetInt32(simbolos.Length)]
            };
            while (partes.Count < 10)
            {
                partes.Add(universo[RandomNumberGenerator.GetInt32(universo.Length)]);
            }

            // Mezcla Fisher-Yates para que la posición de cada categoría sea aleatoria.
            for (int i = partes.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (partes[i], partes[j]) = (partes[j], partes[i]);
            }
            return new string(partes.ToArray());
        }
    }
}
