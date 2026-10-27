namespace Negocio.DomainModel.Enums
{
    /// <summary>
    /// Canal por el que se recibió un reporte de RIN (REQ-FUNC-004):
    /// carga directa en la plataforma o carga manual tras un reporte telefónico.
    /// </summary>
    public enum CanalReporteRIN
    {
        /// <summary>Carga directa del paciente/familiar en el portal (o digital).</summary>
        Digital,

        /// <summary>Carga manual del administrativo tras reporte telefónico.</summary>
        Telefonico
    }
}
