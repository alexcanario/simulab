using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MudBlazor;
using Simulab.Web.Components.Pages.Dev;
using Simulab.Web.Theme;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-125 AC13 (UC3): the gallery shows the identity a screen item builds from — every button variant with its sizes and
/// states, the type scale and the mono font, and the palette of the theme in use with each role's name and value.
/// </summary>
public sealed class UiGalleryIdentityTests : KitTestContext
{
    private IRenderedComponent<UiGallery> RenderGallery()
    {
        Services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(Environments.Development));
        Render<MudPopoverProvider>();
        return Render<UiGallery>(parameters => parameters.Add(p => p.SourceDelay, TimeSpan.Zero));
    }

    [Fact]
    public void Buttons_ShowTheFourVariantsTheSmallSizeAndTheStates()
    {
        var section = RenderGallery().Find("#gallery-buttons");

        section.QuerySelectorAll("button.mud-button-filled-primary").Should().NotBeEmpty();
        section.QuerySelectorAll("button.mud-button-outlined").Should().NotBeEmpty();
        section.QuerySelectorAll("button.mud-button-text").Should().NotBeEmpty();
        // F-148 BR7/BR8 (AC11): the action is the shortcut, with its compact variant and a disabled one.
        section.QuerySelectorAll(".app-action:not(.app-action-compact) .app-action-key:not([disabled])").Should().ContainSingle("the shortcut");
        section.QuerySelectorAll(".app-action.app-action-compact .app-action-key").Should().ContainSingle("the compact variant");
        section.QuerySelectorAll(".app-action-disabled .app-action-key[disabled]").Should().ContainSingle("the disabled shortcut");
        section.QuerySelectorAll(".app-action-ctrl, .app-action-alt").Should().HaveCount(4, "the shortcut and the disabled one show Ctrl and Alt");
        section.QuerySelectorAll("button.mud-button-filled-tertiary").Should().BeEmpty("the action is the shortcut component, not a tertiary MudButton");
        section.QuerySelectorAll("button.mud-button-filled-error, button.mud-button-outlined-error").Should().NotBeEmpty();
        section.QuerySelectorAll("button[class*='-size-small']").Should().NotBeEmpty();
        section.QuerySelectorAll("button[disabled]").Should().NotBeEmpty();
        section.TextContent.Should().Contain("Hover").And.Contain("Pressed").And.Contain("Focus").And.Contain("Disabled");
    }

    [Fact]
    public void Type_ShowsTheScaleAndTheMonoFont()
    {
        var section = RenderGallery().Find("#gallery-type");

        foreach (var step in new[] { "ui-title 28/32", "ui-heading 20/24", "ui-subheading 16/20", "ui-body 15/22", "ui-control 14/20", "ui-caption 12/16", "mono 14/22" })
        {
            section.TextContent.Should().Contain(step);
        }

        section.QuerySelectorAll("code, kbd").Should().NotBeEmpty("the mono font is shown on code and a keyboard shortcut");
    }

    [Fact]
    public void Palette_ShowsEveryRoleOfTheThemeInUseWithItsValue()
    {
        var section = RenderGallery().Find("#gallery-palette");
        var light = SimulabTheme.Create().PaletteLight;

        foreach (var (role, value) in new[]
                 {
                     ("primary", light.Primary.ToString()), ("info", light.Info.ToString()), ("secondary", light.Secondary.ToString()),
                     ("tertiary", light.Tertiary.ToString()), ("background", light.Background.ToString()), ("surface", light.Surface.ToString()),
                     ("text-primary", light.TextPrimary.ToString()), ("lines-inputs", light.LinesInputs.ToString()), ("error", light.Error.ToString()),
                 })
        {
            var swatch = section.QuerySelector($"[data-role='{role}']");
            swatch.Should().NotBeNull(role);
            swatch!.TextContent.Should().Contain(role).And.Contain(IdentityTokensComparison.Hex(value)[..7]);
        }

        // F-148 AC11: the link role is listed with the value of the theme in use.
        var link = section.QuerySelector("[data-role='link']");
        link.Should().NotBeNull("link");
        link!.TextContent.Should().Contain("link").And.Contain(SimulabTheme.LightLink);
    }
}
