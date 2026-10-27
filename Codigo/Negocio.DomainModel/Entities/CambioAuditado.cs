namespace Negocio.DomainModel
{
    /// <summary>
    /// Registro de auditoría de cambios (requisito T06b): documenta quién, cuándo y qué cambió
    /// sobre cada entidad del negocio, guardando el estado anterior y el nuevo en formato JSON.
    /// Esta tabla no se firma con DVH/DVV a propósito: es ella misma la evidencia de auditoría
    /// y su alteración se detecta por su propio historial, no por dígitos verificadores.
    /// </summary>
    public class CambioAuditado
    {
        /// <summary>Identificador del registro de auditoría.</summary>
        public int Id { get; set; }

        /// <summary>Nombre de la tabla afectada (por ejemplo, "Pacientes").</summary>
        public string Entidad { get; set; } = string.Empty;

        /// <summary>Identificador de la fila afectada dentro de su entidad.</summary>
        public int IdRegistro { get; set; }

        /// <summary>Momento en que se registró el cambio.</summary>
        public DateTime FechaCambio { get; set; }

        /// <summary>Usuario responsable del cambio (o "sistema" para procesos automáticos).</summary>
        public string Usuario { get; set; } = string.Empty;

        /// <summary>Tipo de cambio: Alta, Modificacion o Baja.</summary>
        public string TipoCambio { get; set; } = string.Empty;

        /// <summary>Estado anterior del registro en formato JSON (null en las altas).</summary>
        public string? DatosAnteriores { get; set; }

        /// <summary>Estado nuevo del registro en formato JSON (null en las bajas).</summary>
        public string? DatosNuevos { get; set; }
    }
}
