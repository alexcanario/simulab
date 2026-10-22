using System.Globalization;
using System.Resources;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-90 AC12, BR12: every new text of the "Where used" action and dialog exists in en, pt-BR and pt-PT.
/// ResourceParityTests checks that no key is missing in a language; this pins the keys the screens read.
/// </summary>
public sealed class WhereUsedResourcesTests
{
    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    private static readonly string[] Cultures = ["en", "pt-BR", "pt-PT"];

    public static TheoryData<string> Keys() => new(
    [
        "WhereUsed.Action", "WhereUsed.Title", "WhereUsed.NoticeSubjects", "WhereUsed.Questions",
        "WhereUsed.QuestionCount", "WhereUsed.QuestionCount.One", "WhereUsed.Empty"
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
    [InlineData("WhereUsed.Title")]
    [InlineData("WhereUsed.QuestionCount")]
    [InlineData("WhereUsed.QuestionCount.One")]
    public void TheTextsWithAValue_KeepTheirPlaceholderInEveryLanguage(string key)
    {
        foreach (var culture in Cultures)
        {
            Manager.GetString(key, new CultureInfo(culture)).Should().Contain("{0}", $"'{key}' in {culture}");
        }
    }
}
