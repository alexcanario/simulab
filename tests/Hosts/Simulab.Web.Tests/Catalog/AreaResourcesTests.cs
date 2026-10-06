using System.Globalization;
using System.Resources;
using Simulab.Web.Components.Pages.Catalog;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-79 AC2 and BR1: each seeded area has its name in en, pt-BR and pt-PT, with the words of the item's approved
/// list. The codes are the ones the migration seeds; a code with no resource would show the key instead of a name.
/// </summary>
public sealed class AreaResourcesTests
{
    private static readonly ResourceManager Manager = new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    public static TheoryData<string, string, string, string> Areas() => new()
    {
        { "Languages", "Languages", "Linguagens", "Linguagens" },
        { "Mathematics", "Mathematics", "Matemática", "Matemática" },
        { "LogicalReasoning", "Logical Reasoning", "Raciocínio Lógico", "Raciocínio Lógico" },
        { "NaturalSciences", "Natural Sciences", "Ciências da Natureza", "Ciências da Natureza" },
        { "HumanSciences", "Human Sciences", "Ciências Humanas", "Ciências Humanas" },
        { "Law", "Law", "Direito", "Direito" },
        { "InformationTechnology", "Information Technology", "Tecnologia da Informação", "Tecnologias de Informação" },
        { "Administration", "Administration and Management", "Administração e Gestão", "Administração e Gestão" },
        { "SpecificKnowledge", "Specific Knowledge", "Conhecimentos Específicos", "Conhecimentos Específicos" },
    };

    [Theory]
    [MemberData(nameof(Areas))]
    public void EveryArea_HasItsNameInTheThreeLanguages(string code, string en, string ptBr, string ptPt)
    {
        Manager.GetString($"Area.{code}", new CultureInfo("en")).Should().Be(en, $"'{code}' in en");
        Manager.GetString($"Area.{code}", new CultureInfo("pt-BR")).Should().Be(ptBr, $"'{code}' in pt-BR");
        Manager.GetString($"Area.{code}", new CultureInfo("pt-PT")).Should().Be(ptPt, $"'{code}' in pt-PT");
    }

    // The screen reads the name through the code, in the reader's language.
    [Theory]
    [InlineData("en", "Law")]
    [InlineData("pt-BR", "Direito")]
    [InlineData("pt-PT", "Direito")]
    public void AreaText_NamesTheAreaInTheReadersLanguage(string culture, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(culture);
            var localizer = new ResourceManagerLocalizer(Manager);

            AreaText.Name(localizer, "Law").Should().Be(expected);
            AreaText.Name(localizer, null).Should().BeEmpty();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    private sealed class ResourceManagerLocalizer(ResourceManager manager) : Microsoft.Extensions.Localization.IStringLocalizer
    {
        public Microsoft.Extensions.Localization.LocalizedString this[string name] =>
            new(name, manager.GetString(name, CultureInfo.CurrentUICulture) ?? name);

        public Microsoft.Extensions.Localization.LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(CultureInfo.CurrentCulture, manager.GetString(name, CultureInfo.CurrentUICulture) ?? name, arguments));

        public IEnumerable<Microsoft.Extensions.Localization.LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
