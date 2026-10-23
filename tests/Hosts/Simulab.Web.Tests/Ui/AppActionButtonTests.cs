using Bunit;
using MudBlazor;
using Simulab.Web.Components.Ui;
using Simulab.Web.Theme;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-148 AC6 and AC7 (BR8): the action shortcut of the brand, Ctrl + Alt + the amber key, and its compact variant, the
/// amber key alone. One element is clickable and named by the caller's label; Ctrl, Alt and the plus signs are
/// decorative. The states are asserted on the kit stylesheet, as the other buttons' are.
/// </summary>
public sealed class AppActionButtonTests : KitTestContext
{
    private const string Key = ".app-action-key";

    private IRenderedComponent<AppActionButton> RenderAction(bool compact = false, bool disabled = false, Action? onClick = null) =>
        Render<AppActionButton>(parameters => parameters
            .Add(p => p.Label, "Start exam")
            .Add(p => p.Compact, compact)
            .Add(p => p.Disabled, disabled)
            .Add(p => p.OnClick, onClick is null ? default : Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, onClick)));

    [Fact]
    public void Shortcut_ShowsCtrlAltTwoPlusSignsAndTheAmberKey()
    {
        var action = RenderAction();

        action.FindAll(".app-action-ctrl").Should().ContainSingle().Which.TextContent.Trim().Should().Be("Ctrl");
        action.FindAll(".app-action-alt").Should().ContainSingle().Which.TextContent.Trim().Should().Be("Alt");
        action.FindAll(".app-action-plus").Should().HaveCount(2).And.OnlyContain(plus => plus.TextContent.Trim() == "+");
        action.FindAll(Key).Should().ContainSingle();
        action.Find(".app-action").ClassList.Should().NotContain("app-action-compact");
    }

    [Fact]
    public void Compact_ShowsTheAmberKeyAlone()
    {
        var action = RenderAction(compact: true);

        action.FindAll(".app-action-ctrl, .app-action-alt, .app-action-plus").Should().BeEmpty();
        action.FindAll(Key).Should().ContainSingle();
        action.Find(".app-action").ClassList.Should().Contain("app-action-compact");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheClickableElementIsOneButtonNamedByTheLabel(bool compact)
    {
        var action = RenderAction(compact);

        var clickable = action.FindAll("button, a, [role='button'], [tabindex]");
        clickable.Should().ContainSingle();
        clickable[0].ClassList.Should().Contain("app-action-key");
        clickable[0].GetAttribute("type").Should().Be("button");
        clickable[0].TextContent.Trim().Should().Be("Start exam", "the accessible name is the amber key's label");
        clickable[0].HasAttribute("aria-label").Should().BeFalse("the visible label is the name");
        action.Find($"{Key} .app-action-icon").GetAttribute("aria-hidden").Should().Be("true");
    }

    [Fact]
    public void CtrlAltAndPlusSigns_AreDecorative()
    {
        var action = RenderAction();

        foreach (var decoration in action.FindAll(".app-action-ctrl, .app-action-alt, .app-action-plus"))
        {
            decoration.GetAttribute("aria-hidden").Should().Be("true");
            decoration.QuerySelectorAll("button, a, [tabindex]").Should().BeEmpty();
        }
    }

    [Fact]
    public void Click_RaisesOnClick()
    {
        var clicks = 0;
        var action = RenderAction(onClick: () => clicks++);

        action.Find(Key).Click();

        clicks.Should().Be(1);
    }

    [Fact]
    public void Disabled_IsARealDisabledAndMarksTheWholeShortcut()
    {
        var action = RenderAction(disabled: true);

        action.Find(Key).HasAttribute("disabled").Should().BeTrue("disabled is a real attribute, not a class");
        action.Find(".app-action").ClassList.Should().Contain("app-action-disabled");
    }

    [Fact]
    public void Enabled_HasNoDisabledAttribute()
    {
        var action = RenderAction();

        action.Find(Key).HasAttribute("disabled").Should().BeFalse();
        action.Find(".app-action").ClassList.Should().NotContain("app-action-disabled");
    }

    /// <summary>The amber key is a keycap: the tertiary fill, its ink text and the amber edge, always drawn, 44px high at least.</summary>
    [Fact]
    public void AmberKey_IsAKeycapWithItsEdgeAndTheMinimumHeight()
    {
        AppCssColours.Declaration(Key, "background-color").Should().Be("var(--mud-palette-tertiary)");
        AppCssColours.Declaration(Key, "color").Should().Be("var(--mud-palette-tertiary-text)");
        AppCssColours.Declaration(Key, "box-shadow").Should().Be("0 var(--app-keycap-depth) 0 var(--app-keycap-tertiary-edge)");
        AppCssColours.Declaration(Key, "min-height").Should().Be("44px");
    }

    [Fact]
    public void CtrlIsTealAndAltIsBlue_EachWithItsLabelColourAndEdge()
    {
        AppCssColours.Declaration(".app-action-ctrl", "background-color").Should().Be("var(--mud-palette-secondary)");
        AppCssColours.Declaration(".app-action-ctrl", "color").Should().Be("var(--mud-palette-secondary-text)");
        AppCssColours.Declaration(".app-action-ctrl", "box-shadow").Should().Contain("var(--app-keycap-secondary-edge)");
        AppCssColours.Declaration(".app-action-alt", "background-color").Should().Be("var(--mud-palette-info)");
        AppCssColours.Declaration(".app-action-alt", "color").Should().Be("var(--mud-palette-info-text)");
        AppCssColours.Declaration(".app-action-alt", "box-shadow").Should().Contain("var(--app-keycap-info-edge)");
        AppCssColours.Declaration(".app-action-plus", "color").Should().Be("var(--app-brand-name)");
    }

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public void Labels_ReadAtAaOnTheirKeys(string theme)
    {
        var palette = theme == "light" ? (Palette)SimulabTheme.Create().PaletteLight : SimulabTheme.Create().PaletteDark;

        foreach (var (selector, background) in new[] { (".app-action-ctrl", "background-color"), (".app-action-alt", "background-color"), (Key, "background-color") })
        {
            var fill = AppCssColours.Resolve(AppCssColours.Declaration(selector, background), palette);
            var text = AppCssColours.Resolve(AppCssColours.Declaration(selector, "color"), palette);

            ColourContrast.Ratio(text, fill).Should().BeGreaterThanOrEqualTo(ColourContrast.MinimumForText, $"{selector} ({theme})");
        }
    }

    /// <summary>AC7 pressed: the amber key moves down and its edge shrinks; in the shortcut Ctrl and Alt sink too; the icon turns half way.</summary>
    [Fact]
    public void Pressed_MovesTheKeyDownShrinksTheEdgeAndTurnsTheIcon()
    {
        const string pressed = ".app-action-key:active:not(:disabled)";

        AppCssColours.Declaration(pressed, "transform").Should().Be("translateY(var(--app-keycap-press))");
        AppCssColours.Declaration(pressed, "box-shadow").Should().Be("0 var(--app-keycap-press) 0 var(--app-keycap-tertiary-edge)");
        AppCssColours.Declaration(".app-action:has(.app-action-key:active:not(:disabled)) .app-action-ctrl", "transform")
            .Should().Be("translateY(var(--app-keycap-press))");
        AppCssColours.Declaration(".app-action:has(.app-action-key:active:not(:disabled)) .app-action-alt", "transform")
            .Should().Be("translateY(var(--app-keycap-press))");
        AppCssColours.Declaration(".app-action-key:active:not(:disabled) .app-action-icon", "transform").Should().Be("rotate(200deg)");
        AppCssColours.Declaration(".app-action-icon", "transition").Should().Contain("0.4s");
    }

    /// <summary>AC7 disabled: the sunken fill, ink-muted text and no edge, on the amber key and on Ctrl and Alt of a disabled shortcut.</summary>
    [Fact]
    public void Disabled_IsSunkenWithNoEdge()
    {
        foreach (var selector in new[] { ".app-action-key:disabled", ".app-action-disabled .app-action-ctrl", ".app-action-disabled .app-action-alt" })
        {
            AppCssColours.Declaration(selector, "background-color").Should().Be("var(--mud-palette-background-gray)", selector);
            AppCssColours.Declaration(selector, "color").Should().Be("var(--mud-palette-text-secondary)", selector);
            AppCssColours.Declaration(selector, "box-shadow").Should().Be("none", selector);
        }
    }

    /// <summary>AC7 reduced motion: the icon does not turn and nothing animates.</summary>
    [Fact]
    public void ReducedMotion_StopsTheTurnAndTheTransitions()
    {
        AppCssColours.MediaDeclaration("prefers-reduced-motion: reduce", ".app-action-icon", "transition").Should().Be("none");
        AppCssColours.MediaDeclaration("prefers-reduced-motion: reduce", ".app-action-key", "transition").Should().Be("none");
    }
}
