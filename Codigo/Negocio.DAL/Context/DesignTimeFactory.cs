using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Negocio.DAL.Context
{
    /// <summary>
    /// Fábrica de diseño para las herramientas de Entity Framework Core (dotnet ef):
    /// permite crear y aplicar migraciones usando autenticación integrada de Windows
    /// contra el servidor local. La aplicación en runtime usa la cadena "NegocioDB"
    /// de su propia configuración (con el login de mínimo privilegio usr_negocio).
    /// </summary>
    public class DesignTimeFactory : IDesignTimeDbContextFactory<NegocioDbContext>
    {
        public NegocioDbContext CreateDbContext(string[] args)
        {
            DbContextOptions<NegocioDbContext> opciones = new DbContextOptionsBuilder<NegocioDbContext>()
                .UseSqlServer("Server=localhost;Database=OpenRIN_Negocio;Integrated Security=True;TrustServerCertificate=True")
                .Options;

            return new NegocioDbContext(opciones);
        }
    }
}
