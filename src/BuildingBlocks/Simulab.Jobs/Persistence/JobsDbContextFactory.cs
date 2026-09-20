using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Simulab.Persistence;
using Simulab.SharedKernel.Security;

namespace Simulab.Jobs.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c> only, so the queue's migration can be created without starting the host.
/// The connection string is never used to connect: the tool reads the model, not the database.
/// </summary>
public sealed class JobsDbContextFactory : IDesignTimeDbContextFactory<JobsDbContext>
{
    public JobsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<JobsDbContext>()
                .UseNpgsql("Host=localhost;Database=simulab;Username=postgres", npgsql =>
                    npgsql.UseModuleHistoryTable(JobsDbContext.SchemaName))
                .UseSnakeCaseNamingConvention()
                .Options,
            new NoTenant());
}
