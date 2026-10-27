using Microsoft.EntityFrameworkCore;
using Negocio.DAL.Context;

namespace Negocio.DAL.Repositorios
{
    /// <summary>
    /// Implementación genérica de repositorio sobre EF Core: cada operación de escritura
    /// persiste de inmediato (SaveChanges). Los repositorios específicos heredan de esta
    /// clase y agregan consultas propias del dominio.
    /// </summary>
    public class RepositorioGenerico<T> : IRepositorioGenerico<T> where T : class
    {
        protected readonly NegocioDbContext Contexto;

        public RepositorioGenerico(NegocioDbContext contexto)
        {
            Contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
        }

        public T? ObtenerPorId(int id) => Contexto.Set<T>().Find(id);

        public List<T> ObtenerTodos() => Contexto.Set<T>().AsNoTracking().ToList();

        public void Agregar(T entidad)
        {
            Contexto.Set<T>().Add(entidad);
            Contexto.SaveChanges();
        }

        public void Modificar(T entidad)
        {
            Contexto.Set<T>().Update(entidad);
            Contexto.SaveChanges();
        }

        public void Eliminar(T entidad)
        {
            Contexto.Set<T>().Remove(entidad);
            Contexto.SaveChanges();
        }

        public bool Existe(int id) => Contexto.Set<T>().Find(id) != null;
    }
}
