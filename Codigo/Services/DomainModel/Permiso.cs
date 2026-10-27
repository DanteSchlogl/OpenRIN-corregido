using System;
using System.Collections.Generic;
using System.Linq;

namespace Services.DomainModel
{
    /// <summary>
    /// Componente del patrón Composite (T04): un permiso puede ser atómico (representa una
    /// funcionalidad) o compuesto (agrupa a un conjunto de permisos). El código único
    /// identifica al permiso dentro del sistema (por ejemplo, "PA002" o "GE010").
    /// </summary>
    public abstract class Permiso
    {
        /// <summary>Crea un permiso con su código único y nombre para mostrar.</summary>
        protected Permiso(string codigo, string nombre)
        {
            Codigo = codigo;
            Nombre = nombre;
        }

        /// <summary>Código único del permiso dentro del sistema.</summary>
        public string Codigo { get; }

        /// <summary>Nombre para mostrar del permiso.</summary>
        public string Nombre { get; }

        /// <summary>Indica si este permiso incluye (directa o recursivamente) el código indicado.</summary>
        public abstract bool Incluye(string codigoPermiso);

        /// <summary>Devuelve este código y, recursivamente, los de los permisos que agrupa.</summary>
        public abstract IEnumerable<string> ObtenerCodigos();

        /// <summary>Representación legible (código y nombre).</summary>
        public override string ToString() => $"{Codigo} {Nombre}";
    }

    /// <summary>Permiso atómico (hoja del composite): representa una funcionalidad concreta.</summary>
    public sealed class PermisoAtomico : Permiso
    {
        /// <summary>Crea un permiso atómico.</summary>
        public PermisoAtomico(string codigo, string nombre) : base(codigo, nombre)
        {
        }

        /// <inheritdoc />
        public override bool Incluye(string codigoPermiso)
            => string.Equals(Codigo, codigoPermiso, StringComparison.OrdinalIgnoreCase);

        /// <inheritdoc />
        public override IEnumerable<string> ObtenerCodigos()
        {
            yield return Codigo;
        }
    }

    /// <summary>Permiso compuesto (nodo del composite): agrupa un conjunto de permisos.</summary>
    public sealed class PermisoCompuesto : Permiso
    {
        private readonly List<Permiso> _hijos = new();

        /// <summary>Crea un permiso compuesto (por ejemplo, una gestión o un perfil).</summary>
        public PermisoCompuesto(string codigo, string nombre) : base(codigo, nombre)
        {
        }

        /// <summary>Permisos que este compuesto agrupa.</summary>
        public IReadOnlyList<Permiso> Hijos => _hijos;

        /// <summary>Agrega un permiso (atómico o compuesto) al grupo.</summary>
        public void Agregar(Permiso hijo)
            => _hijos.Add(hijo ?? throw new ArgumentNullException(nameof(hijo)));

        /// <inheritdoc />
        public override bool Incluye(string codigoPermiso)
            => string.Equals(Codigo, codigoPermiso, StringComparison.OrdinalIgnoreCase)
               || _hijos.Any(hijo => hijo.Incluye(codigoPermiso));

        /// <inheritdoc />
        public override IEnumerable<string> ObtenerCodigos()
        {
            yield return Codigo;
            foreach (Permiso hijo in _hijos)
            {
                foreach (string codigo in hijo.ObtenerCodigos())
                {
                    yield return codigo;
                }
            }
        }
    }

    /// <summary>Fila plana del catálogo de permisos (para construir el árbol en memoria).</summary>
    public sealed record PermisoFila(string Codigo, string Nombre, string Tipo, string? CodigoPadre);
}
