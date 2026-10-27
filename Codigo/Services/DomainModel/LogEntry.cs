using System;

namespace Services.DomainModel
{
    /// <summary>Entrada de bitácora: un evento registrado por cualquier capa del sistema (REQ-ARQ-003).</summary>
    public class LogEntry
    {
        public int Id { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        public LogLevel Nivel { get; set; }

        public string Mensaje { get; set; } = string.Empty;

        public string? Excepcion { get; set; }

        public string? Usuario { get; set; }

        public string? Capa { get; set; }
    }
}
