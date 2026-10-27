namespace Negocio.DomainModel.Enums
{
    /// <summary>
    /// Estado del ciclo de vida de un paciente (REQ-FUNC-001). La baja es lógica:
    /// el historial clínico nunca se elimina.
    /// </summary>
    public enum EstadoPaciente
    {
        /// <summary>Paciente registrado y operativo en el sistema.</summary>
        Activo,

        /// <summary>Paciente dado de baja (credenciales deshabilitadas, historial conservado).</summary>
        Inactivo
    }
}
