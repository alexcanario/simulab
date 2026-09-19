using System.Text.RegularExpressions;

namespace Simulab.ArchitectureTests;

/// <summary>
/// B-7 BR4: a field's length limit is written once, in <c>AccountLimits</c> (contracts), and every screen reads it.
/// A number typed into a page drifts from the column and the Api check. Razor markup is not visible through
/// reflection, so the rule reads the source files.
/// </summary>
public partial class FieldLengthTests
{
    [GeneratedRegex("""MaxLength="(?<value>[^"]+)""")]
    private static partial Regex MaxLength();

    [Fact]
    public void Web_Pages_ReadLengthLimitsFromTheContracts()
    {
        var webRoot = Path.Combine(SolutionAssemblies.RepositoryRoot(), "src", "Hosts", "Simulab.Web", "Components", "Pages");

        var limits = Directory.EnumerateFiles(webRoot, "*.razor", SearchOption.AllDirectories)
            .SelectMany(path => MaxLength().Matches(File.ReadAllText(path))
                .Select(match => (File: Path.GetRelativePath(webRoot, path), Value: match.Groups["value"].Value)))
            .ToList();

        // Rule of presence: the pages do set limits, so an empty match is a broken scan, not a pass.
        limits.Should().NotBeEmpty();
        limits.Where(limit => int.TryParse(limit.Value, out _))
            .Select(limit => $"{limit.File}: MaxLength=\"{limit.Value}\"")
            .Should().BeEmpty("a length limit is read from AccountLimits, never typed as a number");
    }
}
