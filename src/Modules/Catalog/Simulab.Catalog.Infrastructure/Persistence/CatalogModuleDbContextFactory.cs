using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Simulab.Persistence;
using Simulab.SharedKernel.Security;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c> only, so migrations can be created without starting the host. The
/// connection string is never used to connect: the tool reads the model, not the database.
/// </summary>
public sealed class CatalogModuleDbContextFactory : IDesignTimeDbContextFactory<CatalogModuleDbContext>
{
    public CatalogModuleDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<CatalogModuleDbContext>()
                .UseNpgsql("Host=localhost;Database=simulab;Username=postgres", npgsql =>
                    npgsql.UseModuleHistoryTable(CatalogModuleDbContext.SchemaName))
                .UseSnakeCaseNamingConvention()
                .Options,
            new NoTenant());
}
