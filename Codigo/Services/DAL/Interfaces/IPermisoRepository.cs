using System.Collections.Generic;
using Services.DomainModel;

namespace Services.DAL.Interfaces
{
    /// <summary>
    /// Contrato de acceso a datos del sistema de permisos (T04): catálogo en árbol y
    /// asignaciones por perfil, almacenados en la base.
    /// </summary>
    internal interface IPermisoRepository
    {
        /// <summary>Catálogo completo de permisos como filas planas (código, nombre, tipo y padre).</summary>
        List<PermisoFila> ObtenerCatalogo();

        /// <summary>Perfiles conocidos: los que tienen asignaciones más los usados por usuarios.</summary>
        List<string> ObtenerPerfiles();

        /// <summary>Códigos de permisos asignados directamente a un perfil.</summary>
        List<string> ObtenerAsignacionesDePerfil(string perfil);

        /// <summary>Reemplaza por completo las asignaciones de un perfil (asignación rápida).</summary>
        void ReemplazarAsignaciones(string perfil, List<string> codigos);
    }
}
