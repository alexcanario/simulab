using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using Simulab.Web.Components.Ui;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-35: the picker's own controls (calendar button, month and year arrows) are read in the reader's language,
/// from <see cref="SharedResources"/> - and a key the map does not know is left to MudBlazor's English.
/// </summary>
public sealed class SharedResourcesMudLocalizerTests : KitTestContext
{
    private MudLocalizer Localizer() => Services.GetRequiredService<MudLocalizer>();

    [Fact]
    public void UiKit_RegistersTheAppsLocalizer()
    {
        Localizer().Should().BeOfType<SharedResourcesMudLocalizer>();
    }

    [Theory]
    [InlineData("en", "MudBaseDatePicker_Open", "Open the calendar")]
    [InlineData("pt-BR", "MudBaseDatePicker_PrevMonth", "Mês anterior")]
    [InlineData("pt-BR", "MudBaseDatePicker_NextMonth", "Próximo mês")]
    [InlineData("pt-PT", "MudBaseDatePicker_NextMonth", "Mês seguinte")]
    [InlineData("pt-PT", "MudBaseDatePicker_PrevYear", "Ano anterior")]
    public void Localizer_AKeyItKnows_AnswersInTheReadersLanguage(string culture, string key, string expected)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(culture);

        var text = Localizer()[key];

        text.ResourceNotFound.Should().BeFalse();
        text.Value.Should().Be(expected);
    }

    [Fact]
    public void Localizer_AKeyItDoesNotKnow_IsNotFoundSoMudBlazorKeepsItsOwnText()
    {
        Localizer()["MudDataGrid_Apply"].ResourceNotFound.Should().BeTrue();
    }

    // Every key the map answers with exists in the shared resources of the three languages.
    [Theory]
    [InlineData("en")]
    [InlineData("pt-BR")]
    [InlineData("pt-PT")]
    public void Keys_EveryOneMapsToAnExistingResource(string culture)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        var shared = Services.GetRequiredService<IStringLocalizer<SharedResources>>();

        SharedResourcesMudLocalizer.Keys.Should().NotBeEmpty();
        SharedResourcesMudLocalizer.Keys.Values
            .Where(own => shared[own].ResourceNotFound)
            .Should().BeEmpty($"every mapped key must exist in {culture}");
    }
}
