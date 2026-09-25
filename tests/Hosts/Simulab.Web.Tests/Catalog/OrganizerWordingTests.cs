using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Simulab.Catalog.Contracts;
using Simulab.Web.Resources;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.Catalog;

/// <summary>
/// F-34 AC21 (v2): Brazil calls the institution that applies a paper a "banca", so the pt-BR texts say that
/// and nothing else. pt-PT and en keep their own wording, and the kinds stay the three F-33 defined — the
/// fourth existed only while one table held both roles.
/// </summary>
public sealed class OrganizerWordingTests : KitTestContext
{
    private IStringLocalizer<SharedResources> Localizer(string culture)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        return Services.GetRequiredService<IStringLocalizer<SharedResources>>();
    }

    [Theory]
    [InlineData("Nav.Organizers", "Bancas")]
    [InlineData("Organizers.Title", "Bancas")]
    [InlineData("Organizers.Dialog.AddTitle", "Adicionar banca")]
    [InlineData("Organizers.Dialog.EditTitle", "Editar banca")]
    [InlineData("Organizers.Saved", "Banca salva.")]
    [InlineData("Organizers.Deleted", "Banca excluída.")]
    public void PtBr_TheOrganizerScreen_SaysBanca(string key, string expected)
    {
        Localizer("pt-BR")[key].Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("organizer.name_taken", "Outra banca já tem esse nome.")]
    [InlineData("organizer.acronym_taken", "Outra banca já tem essa sigla.")]
    public void PtBr_TheOrganizerErrors_SayBanca(string code, string expected)
    {
        Localizer("pt-BR")[code].Value.Should().Be(expected);
    }

    [Fact]
    public void PtBr_NoOrganizerTextStillSaysOrganizadora()
    {
        var localizer = Localizer("pt-BR");

        var stale = localizer.GetAllStrings(includeParentCultures: false)
            .Where(entry => entry.Name.StartsWith("Organizers.", StringComparison.Ordinal)
                || entry.Name == "Nav.Organizers"
                || entry.Name.StartsWith("organizer.", StringComparison.Ordinal))
            .Where(entry => entry.Value.Contains("rganizadora", StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry.Name)
            .ToList();

        stale.Should().BeEmpty();
    }

    // The other two languages were not asked to change, so they must not have.
    [Theory]
    [InlineData("en", "Organizers")]
    [InlineData("pt-PT", "Entidades organizadoras")]
    public void OtherLanguages_KeepTheirWording(string culture, string expected)
    {
        Localizer(culture)["Nav.Organizers"].Value.Should().Be(expected);
    }

    [Fact]
    public void TheKinds_AreTheThreeOfF33()
    {
        Enum.GetValues<OrganizerKind>().Should().Equal(
            OrganizerKind.ExamBoard,
            OrganizerKind.CertifyingBody,
            OrganizerKind.University);
    }

    // The issuing authority is a different register, and its pt-BR name says so.
    [Fact]
    public void PtBr_TheIssuingAuthorityScreen_SaysOrgaoContratante()
    {
        var localizer = Localizer("pt-BR");

        localizer["Nav.IssuingAuthorities"].Value.Should().Be("Órgãos contratantes");
        localizer["IssuingAuthorities.Title"].Value.Should().Be("Órgãos contratantes");
    }
}
