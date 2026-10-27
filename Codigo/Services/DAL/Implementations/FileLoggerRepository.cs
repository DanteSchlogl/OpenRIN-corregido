using System;
using System.IO;
using Services.DAL.Interfaces;
using Services.DomainModel;

namespace Services.DAL.Implementations
{
    /// <summary>
    /// Persistencia de respaldo de bitácora en archivo de texto con rotación automática (10 MB).
    /// Se usa cuando la escritura en base de datos falla (REQ-ARQ-003).
    /// </summary>
    public class FileLoggerRepository : ILoggerRepository
    {
        private const long TamanoMaximoBytes = 10 * 1024 * 1024; // 10 MB (REQ-ARQ-003)
        private readonly string _rutaArchivo;

        public FileLoggerRepository(string rutaArchivo = "log_fallback.txt")
        {
            _rutaArchivo = rutaArchivo;
        }

        /// <inheritdoc />
        public void Registrar(LogEntry entrada)
        {
            RotarSiCorresponde();

            string linea =
                $"[{entrada.Fecha:yyyy-MM-dd HH:mm:ss}] [{entrada.Nivel}] {entrada.Mensaje} | Usuario: {entrada.Usuario ?? "-"} | Capa: {entrada.Capa ?? "-"}";
            if (!string.IsNullOrEmpty(entrada.Excepcion))
            {
                linea += Environment.NewLine + entrada.Excepcion;
            }
            File.AppendAllText(_rutaArchivo, linea + Environment.NewLine);
        }

        private void RotarSiCorresponde()
        {
            FileInfo info = new FileInfo(_rutaArchivo);
            if (info.Exists && info.Length >= TamanoMaximoBytes)
            {
                string destino = $"{_rutaArchivo}.{DateTime.Now:yyyyMMddHHmmss}.bak";
                File.Move(_rutaArchivo, destino);
            }
        }
    }
}
