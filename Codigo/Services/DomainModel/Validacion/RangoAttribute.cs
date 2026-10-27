using System;

namespace Services.DomainModel.Validacion
{
    /// <summary>
    /// Marca una propiedad numérica con un rango válido [Mínimo, Máximo] (REQ-ARQ-007).
    /// Ejemplo de uso: [Rango(0.5, 10.0)].
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class RangoAttribute : Attribute
    {
        /// <summary>Límite inferior válido (inclusive).</summary>
        public double Minimo { get; }

        /// <summary>Límite superior válido (inclusive).</summary>
        public double Maximo { get; }

        public RangoAttribute(double minimo, double maximo)
        {
            Minimo = minimo;
            Maximo = maximo;
        }
    }
}
