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

        text.Should().StartWith("# Jobs — entities").And.Contain("    jobs {\n").And.Contain("uuid id PK");
    }

    // F-26 AC1, AC2: the ELK front matter opens every block, and the page says what a viewer without ELK draws.
    [Fact]
    public void RenderEntities_OpensEveryBlockWithTheElkLayoutUnderTheViewerNote()
    {
        foreach (var (module, model) in RealModels())
        {
            var text = EntityModels.RenderEntities(module, model);

            Occurrences(text, "```mermaid\n---\nconfig:\n  layout: elk\n---\nerDiagram\n").Should().Be(1, $"{module} has one diagram");
            Occurrences(text, "```mermaid").Should().Be(1, $"{module} has one diagram");
            text.Should().StartWith($"# {module} — entities\n\nGenerated from the EF model. Do not edit.\n\n{EntityModels.ElkNote}\n\n```mermaid\n");
        }
    }

    // F-26 AC3: the tenant, audit and soft-delete columns are one line per table, the dictionary keeps them.
    [Fact]
    public void RenderEntities_FoldsTheStandardColumnsIntoOneLine()
    {
        var (module, model) = RealModels().Single(m => m.Module == "Identity");

        var text = EntityModels.RenderEntities(module, model);

        foreach (var column in new[] { "tenant_id", "created_at", "created_by", "updated_at", "updated_by", "is_deleted", "deleted_at", "deleted_by" })
        {
            text.Should().NotMatchRegex($"(?m)^        \\S+ {column}( |$)", $"{column} is folded");
        }

        const string folded = "        standard columns \"8: tenant, audit, soft delete - see data dictionary\"\n    }\n";
        Box(text, "users").Should().EndWith(folded);
        Box(text, "consent_records").Should().EndWith(folded);
        Occurrences(Box(text, "users"), "standard columns").Should().Be(1);
    }

    // F-26 AC4: a table without standard columns has no folded line.
    [Fact]
    public void RenderEntities_AddsNoFoldedLineToATableWithoutStandardColumns()
    {
        var (module, model) = RealModels().Single(m => m.Module == "Identity");

        Box(EntityModels.RenderEntities(module, model), "openiddict_applications").Should().NotContain("standard columns");
    }

    // F-26 AC5: short PostgreSQL type names with their lengths.
    [Fact]
    public void RenderEntities_ShowsShortTypeNames()
    {
        var (module, model) = RealModels().Single(m => m.Module == "Identity");

        var text = EntityModels.RenderEntities(module, model);

        text.Should().Contain("        varchar(45) ip_address\n")
            .And.Contain("        timestamptz accepted_at\n")
            .And.NotContain("character_varying")
            .And.NotContain("timestamp_with_time_zone");
    }

    [Theory]
    [InlineData("character varying(45)", "varchar(45)")]
    [InlineData("character varying", "varchar")]
    [InlineData("character(2)", "char(2)")]
    [InlineData("timestamp with time zone", "timestamptz")]
    [InlineData("timestamp(3) with time zone", "timestamptz(3)")]
    [InlineData("timestamp without time zone", "timestamp")]
    [InlineData("time with time zone", "timetz")]
    [InlineData("time without time zone", "time")]
    [InlineData("double precision", "float8")]
    [InlineData("bit varying(8)", "varbit(8)")]
    [InlineData("numeric(10,2)", "numeric(10,2)")]
    [InlineData("text[]", "text[]")]
    [InlineData("uuid", "uuid")]
    [InlineData("some type", "some_type")]
    public void TypeId_ShortensPostgreSqlTypeNames(string storeType, string expected) =>
        EntityModels.TypeId(storeType).Should().Be(expected);

    // F-26 AC6: relations are labelled with the dependent's key columns, not the constraint name.
    [Fact]
    public void RenderEntities_LabelsRelationsWithTheForeignKeyColumns()
    {
        var (module, model) = RealModels().Single(m => m.Module == "Identity");

        var text = EntityModels.RenderEntities(module, model);

        Occurrences(text, "    openiddict_applications ||--}o openiddict_tokens : \"application_id\"\n").Should().Be(1);
        text.Should().NotContain(": \"fk_");
    }

    // F-26 AC7: the data dictionary keeps every column with its full type.
    [Fact]
    public void RenderDictionary_KeepsTheStandardColumnsWithTheFullType()
    {
        var (module, model) = RealModels().Single(m => m.Module == "Identity");

        var section = Section(EntityModels.RenderDictionary(module, model), "users");

        section.Should().Contain("| created_at | timestamp with time zone |");
        Rows(section).Should().Contain(["tenant_id", "created_at", "created_by", "updated_at", "updated_by", "is_deleted", "deleted_at", "deleted_by"]);
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
            .Should().Contain("    categories ||--}o categories : \"parent_id\"\n");
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

    // One table's box in the diagram, from its opening line to its closing brace.
    private static string Box(string entities, string table)
    {
        var start = entities.IndexOf($"    {table} {{\n", StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"the diagram has a {table} box");
        var end = entities.IndexOf("\n    }\n", start, StringComparison.Ordinal);
        return entities[start..(end + "\n    }\n".Length)];
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
