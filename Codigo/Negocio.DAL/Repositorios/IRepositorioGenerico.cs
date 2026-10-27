namespace Negocio.DAL.Repositorios
{
    /// <summary>
    /// Contrato genérico de repositorio de la capa de datos del módulo Negocio (EF Core).
    /// </summary>
    public interface IRepositorioGenerico<T> where T : class
    {
        /// <summary>Obtiene una entidad por su identificador (o null si no existe).</summary>
        T? ObtenerPorId(int id);

        /// <summary>Obtiene todas las entidades del conjunto (sin seguimiento).</summary>
        List<T> ObtenerTodos();

        /// <summary>Agrega una entidad nueva y persiste el cambio.</summary>
        void Agregar(T entidad);

        /// <summary>Marca una entidad como modificada y persiste el cambio.</summary>
        void Modificar(T entidad);

        /// <summary>Elimina físicamente una entidad y persiste el cambio
        /// (el dominio clínico usa baja lógica; reservado para catálogos y pruebas).</summary>
        void Eliminar(T entidad);

        /// <summary>Indica si existe una entidad con el identificador indicado.</summary>
        bool Existe(int id);
    }
}
