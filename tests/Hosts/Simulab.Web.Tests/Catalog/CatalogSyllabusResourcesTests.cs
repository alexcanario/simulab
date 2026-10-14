using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-76 AC10: every text of the student's syllabus exists in en, pt-BR and pt-PT and keeps its placeholders in all
/// three. ResourceParityTests checks that no key is missing in a language; this pins what the syllabus needs.
/// </summary>
public sealed partial class CatalogSyllabusResourcesTests
{
    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    private static readonly string[] Cultures = ["en", "pt-BR", "pt-PT"];

    public static TheoryData<string> Keys() => new(
    [
        "Catalog.Syllabus.Show.One", "Catalog.Syllabus.Show.Many", "Catalog.Syllabus.Hide",
        "Catalog.Syllabus.NotRegistered", "Catalog.Syllabus.NotMapped"
    ]);

    [Theory]
    [MemberData(nameof(Keys))]
    public void EveryNewText_ExistsInTheThreeLanguages(string key)
    {
        foreach (var culture in Cultures)
        {
            Manager.GetString(key, new CultureInfo(culture)).Should().NotBeNullOrWhiteSpace($"'{key}' in {culture}");
        }
    }

    [Theory]
    [MemberData(nameof(Keys))]
    public void EveryNewText_KeepsEveryPlaceholderInEveryLanguage(string key)
    {
        var expected = Placeholders(Manager.GetString(key, new CultureInfo("en"))!);

        foreach (var culture in Cultures)
        {
            Placeholders(Manager.GetString(key, new CultureInfo(culture))!).Should().Equal(expected, $"'{key}' in {culture}");
        }
    }

    private static List<string> Placeholders(string text) =>
        [.. PlaceholderPattern().Matches(text).Select(match => match.Value).Order()];

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex PlaceholderPattern();
}
