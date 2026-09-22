using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Simulab.ArchitectureTests.DocGen.SharedTables;
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

    // B-12: an owned type mapped with ToJson is a column of its owner's table, not a table of its own.
    [Fact]
    public void RenderDictionary_ListsJsonColumnsInTheOwnerTableOnce()
    {
        var (module, model) = RealModels().Single(m => m.Module == "Identity");

        var text = EntityModels.RenderDictionary(module, model);

        Occurrences(text, "\n## role_changes\n").Should().Be(1);
        text.Should().Contain("| added | jsonb | yes |  |  | JSON: RoleChangeItem (Key, Name) |\n")
            .And.Contain("| removed | jsonb | yes |  |  | JSON: RoleChangeItem (Key, Name) |\n")
            .And.NotContain("__synthesizedOrdinal")
            .And.NotContain("Entity: `RoleChangeItem`");
    }

    [Fact]
    public void RenderEntities_DrawsJsonColumnsInTheOwnerBoxWithoutSelfRelation()
    {
        var (module, model) = RealModels().Single(m => m.Module == "Identity");

        var text = EntityModels.RenderEntities(module, model);

        Occurrences(text, "    role_changes {\n").Should().Be(1);
        text.Should().Contain("        jsonb added\n")
            .And.Contain("        jsonb removed\n")
            .And.NotContain("role_changes ||--}o role_changes");
    }

    [Fact]
    public void Render_ListsEachTableOnce()
    {
        foreach (var (module, model) in RealModels())
        {
            var dictionary = EntityModels.RenderDictionary(module, model);
            var entities = EntityModels.RenderEntities(module, model);
            foreach (var table in model.GetEntityTypes().Select(e => e.GetTableName()).OfType<string>().Distinct())
            {
                Occurrences(dictionary, $"\n## {table}\n").Should().Be(1, $"{module}.{table} is one table");
                Occurrences(entities, $"    {table} {{\n").Should().Be(1, $"{module}.{table} is one table");
            }
        }
    }

    // F-25: types sharing a table, over a test-only model (no module maps one yet).
    internal static (string Module, IModel Model) SharedTableModel() =>
        EntityModels.Load([typeof(SharedTableDbContext), typeof(SharedTableDbContextFactory)]).Single();

    [Fact]
    public void RenderDictionary_ListsOwnedColumnsInTheOwnerTableOnce()
    {
        var (module, model) = SharedTableModel();

        var text = EntityModels.RenderDictionary(module, model);

        Occurrences(text, "\n## customers\n").Should().Be(1);
        text.Should().Contain("| address_street | character varying(200) | yes |  |  | Owned: Address; max 200 |\n",
                "the address is optional, so its columns are nullable even when the property is required")
            .And.Contain("| address_city | text | yes |  |  | Owned: Address |\n")
            .And.NotContain("Entity: `Address`");
    }

    [Fact]
    public void RenderDictionary_ListsOwnedColumnsAfterTheOwnerColumns()
    {
        var (module, model) = SharedTableModel();

        var section = Section(EntityModels.RenderDictionary(module, model), "customers");

        Rows(section).Should().Equal("id", "name", "address_city", "address_street");
    }

    [Fact]
    public void Render_ListsComplexColumnsInTheOwnerTable()
    {
        var (module, model) = SharedTableModel();

        var dictionary = EntityModels.RenderDictionary(module, model);
        var entities = EntityModels.RenderEntities(module, model);

        Rows(Section(dictionary, "products")).Should().Equal("id", "name", "price_amount", "price_currency", "dimensions");
        dictionary.Should().Contain("| price_amount | numeric | no |  |  | Complex: Money |\n")
            .And.Contain("| price_currency | text | no |  |  | Complex: Money |\n");
        entities.Should().Contain("        numeric price_amount\n").And.Contain("        text price_currency\n");
    }

    [Fact]
    public void RenderDictionary_ListsComplexJsonAsOneColumn()
    {
        var (module, model) = SharedTableModel();

        var text = EntityModels.RenderDictionary(module, model);

        text.Should().Contain("| dimensions | jsonb | no |  |  | JSON: Dimensions (Height, Width) |\n")
            .And.NotContain("| height |")
            .And.NotContain("| width |");
    }

    [Fact]
    public void RenderDictionary_ListsEntitiesSharingATableInOneSection()
    {
        var (module, model) = SharedTableModel();

        var text = EntityModels.RenderDictionary(module, model);

        Occurrences(text, "\n## orders\n").Should().Be(1);
        var section = Section(text, "orders");
        section.Should().Contain("Entities: `Order`, `OrderSummary`\n")
            .And.Contain("| id | uuid | no | PK |  |  |\n")
            .And.Contain("| total | numeric | no |  |  | Entity: OrderSummary |\n");
        Rows(section).Should().Equal("id", "placed_at", "total");
    }

    [Fact]
    public void RenderEntities_DrawsOneBoxPerTableWithoutSplittingSelfRelations()
    {
        var (module, model) = SharedTableModel();

        var text = EntityModels.RenderEntities(module, model);

        foreach (var table in new[] { "categories", "customers", "orders", "products" })
        {
            Occurrences(text, $"    {table} {{\n").Should().Be(1, $"{table} is one table");
        }

        text.Should().Contain("        text address_city\n")
            .And.NotContain("customers ||--")
            .And.NotContain("orders ||--");
    }

    [Fact]
    public void RenderEntities_KeepsARealSelfReference()
    {
        var (module, model) = SharedTableModel();

        EntityModels.RenderEntities(module, model)
            .Should().Contain("    categories ||--}o categories : \"fk_categories_categories_parent_id\"\n");
    }

    [Fact]
    public void RenderDictionary_ListsAnOwnedTypeIndexInTheOwnerTableOnce()
    {
        var (module, model) = SharedTableModel();

        var section = Section(EntityModels.RenderDictionary(module, model), "customers");

        Occurrences(section, "- `ix_customers_address_city` on address_city\n").Should().Be(1);
    }

    private static string Section(string dictionary, string table)
    {
        var start = dictionary.IndexOf($"\n## {table}\n", StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"the dictionary has a {table} section");
        var end = dictionary.IndexOf("\n## ", start + 1, StringComparison.Ordinal);
        return end < 0 ? dictionary[start..] : dictionary[start..end];
    }

    private static List<string> Rows(string section) =>
    [
        .. section.Split('\n')
            .Where(line => line.StartsWith("| ", StringComparison.Ordinal) && !line.StartsWith("| Column |", StringComparison.Ordinal))
            .Select(line => line.Split('|')[1].Trim())
    ];

    private static int Occurrences(string text, string value) =>
        (text.Length - text.Replace(value, "", StringComparison.Ordinal).Length) / value.Length;
}
