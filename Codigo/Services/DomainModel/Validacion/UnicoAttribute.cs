using System;

namespace Services.DomainModel.Validacion
{
    /// <summary>
    /// Marca una propiedad cuyo valor debe ser único (REQ-ARQ-007). La verificación real de
    /// unicidad (contra la base de datos) la aporta el invocante mediante un delegado al
    /// llamar a ValidarEntidad.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class UnicoAttribute : Attribute
    {
    }
}
