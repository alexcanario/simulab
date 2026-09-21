using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Simulab.DocGen;
using Simulab.Identity.Infrastructure.Persistence;
using Simulab.Jobs.Persistence;

namespace Simulab.ArchitectureTests.DocGen;

/// <summary>F-15: entity diagrams and data dictionaries from the real EF models (AC2, AC3).</summary>
public class EntityModelsTests
{
    internal static IReadOnlyList<(string Module, IModel Model)> RealModels() =>
    [
        .. EntityModels.Load(
        [
            .. typeof(IdentityModuleDbContext).Assembly.GetTypes(),
            .. typeof(JobsDbContext).Assembly.GetTypes()
        ])
    ];

    [Theory]
    [InlineData("IdentityModuleDbContext", "Identity")]
    [InlineData("JobsDbContext", "Jobs")]
    [InlineData("CatalogContext", "Catalog")]
    public void ModuleName_StripsTheContextSuffixes(string typeName, string expected) =>
        EntityModels.ModuleName(typeName).Should().Be(expected);

    [Fact]
    public void Load_NamesTheRealModulesIdentityAndJobs() =>
        RealModels().Select(m => m.Module).Should().Equal("Identity", "Jobs");

    [Fact]
    public void RenderDictionary_ListsEveryIdentityTableInSnakeCase()
    {
        var (module, model) = RealModels().Single(m => m.Module == "Identity");

        var text = EntityModels.RenderDictionary(module, model);

        text.Should().Contain("Schema: `identity`");
        var tables = model.GetEntityTypes().Select(e => e.GetTableName()).OfType<string>().Distinct().ToList();
        tables.Should().Contain("users");
        foreach (var table in tables)
        {
            text.Should().Contain($"\n## {table}\n");
        }

        text.Should().Contain("| normalized_email |", "columns follow the snake_case convention of the migrations");
        text.Should().NotContain("| NormalizedEmail |");
    }

    [Fact]
    public void RenderDictionary_ShowsNullsNotDistinctOnTenantUniqueIndexes()
    {
        var (module, model) = RealModels().Single(m => m.Module == "Identity");

        EntityModels.RenderDictionary(module, model)
            .Should().Contain("- `ux_users_tenant_normalized_email` on tenant_id, normalized_email (unique, NULLS NOT DISTINCT)");
    }

    [Fact]
    public void RenderEntities_DrawsEveryTableOfTheModule()
    {
        var (module, model) = RealModels().Single(m => m.Module == "Jobs");

        var text = EntityModels.RenderEntities(module, model);

        text.Should().StartWith("# Jobs — entities").And.Contain("```mermaid\nerDiagram\n").And.Contain("    jobs {\n").And.Contain("uuid id PK");
    }
}
