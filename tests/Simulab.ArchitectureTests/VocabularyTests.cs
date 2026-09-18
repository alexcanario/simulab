using System.Reflection;
using System.Text.RegularExpressions;

namespace Simulab.ArchitectureTests;

/// <summary>
/// Identifiers are English in every assembly. The forbidden terms come from docs/glossary.md,
/// so the glossary stays the single source of truth.
/// </summary>
public partial class VocabularyTests
{
    [GeneratedRegex("`([^`]+)`")]
    private static partial Regex Backticked();

    [GeneratedRegex("[A-Z][a-z0-9]*")]
    private static partial Regex PascalWord();

    private static HashSet<string> ForbiddenTerms()
    {
        var glossary = File.ReadAllLines(Path.Combine(SolutionAssemblies.RepositoryRoot(), "docs", "glossary.md"));
        var start = Array.FindIndex(glossary, line => line.StartsWith("## Forbidden terms", StringComparison.Ordinal));
        start.Should().BeGreaterThanOrEqualTo(0, "docs/glossary.md must keep its 'Forbidden terms in identifiers' section");

        return glossary.Skip(start + 1)
            .TakeWhile(line => !line.StartsWith("## ", StringComparison.Ordinal))
            .SelectMany(line => Backticked().Matches(line).Select(match => match.Groups[1].Value))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static IEnumerable<string> Identifiers(Assembly assembly)
    {
        const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        foreach (var type in assembly.GetTypes().Where(t => t.Namespace?.StartsWith("Simulab", StringComparison.Ordinal) == true))
        {
            yield return type.Name;
            foreach (var member in type.GetMembers(Declared))
            {
                yield return member.Name;
            }
        }
    }

    [Fact]
    public void The_glossary_lists_forbidden_terms()
    {
        ForbiddenTerms().Should().Contain(["Banca", "Questao", "Usuario"]);
    }

    [Fact]
    public void No_identifier_uses_a_forbidden_portuguese_term()
    {
        var forbidden = ForbiddenTerms();
        var identifiers = SolutionAssemblies.All.SelectMany(Identifiers).Distinct().ToList();

        identifiers.Should().Contain("TenantEntity", "the rule must have looked at real identifiers");

        var offenders = identifiers
            .Where(name => PascalWord().Matches(name).Any(word => forbidden.Contains(word.Value)))
            .ToList();
        offenders.Should().BeEmpty();
    }
}
