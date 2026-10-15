using System.Globalization;
using System.Resources;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-77 AC16: every new text of the "Other names" field exists in en, pt-BR and pt-PT. ResourceParityTests checks
/// that no key is missing in a language, and ErrorCodeTextTests finds every error code of the solution and checks
/// its text; this pins the keys the field and the dialogs read, and the new gallery section.
/// </summary>
public sealed class AliasResourcesTests
{
    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    private static readonly string[] Cultures = ["en", "pt-BR", "pt-PT"];

    public static TheoryData<string> Keys() => new(
    [
        "Subjects.Field.Aliases", "Subjects.Field.Aliases.Placeholder", "Subjects.Field.Aliases.Help",
        "Topics.Field.Aliases", "Topics.Field.Aliases.Placeholder", "Topics.Field.Aliases.Help",
        "Aliases.Added", "Aliases.Removed", "Common.RemoveItem",
        "subject.alias_too_short", "subject.alias_too_long", "subject.alias_same_as_name", "subject.alias_taken",
        "topic.alias_too_short", "topic.alias_too_long", "topic.alias_same_as_name", "topic.alias_taken",
        "Gallery.Section.ChipsField", "Gallery.ChipsField.Hint", "Gallery.ChipsField.Label"
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
    [InlineData("Aliases.Added")]
    [InlineData("Aliases.Removed")]
    public void TheAnnouncements_KeepTheirNamePlaceholderInEveryLanguage(string key)
    {
        foreach (var culture in Cultures)
        {
            Manager.GetString(key, new CultureInfo(culture)).Should().Contain("{0}", $"'{key}' in {culture}");
        }
    }
}
