using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Simulab.ArchitectureTests.DocGen;

namespace Simulab.ArchitectureTests;

/// <summary>
/// F-24, BR7: a table Simulab defines explains itself, and so does every column of it that EF maps to a
/// property. A column with no property behind it — the container of an <c>OwnsMany(...).ToJson(...)</c> —
/// is exempt, because <c>HasComment</c> has nothing to attach to (BR1, v2).
/// </summary>
public class TableDescriptionTests
{
    private sealed record ColumnRef(string Module, string Table, string Column);

    /// <summary>
    /// Whose table it is. The entity's own CLR type decides, not the type that declared the property:
    /// <c>users</c> is Simulab's even though twelve of its columns come from <c>IdentityUser</c>, and
    /// <c>user_claims</c> is not, because <c>IdentityUserClaim</c> is Microsoft's (BR2).
    /// </summary>
    private static bool IsOurs(ITable table) =>
        table.EntityTypeMappings.Any(mapping =>
            SolutionAssemblies.All.Contains(mapping.TypeBase.ClrType.Assembly));

    private static IEnumerable<(string Module, ITable Table)> Tables() =>
        EntityModelsTests.RealModels()
            .SelectMany(entry => entry.Model.GetRelationalModel().Tables.Select(table => (entry.Module, Table: table)));

    [Fact]
    public void EveryTableWeDefine_HasADescription()
    {
        var missing = Tables()
            .Where(entry => IsOurs(entry.Table))
            .Where(entry => string.IsNullOrWhiteSpace(entry.Table.Comment))
            .Select(entry => $"{entry.Module}.{entry.Table.Name}")
            .ToList();

        Ours().Should().NotBeEmpty("the rule must have looked at our tables");
        missing.Should().BeEmpty(string.Join(", ", missing));
    }

    [Fact]
    public void EveryColumnWeDefine_HasADescription()
    {
        var missing = Ours()
            .Where(column => column.Property is not null)
            .Where(column => string.IsNullOrWhiteSpace(column.Property!.GetComment()))
            .Select(column => $"{column.Reference.Module}.{column.Reference.Table}.{column.Reference.Column}")
            .ToList();

        Ours().Should().NotBeEmpty("the rule must have looked at our columns");
        missing.Should().BeEmpty(
            $"every column of a table we define is described; a shadow column needs builder.Property<T>(\"Name\").HasComment(...). Missing: {string.Join(", ", missing)}");
    }

    /// <summary>AC1b: the exemption is real and narrow — the JSON containers of <c>role_changes</c>.</summary>
    [Fact]
    public void OnlyColumnsWithNoPropertyBehindThem_AreExempt()
    {
        var exempt = Ours()
            .Where(column => column.Property is null)
            .Select(column => $"{column.Reference.Table}.{column.Reference.Column}")
            .ToList();

        exempt.Should().NotBeEmpty("the exemption must cover something, or it hides a gap instead of naming one");
        exempt.Should().BeEquivalentTo(["role_changes.added", "role_changes.removed"]);
    }

    /// <summary>AC6: the tables we do not define are skipped, and the skipped set is not empty.</summary>
    [Fact]
    public void TablesWeDoNotDefine_AreSkipped()
    {
        var skipped = Tables()
            .Where(entry => !IsOurs(entry.Table))
            .Select(entry => entry.Table.Name)
            .ToList();

        skipped.Should().NotBeEmpty("the rule must have skipped something, or its filter matches everything");
        skipped.Should().Contain("user_claims").And.Contain("openiddict_applications");
        skipped.Should().NotContain("users", "the table is ours even though ASP.NET Identity declared some of its columns");
    }

    /// <summary>AC1c: a column ASP.NET Identity declared, on a table of ours, is described like any other.</summary>
    [Fact]
    public void ColumnsDeclaredByAspNetIdentity_OnOurTables_AreDescribed()
    {
        var framework = Ours()
            .Where(column => column.Reference.Table == "users")
            // The CLR property's declaring type, not the entity type: User is ours, IdentityUser is not.
            .Where(column => column.Property?.PropertyInfo?.DeclaringType?.Assembly is { } assembly
                && !SolutionAssemblies.All.Contains(assembly))
            .ToList();

        framework.Should().NotBeEmpty("users carries columns ASP.NET Identity declared");
        framework.Select(column => column.Reference.Column).Should().Contain("password_hash").And.Contain("security_stamp");
        framework.Should().OnlyContain(column => !string.IsNullOrWhiteSpace(column.Property!.GetComment()));
    }

    private static IReadOnlyList<(ColumnRef Reference, IProperty? Property)> Ours() =>
        [.. Tables()
            .Where(entry => IsOurs(entry.Table))
            .SelectMany(entry => entry.Table.Columns.Select(column => (
                Reference: new ColumnRef(entry.Module, entry.Table.Name, column.Name),
                Property: column.PropertyMappings
                    .Select(mapping => mapping.Property)
                    .FirstOrDefault(property => property.DeclaringType.IsMappedToJson() == false))))];
}
