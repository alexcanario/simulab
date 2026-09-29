using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Simulab.ArchitectureTests.DocGen;

namespace Simulab.ArchitectureTests;

/// <summary>
/// B-20, BR1: a comment on a column that stores an enum as text may name that enum's values, and every value
/// it names must exist. A full list ("A, B or C") must name all of them; a list introduced by "such as" may
/// name only some.
/// </summary>
public partial class EnumColumnCommentTests
{
    private sealed record EnumColumn(string Module, string Table, string Column, Type EnumType, string Comment);

    [GeneratedRegex("[A-Z][A-Za-z0-9]+")]
    private static partial Regex PascalWord();

    private static IEnumerable<EnumColumn> EnumColumns() =>
        EntityModelsTests.RealModels()
            .SelectMany(entry => entry.Model.GetEntityTypes()
                .Where(entityType => entityType.GetTableName() is not null)
                .SelectMany(entityType => entityType.GetDeclaredProperties()
                    .Where(property => property.GetProviderClrType() == typeof(string))
                    .Where(property => (Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType).IsEnum)
                    .Where(property => !string.IsNullOrWhiteSpace(property.GetComment()))
                    .Select(property => new EnumColumn(
                        entry.Module,
                        entityType.GetTableName()!,
                        property.GetColumnName(),
                        Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType,
                        property.GetComment()!))));

    /// <summary>The words of the value list: after "such as" when there is one, else after the first colon.</summary>
    private static (bool Partial, IReadOnlyList<string> Words)? ValueList(string comment)
    {
        var suchAs = comment.IndexOf("such as", StringComparison.Ordinal);
        var colon = comment.IndexOf(':', StringComparison.Ordinal);
        var start = suchAs >= 0 ? suchAs + "such as".Length : colon >= 0 ? colon + 1 : -1;
        if (start < 0)
        {
            return null;
        }

        var end = comment.IndexOf('.', start);
        var list = comment[start..(end >= 0 ? end : comment.Length)];
        return (suchAs >= 0, [.. PascalWord().Matches(list).Select(match => match.Value)]);
    }

    [Fact]
    public void EnumColumnComments_NameOnlyValuesTheEnumHas()
    {
        var columns = EnumColumns().ToList();
        columns.Should().NotBeEmpty("the rule must have looked at enum columns");

        var problems = new List<string>();
        foreach (var column in columns)
        {
            if (ValueList(column.Comment) is not var (isPartial, words))
            {
                continue;
            }

            var names = Enum.GetNames(column.EnumType);
            var unknown = words.Where(word => !names.Contains(word)).ToList();
            if (unknown.Count > 0)
            {
                problems.Add($"{column.Module}.{column.Table}.{column.Column} names {string.Join(", ", unknown)}, not values of {column.EnumType.Name}");
            }
            else if (!isPartial && names.Except(words).Any())
            {
                problems.Add($"{column.Module}.{column.Table}.{column.Column} lists {column.EnumType.Name} without {string.Join(", ", names.Except(words))}");
            }
        }

        problems.Should().BeEmpty(string.Join("; ", problems));
    }

    [Fact]
    public void ScopeColumn_IsAmongTheOnesChecked()
    {
        EnumColumns().Select(column => $"{column.Table}.{column.Column}")
            .Should().Contain(["exams.scope", "users.status", "role_changes.action", "account_events.reason"]);
    }
}
