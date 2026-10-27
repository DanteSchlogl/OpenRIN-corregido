namespace Negocio.UI
{
    /// <summary>
    /// Recursos visuales embebidos de OpenRIN: el logo (openrin.ico) se carga desde el
    /// ensamblado, sin depender de archivos externos en disco.
    /// </summary>
    internal static class Recursos
    {
        /// <summary>Carga el logo embebido en el tamaño más cercano al solicitado, o null si falla.</summary>
        public static Icon? CargarIcono(int tamano)
        {
            using Stream? flujo = typeof(Recursos).Assembly
                .GetManifestResourceStream("Negocio.UI.openrin.ico");
            return flujo == null ? null : new Icon(flujo, new Size(tamano, tamano));
        }
    }
}
