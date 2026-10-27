using MudBlazor;
using Simulab.Web.Theme;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-169 AC6: a marked answer and an answered cell are painted with <c>primarySelected</c> and a 2 px <c>primary</c>
/// border, and both read in the light and the dark theme: the text on the fill at AA, the border at the 3:1 of a
/// component. The numbers come from the stylesheet and the theme, not from the screen's own copy.
/// </summary>
public sealed class AnswerSelectionContrastTests
{
    private static readonly MudTheme Theme = SimulabTheme.Create();

    [Fact]
    public void MarkedRowAndAnsweredCell_ArePaintedWithThePrimarySelectedFillAndATwoPixelPrimaryBorder()
    {
        AppCssColours.Declaration(".app-answer-option-selected", "background").Should().Be("var(--app-primary-selected)");
        AppCssColours.Declaration(".app-answer-option-selected", "border-color").Should().Be("var(--mud-palette-primary)");
        AppCssColours.Declaration(".app-answer-option-selected", "box-shadow").Should().Be("inset 0 0 0 1px var(--mud-palette-primary)");
        AppCssColours.Declaration(".app-answer-cell-answered", "background").Should().Be("var(--app-primary-selected)");
        AppCssColours.Declaration(".app-answer-cell-answered", "border").Should().Be("2px solid var(--mud-palette-primary)");
    }

    [Theory]
    [InlineData("light", true)]
    [InlineData("dark", false)]
    public void TextAndBorder_ReadOnThePrimarySelectedFill(string what, bool light)
    {
        var palette = light ? (Palette)Theme.PaletteLight : Theme.PaletteDark;
        var fill = AppCssColours.Resolve(AppCssColours.Declaration(".app-answer-option-selected", "background"), palette);
        var text = AppCssColours.Resolve("var(--mud-palette-text-primary)", palette);
        var border = AppCssColours.Resolve(AppCssColours.Declaration(".app-answer-option-selected", "border-color"), palette);

        ColourContrast.Ratio(text, fill).Should().BeGreaterThanOrEqualTo(ColourContrast.MinimumForText, $"the option text on a marked row ({what})");
        ColourContrast.Ratio(border, fill).Should().BeGreaterThanOrEqualTo(ColourContrast.MinimumForNonText, $"the border of a marked row ({what})");
    }
}
