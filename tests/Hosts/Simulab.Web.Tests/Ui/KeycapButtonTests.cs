using Bunit;
using MudBlazor;
using Simulab.Web.Theme;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-125 AC4 (BR4): a page keeps writing <c>MudButton</c> with its own <c>Variant</c> and <c>Color</c>; the kit stylesheet
/// turns the classes MudBlazor renders into the four variants of the brand. Nothing on a page changes to get the look.
/// </summary>
public sealed class KeycapButtonTests : KitTestContext
{
    public static TheoryData<Variant, Color, string> Variants() => new()
    {
        { Variant.Filled, Color.Primary, ".mud-button-filled.mud-button-filled-primary" },
        { Variant.Outlined, Color.Default, ".mud-button-root.mud-button-outlined" },
        { Variant.Text, Color.Primary, ".mud-button-root.mud-button-text" },
        { Variant.Filled, Color.Tertiary, ".mud-button-filled.mud-button-filled-tertiary" },
    };

    [Theory]
    [MemberData(nameof(Variants))]
    public void MudButton_RendersTheClassesTheKitStyles(Variant variant, Color color, string selector)
    {
        var button = Render<MudButton>(parameters => parameters
            .Add(p => p.Variant, variant)
            .Add(p => p.Color, color)
            .AddChildContent("Save"));

        var classes = button.Find("button").ClassList;
        foreach (var part in selector.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            classes.Should().Contain(part, $"the kit's rule '{selector}' must match the button MudBlazor renders");
        }
    }

    /// <summary>Primary and action are keycaps: the fill, the text and the edge under it, from the theme.</summary>
    [Theory]
    [InlineData(".mud-button-filled.mud-button-filled-primary", "primary")]
    [InlineData(".mud-button-filled.mud-button-filled-tertiary", "tertiary")]
    [InlineData(".mud-button-filled.mud-button-filled-error", "error")]
    public void FilledVariant_IsAKeycapWithItsEdge(string selector, string colour)
    {
        AppCssColours.Declaration(selector, "background-color").Should().Be($"var(--mud-palette-{colour})");
        AppCssColours.Declaration(selector, "color").Should().Be($"var(--mud-palette-{colour}-text)");
        AppCssColours.Declaration(selector, "box-shadow").Should().Be($"0 var(--app-keycap-depth) 0 var(--app-keycap-{colour}-edge)");
    }

    [Fact]
    public void SecondaryVariant_IsTheSurfaceInsideALineStrongBorder()
    {
        const string selector = ".mud-button-root.mud-button-outlined";

        AppCssColours.Declaration(selector, "background-color").Should().Be("var(--mud-palette-surface)");
        AppCssColours.Declaration(selector, "border").Should().Be("2px solid var(--mud-palette-lines-inputs)");
        AppCssColours.Declaration(selector, "color").Should().Be("var(--mud-palette-text-primary)");
    }

    [Fact]
    public void TextVariant_IsAnUnderlinedInfoLink()
    {
        const string selector = ".mud-button-root.mud-button-text";

        AppCssColours.Declaration(selector, "color").Should().Be("var(--mud-palette-info)");
        AppCssColours.Declaration(selector, "text-decoration").Should().StartWith("underline");
        AppCssColours.Declaration(selector, "background-color").Should().Be("transparent");
    }

    [Fact]
    public void Buttons_HaveTheMinimumHeights()
    {
        AppCssColours.Declaration(".mud-button-root.mud-button", "min-height").Should().Be("44px");
        // MudBlazor names the small size per variant: mud-button-filled-size-small, -outlined-, -text-.
        foreach (var (variant, kind) in new[] { (Variant.Filled, "filled"), (Variant.Outlined, "outlined"), (Variant.Text, "text") })
        {
            var selector = $".mud-button-root.mud-button-{kind}-size-small";
            AppCssColours.Declaration(selector, "min-height").Should().Be("36px", selector);
            Render<MudButton>(parameters => parameters.Add(p => p.Variant, variant).Add(p => p.Size, Size.Small).AddChildContent("Filter"))
                .Find("button").ClassList.Should().Contain($"mud-button-{kind}-size-small");
        }
    }

    /// <summary>Pressed moves the key down and shrinks the edge; with reduced motion it happens at once.</summary>
    [Fact]
    public void PressedKeycap_MovesDownAndHonoursReducedMotion()
    {
        AppCssColours.Declaration(".mud-button-filled:active:not(:disabled)", "transform").Should().Be("translateY(var(--app-keycap-press))");
        AppCssColours.Declaration(".mud-button-root.mud-button", "transition").Should().Contain("transform");
        AppCssColours.MediaDeclaration("prefers-reduced-motion: reduce", ".mud-button-root.mud-button", "transition").Should().Be("none");
    }

    /// <summary>Disabled: the sunken fill, ink-muted text, no edge.</summary>
    [Fact]
    public void DisabledButton_IsSunkenWithNoEdge()
    {
        const string selector = ".mud-button-filled:disabled";

        AppCssColours.Declaration(selector, "background-color").Should().Be("var(--mud-palette-background-gray) !important");
        AppCssColours.Declaration(selector, "color").Should().Be("var(--mud-palette-text-secondary)");
        AppCssColours.Declaration(selector, "box-shadow").Should().Be("none");
    }

    /// <summary>The edges reach the page: the provider writes them in the same block as the palette, per theme.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ThemeProvider_WritesTheKeycapEdgesOfTheThemeInUse(bool dark)
    {
        var edges = dark ? SimulabTheme.DarkKeycapEdges : SimulabTheme.LightKeycapEdges;

        var provider = Render<SimulabThemeProvider>(parameters => parameters
            .Add(p => p.Theme, SimulabTheme.Create())
            .Add(p => p.IsDarkMode, dark));

        provider.Markup.Should().Contain($"--app-keycap-primary-edge: {edges.Primary};")
            .And.Contain($"--app-keycap-secondary-edge: {edges.Secondary};")
            .And.Contain($"--app-keycap-tertiary-edge: {edges.Tertiary};")
            .And.Contain($"--app-keycap-error-edge: {edges.Error};");
    }
}
