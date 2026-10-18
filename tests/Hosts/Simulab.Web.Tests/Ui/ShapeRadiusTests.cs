using Simulab.Web.Theme;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-125 AC7 (BR7): tags and chips 6px, buttons and fields 12px (the theme's default radius, which MudBlazor's buttons
/// and outlined inputs read), cards and dialogs 20px.
/// </summary>
public sealed class ShapeRadiusTests
{
    [Fact]
    public void Theme_DefaultRadiusIsTheButtonAndFieldRadius()
    {
        SimulabTheme.Create().LayoutProperties.DefaultBorderRadius.Should().Be("12px");
    }

    [Theory]
    [InlineData("--app-radius-sm", "6px")]
    [InlineData("--app-radius-md", "12px")]
    [InlineData("--app-radius-lg", "20px")]
    public void Scale_HoldsTheThreeRadii(string token, string value)
    {
        AppCssColours.Declaration(":root", token).Should().Be(value);
    }

    [Theory]
    // Tags and chips.
    [InlineData(".app-chip", "var(--app-radius-sm)")]
    [InlineData(".app-badge", "var(--app-radius-sm)")]
    [InlineData(".app-status-chip", "var(--app-radius-sm)")]
    [InlineData(".app-filter-chip", "var(--app-radius-sm)")]
    [InlineData(".mud-chip.mud-chip-size-small", "var(--app-radius-sm)")]
    [InlineData(".mud-chip.mud-chip-size-medium", "var(--app-radius-sm)")]
    // Buttons and fields.
    [InlineData(".mud-button-root.mud-button", "var(--app-radius-md)")]
    [InlineData(".app-chips-input", "var(--app-radius-md)")]
    [InlineData(".app-mp-input", "var(--app-radius-md)")]
    [InlineData(".app-rt-frame", "var(--app-radius-md)")]
    // Cards and dialogs.
    [InlineData(".mud-paper:not(.mud-paper-square):not(.mud-popover)", "var(--app-radius-lg)")]
    [InlineData(".mud-dialog", "var(--app-radius-lg)")]
    [InlineData(".app-section-card", "var(--app-radius-lg)")]
    [InlineData(".app-form-card", "var(--app-radius-lg)")]
    [InlineData(".app-auth-card", "var(--app-radius-lg)")]
    public void Element_HasTheRadiusOfItsKind(string selector, string radius)
    {
        AppCssColours.Declaration(selector, "border-radius").Should().Be(radius);
    }
}
