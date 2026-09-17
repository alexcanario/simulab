using System.Collections;
using System.Globalization;
using System.Resources;
using Simulab.Identity.Infrastructure.Resources;

namespace Simulab.Identity.Tests;

/// <summary>AC15 for the module's own texts: the verification email exists in the three languages.</summary>
public class EmailResourceParityTests
{
    private static readonly ResourceManager Manager = new(typeof(IdentityEmails).FullName!, typeof(IdentityEmails).Assembly);

    private static HashSet<string> Keys(CultureInfo culture)
    {
        var set = Manager.GetResourceSet(culture, createIfNotExists: true, tryParents: false);
        set.Should().NotBeNull($"the resource file for '{culture.Name}' must exist");
        return [.. set!.Cast<DictionaryEntry>().Select(entry => (string)entry.Key)];
    }

    [Fact]
    public void Neutral_resources_are_not_empty()
    {
        Keys(CultureInfo.InvariantCulture).Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("pt-BR")]
    [InlineData("pt-PT")]
    public void Every_key_exists_in_every_language(string cultureName)
    {
        Keys(new CultureInfo(cultureName)).Should().BeEquivalentTo(Keys(CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("pt-BR")]
    [InlineData("pt-PT")]
    public void Every_text_differs_from_the_neutral_one(string cultureName)
    {
        var culture = new CultureInfo(cultureName);

        // pt-BR and pt-PT are translations, not copies of the English text (rule: i18n).
        foreach (var key in Keys(CultureInfo.InvariantCulture))
        {
            Manager.GetString(key, culture).Should().NotBe(Manager.GetString(key, CultureInfo.InvariantCulture), key);
        }
    }
}
