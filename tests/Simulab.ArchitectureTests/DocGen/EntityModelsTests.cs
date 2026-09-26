using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Simulab.ArchitectureTests.DocGen.SharedTables;
using Simulab.DocGen;

namespace Simulab.ArchitectureTests.DocGen;

/// <summary>F-15: entity diagrams and data dictionaries from the real EF models (AC2, AC3).</summary>
public class EntityModelsTests
{
    /// <summary>
    /// F-45: the models DocGen sees, derived from the closure of its own project references — never a list by
    /// hand. F-24 found the hand list four weeks behind the generator (<c>AiDbContext</c> documented and loaded
    /// by no test); <see cref="DocGenModelDriftTests"/> compares this with what the generator really wrote.
    /// </summary>
    internal static IReadOnlyList<(string Module, IModel Model)> RealModels() =>
        [.. EntityModels.Load([.. DocGenAssemblies().SelectMany(assembly => assembly.GetTypes())])];

    /// <summary>The projects DocGen references, directly or through another project.</summary>
    internal static IReadOnlyList<string> DocGenProjects() =>
        DocGenProjectReferences.Closure(
            Path.Combine(SolutionAssemblies.RepositoryRoot(), "tools", "Simulab.DocGen", "Simulab.DocGen.csproj"));

    /// <summary>
    /// The loaded assemblies of those projects. A project the tests cannot resolve stops them with the file to
    /// edit: skipping it silently is how a documented module goes untested (BR3).
    /// </summary>
    internal static IReadOnlyList<Assembly> DocGenAssemblies()
    {
        var projects = DocGenProjects();
        var unresolved = DocGenProjectReferences.Unresolved(projects, SolutionAssemblies.All.Select(a => a.GetName().Name!));
        if (unresolved.Count > 0)
        {
            throw new InvalidOperationException(DocGenProjectReferences.UnresolvedMessage(unresolved));
        }

        return [.. SolutionAssemblies.All.Where(assembly => projects.Contains(assembly.GetName().Name!))];
    }

    [Theory]
    [InlineData("IdentityModuleDbContext", "Identity")]
    [InlineData("JobsDbContext", "Jobs")]
    [InlineData("CatalogContext", "Catalog")]
    public void ModuleName_StripsTheContextSuffixes(string typeName, string expected) =>
        EntityModels.ModuleName(typeName).Should().Be(expected);

    [Fact]
    public void Load_NamesEveryRealModule() =>
        RealModels().Select(m => m.Module).Should().Equal("Ai", "Catalog", "Identity", "Jobs");

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
    public void RenderSchema_ListsEveryTableOfTheModule()
    {
        var (module, model) = RealModels().Single(m => m.Module == "Jobs");

        var text = EntityModels.RenderSchema(module, model);

        // Since F-24 every column carries its description, so the brackets no longer close after the key.
        text.Should().Contain("Table jobs {\n").And.Contain("  id uuid [pk, note: '");
    }

    // F-26 AC2: the schema says it is generated and which viewer draws it.
    [Fact]
    public void RenderSchema_StartsWithTheGeneratedCommentAndTheViewer()
    {
        foreach (var (module, model) in RealModels())
        {
            EntityModels.RenderSchema(module, model).Should().StartWith(
                $"// {module} — schema. Generated from the EF model. Do not edit.\n// To see the diagram, {EntityModels.SchemaViewerHint}.\n");
        }
    }

    // F-26 AC3: the tenant, audit and soft-delete columns are counted in the table note, the dictionary keeps them.
    [Fact]
    public void RenderSchema_CountsTheStandardColumnsInTheTableNote()
    {
        var (module, model) = RealModels().Single(m => m.Module == "Identity");

        var text = EntityModels.RenderSchema(module, model);

        foreach (var column in new[] { "tenant_id", "created_at", "created_by", "updated_at", "updated_by", "is_deleted", "deleted_at", "deleted_by" })
        {
            text.Should().NotMatchRegex($"(?m)^  {column} ", $"{column} is left out of every table");
        }

        Block(text, "Table users").Should().EndWith("  Note: 'Entity: User - standard columns: 8 (see data dictionary)'\n}\n");
        Block(text, "Table consent_records").Should().EndWith("  Note: 'Entity: ConsentRecord - standard columns: 8 (see data dictionary)'\n}\n");
    }

    // F-26 AC4: a table without standard columns says nothing about them.
    [Fact]
    public void RenderSchema_LeavesTheStandardCountOutOfATableWithoutThem()
    {
        var (module, model) = RealModels().Single(m => m.Module == "Identity");

        Block(EntityModels.RenderSchema(module, model), "Table openiddict_applications").Should().NotContain("standard columns");
    }

    // F-26 AC5: short PostgreSQL type names with their lengths, not null, and a composite key.
    [Fact]
    public void RenderSchema_ShowsShortTypesAndKeys()
    {
        var (module, model) = RealModels().Single(m => m.Module == "Identity");

        var text = EntityModels.RenderSchema(module, model);

        // Since F-24 the description follows the type inside the brackets; the short type name is what
        // this checks, and the NotContain pair below is what proves it is short.
        text.Should().Contain("  ip_address varchar(45) [note: '")
            .And.Contain("  accepted_at timestamptz [not null, note: '")
            .And.Contain("  role_ids \"uuid[]\" [not null, note: '")
            .And.NotContain("character varying")
            .And.NotContain("with time zone");
        Block(text, "Table role_permissions").Should().Contain("    (role_id, permission_name) [pk]\n", "the key columns in the key's order");
    }

    // F-26 AC11: tables linked by foreign keys are grouped; an index never names a column left out.
    [Fact]
    public void RenderSchema_GroupsLinkedTablesAndListsOnlyIndexesOverShownColumns()
    {
        var (module, model) = RealModels().Single(m => m.Module == "Identity");

        var text = EntityModels.RenderSchema(module, model);

        Occurrences(text, "TableGroup ").Should().Be(2);
        Block(text, "TableGroup users_group").Split('\n').Count(l => l.StartsWith("  ", StringComparison.Ordinal)).Should().Be(12);
        Block(text, "TableGroup openiddict_applications_group").Should().Be(
            "TableGroup openiddict_applications_group {\n  openiddict_applications\n  openiddict_authorizations\n  openiddict_tokens\n}\n");
        Block(text, "Table users").Should().NotContain("ux_users_tenant_normalized_email", "the index covers tenant_id, which is left out");
        Block(text, "Table roles").Should().Contain("    normalized_name [name: 'ux_roles_normalized_name', unique]\n");
        Block(text, "Table user_claims").Should().EndWith("  Note: 'Entity: IdentityUserClaim'\n}\n", "a generic type is named without its arity");
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
    [InlineData("text[]", "\"text[]\"")]
    [InlineData("uuid", "uuid")]
    [InlineData("some type", "\"some type\"")]
    public void SchemaType_ShortensPostgreSqlTypeNames(string storeType, string expected) =>
        EntityModels.SchemaType(storeType).Should().Be(expected);

    // F-26 AC6: one Ref per foreign key, from the dependent's columns to the principal's key.
    [Fact]
    public void RenderSchema_WritesOneRefPerForeignKey()
    {
        var (module, model) = RealModels().Single(m => m.Module == "Identity");

        var text = EntityModels.RenderSchema(module, model);

        Occurrences(text, "Ref: openiddict_tokens.application_id > openiddict_applications.id\n").Should().Be(1);
        Occurrences(text, "Ref: role_permissions.permission_name > permissions.name\n").Should().Be(1);
        Occurrences(text, "Ref: ").Should().Be(14);
        text.Should().NotContain("fk_");
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
    public void RenderSchema_ListsJsonColumnsInTheOwnerTableWithoutSelfRef()
    {
        var (module, model) = RealModels().Single(m => m.Module == "Identity");

        var text = EntityModels.RenderSchema(module, model);

        Occurrences(text, "Table role_changes {\n").Should().Be(1);
        text.Should().Contain("  added jsonb [note: 'JSON: RoleChangeItem (Key, Name)']\n")
            .And.Contain("  removed jsonb [note: 'JSON: RoleChangeItem (Key, Name)']\n")
            .And.NotContain("Ref: role_changes");
    }

    [Fact]
    public void Render_ListsEachTableOnce()
    {
        foreach (var (module, model) in RealModels())
        {
            var dictionary = EntityModels.RenderDictionary(module, model);
            var schema = EntityModels.RenderSchema(module, model);
            foreach (var table in model.GetEntityTypes().Select(e => e.GetTableName()).OfType<string>().Distinct())
            {
                Occurrences(dictionary, $"\n## {table}\n").Should().Be(1, $"{module}.{table} is one table");
                Occurrences(schema, $"Table {table} {{\n").Should().Be(1, $"{module}.{table} is one table");
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
        var schema = EntityModels.RenderSchema(module, model);

        Rows(Section(dictionary, "products")).Should().Equal("id", "name", "price_amount", "price_currency", "dimensions");
        dictionary.Should().Contain("| price_amount | numeric | no |  |  | Complex: Money |\n")
            .And.Contain("| price_currency | text | no |  |  | Complex: Money |\n");
        schema.Should().Contain("  price_amount numeric [not null, note: 'Complex: Money']\n")
            .And.Contain("  price_currency text [not null, note: 'Complex: Money']\n");
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
    public void RenderSchema_WritesOneTablePerTableWithoutSplittingSelfRefs()
    {
        var (module, model) = SharedTableModel();

        var text = EntityModels.RenderSchema(module, model);

        foreach (var table in new[] { "categories", "customers", "orders", "products" })
        {
            Occurrences(text, $"Table {table} {{\n").Should().Be(1, $"{table} is one table");
        }

        text.Should().Contain("  address_city text [note: 'Owned: Address']\n")
            .And.Contain("  Note: 'Entities: Order, OrderSummary'\n")
            .And.NotContain("Ref: customers.")
            .And.NotContain("Ref: orders.");
    }

    [Fact]
    public void RenderSchema_KeepsARealSelfReference()
    {
        var (module, model) = SharedTableModel();

        EntityModels.RenderSchema(module, model)
            .Should().Contain("Ref: categories.parent_id > categories.id\n");
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

    // One top-level block of the schema (`Table users`, `TableGroup users_group`), from its opening line to its closing brace.
    private static string Block(string schema, string head)
    {
        var start = schema.IndexOf($"{head} {{\n", StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"the schema has {head}");
        var end = schema.IndexOf("\n}\n", start, StringComparison.Ordinal);
        return schema[start..(end + "\n}\n".Length)];
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
