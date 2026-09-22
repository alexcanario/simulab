using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Simulab.ArchitectureTests.DocGen.SharedTables;

/// <summary>Builds the test model with the same naming convention as the real modules; it never connects.</summary>
public sealed class SharedTableDbContextFactory : IDesignTimeDbContextFactory<SharedTableDbContext>
{
    public SharedTableDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<SharedTableDbContext>()
            .UseNpgsql("Host=localhost;Database=docgen;Username=docgen")
            .UseSnakeCaseNamingConvention()
            .Options);
}
