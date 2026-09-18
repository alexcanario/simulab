using System.Collections;
using System.Globalization;
using System.Resources;
using Simulab.Web.Localization;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests;

/// <summary>A key added to one language must be added to all three (rule: i18n).</summary>
public class ResourceParityTests
{
    /// <summary>Every resource set of the Web. A new set is added here in the feature that creates it.</summary>
    private static readonly Type[] Sets = [typeof(SharedResources), typeof(IdentityResources)];

    private static ResourceManager ManagerFor(string setName)
    {
        var marker = Sets.Single(type => type.Name == setName);
        return new ResourceManager(marker.FullName!, marker.Assembly);
    }

    private static HashSet<string> Keys(ResourceManager manager, CultureInfo culture)
    {
        var set = manager.GetResourceSet(culture, createIfNotExists: true, tryParents: false);
        set.Should().NotBeNull($"the resource file for '{culture.Name}' must exist");
        return set!.Cast<DictionaryEntry>().Select(entry => (string)entry.Key).ToHashSet();
    }

    public static TheoryData<string, string> SetsAndTranslatedCultures()
    {
        var data = new TheoryData<string, string>();
        foreach (var set in Sets)
        {
            foreach (var culture in SupportedCultures.All.Select(c => c.Name).Where(name => name != SupportedCultures.Default))
            {
                data.Add(set.Name, culture);
            }
        }

        return data;
    }

    public static TheoryData<string> SetNames() => new(Sets.Select(type => type.Name));

    [Theory]
    [MemberData(nameof(SetNames))]
    public void Neutral_resources_are_not_empty(string setName)
    {
        Keys(ManagerFor(setName), CultureInfo.InvariantCulture).Should().NotBeEmpty();
    }

    [Fact]
    public void Every_set_is_checked_in_two_translated_cultures()
    {
        SetsAndTranslatedCultures().Should().HaveCount(Sets.Length * 2);
    }

    [Theory]
    [MemberData(nameof(SetsAndTranslatedCultures))]
    public void Every_key_exists_in_every_language(string setName, string cultureName)
    {
        var manager = ManagerFor(setName);

        Keys(manager, new CultureInfo(cultureName)).Should().BeEquivalentTo(Keys(manager, CultureInfo.InvariantCulture));
    }

    [Theory]
    [MemberData(nameof(SetsAndTranslatedCultures))]
    public void Placeholders_match_the_neutral_text(string setName, string cultureName)
    {
        var manager = ManagerFor(setName);
        var culture = new CultureInfo(cultureName);

        foreach (var key in Keys(manager, CultureInfo.InvariantCulture))
        {
            var neutral = manager.GetString(key, CultureInfo.InvariantCulture)!;
            var translated = manager.GetString(key, culture)!;

            translated.Contains("{0}").Should().Be(neutral.Contains("{0}"), $"'{key}' in {cultureName} must keep its placeholder");
        }
    }
}
