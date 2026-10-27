using System;

namespace Services.DomainModel.Validacion
{
    /// <summary>
    /// Marca una propiedad como obligatoria (REQ-ARQ-007): el valor no puede ser nulo
    /// ni, en el caso de cadenas, vacío o compuesto solo por espacios.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class RequeridoAttribute : Attribute
    {
    }
}
