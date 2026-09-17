using System.Collections;
using System.Globalization;
using System.Resources;
using Simulab.Web.Localization;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests;

/// <summary>A key added to one language must be added to all three (rule: i18n).</summary>
public class ResourceParityTests
{
    private static readonly ResourceManager Manager = new(typeof(Shared).FullName!, typeof(Shared).Assembly);

    private static HashSet<string> Keys(CultureInfo culture)
    {
        var set = Manager.GetResourceSet(culture, createIfNotExists: true, tryParents: false);
        set.Should().NotBeNull($"the resource file for '{culture.Name}' must exist");
        return set!.Cast<DictionaryEntry>().Select(entry => (string)entry.Key).ToHashSet();
    }

    public static TheoryData<string> TranslatedCultures() =>
        new(SupportedCultures.All.Select(c => c.Name).Where(name => name != SupportedCultures.Default));

    [Fact]
    public void Neutral_resources_are_not_empty()
    {
        Keys(CultureInfo.InvariantCulture).Should().NotBeEmpty();
    }

    [Fact]
    public void There_is_a_translated_culture_to_check()
    {
        TranslatedCultures().Should().HaveCount(2);
    }

    [Theory]
    [MemberData(nameof(TranslatedCultures))]
    public void Every_key_exists_in_every_language(string cultureName)
    {
        var neutral = Keys(CultureInfo.InvariantCulture);
        var translated = Keys(new CultureInfo(cultureName));

        translated.Should().BeEquivalentTo(neutral);
    }

    [Theory]
    [MemberData(nameof(TranslatedCultures))]
    public void Placeholders_match_the_neutral_text(string cultureName)
    {
        var culture = new CultureInfo(cultureName);

        foreach (var key in Keys(CultureInfo.InvariantCulture))
        {
            var neutral = Manager.GetString(key, CultureInfo.InvariantCulture)!;
            var translated = Manager.GetString(key, culture)!;

            translated.Contains("{0}").Should().Be(neutral.Contains("{0}"), $"'{key}' in {cultureName} must keep its placeholder");
        }
    }
}
