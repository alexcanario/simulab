using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Simulab.Persistence;
using Simulab.SharedKernel.Security;

namespace Simulab.QuestionBank.Infrastructure.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c> only, so migrations can be created without starting the host. The
/// connection string is never used to connect: the tool reads the model, not the database.
/// </summary>
public sealed class QuestionBankModuleDbContextFactory : IDesignTimeDbContextFactory<QuestionBankModuleDbContext>
{
    public QuestionBankModuleDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<QuestionBankModuleDbContext>()
                .UseNpgsql("Host=localhost;Database=simulab;Username=postgres", npgsql =>
                    npgsql.UseModuleHistoryTable(QuestionBankModuleDbContext.SchemaName))
                .UseSnakeCaseNamingConvention()
                .Options,
            new NoTenant());
}
