using System;

namespace Services.DomainModel
{
    /// <summary>Fila de la tabla Respaldos: un archivo .bak generado, restaurado o importado.</summary>
    public class RespaldoRegistro
    {
        /// <summary>Identificador del registro.</summary>
        public int Id { get; set; }

        /// <summary>Fecha y hora de creación del respaldo.</summary>
        public DateTime Fecha { get; set; }

        /// <summary>Base de datos respaldada.</summary>
        public string BaseDatos { get; set; } = "";

        /// <summary>Ruta completa del archivo .bak.</summary>
        public string Archivo { get; set; } = "";

        /// <summary>Tamaño del archivo en bytes.</summary>
        public long TamanoBytes { get; set; }

        /// <summary>Manual, Automatico, Previo (copia de seguridad antes de restaurar) o Importado.</summary>
        public string Origen { get; set; } = "Manual";

        /// <summary>Verdadero si el archivo pasó la verificación (RESTORE VERIFYONLY con CHECKSUM).</summary>
        public bool Sano { get; set; }

        /// <summary>Usuario que lo generó.</summary>
        public string Usuario { get; set; } = "";

        /// <summary>Detalle (por ejemplo, que se copió desde la carpeta del servidor).</summary>
        public string Observaciones { get; set; } = "";

        /// <summary>Indica si el archivo sigue existiendo en disco (no se guarda en la base).</summary>
        public bool ArchivoExiste { get; set; }
    }
}
