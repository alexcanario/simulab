namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-183 BR2, BR3, found on screen: MudBlazor's <c>.mud-input &gt; input.mud-input-root</c> sets <c>font-family: inherit</c>
/// and wins over a plain <c>.app-code-field input</c>, so the code fields lost their monospace. The rule names the same input.
/// </summary>
public sealed class TextKindStylesTests
{
    [Theory]
    [InlineData(".app-code-field .mud-input > input.mud-input-root")]
    [InlineData(".app-otp-field .mud-input > input.mud-input-root")]
    public void CodeFields_AreMonospace_OverMudBlazorsInputRule(string selector) =>
        AppCssColours.Declaration(selector, "font-family").Should().Contain("--app-font-mono");
}
