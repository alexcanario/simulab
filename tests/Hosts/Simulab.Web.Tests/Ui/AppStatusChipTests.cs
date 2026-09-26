using Bunit;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>F-43 BR1 and UC4: a record's state, the same chip for the same meaning on every screen.</summary>
public sealed class AppStatusChipTests : KitTestContext
{
    private IRenderedComponent<AppStatusChip> Render(AppStatusTone tone) =>
        Render<AppStatusChip>(parameters => parameters
            .Add(chip => chip.Text, "Active")
            .Add(chip => chip.Tone, tone));

    [Theory]
    [InlineData(AppStatusTone.Neutral, "app-status-chip-neutral")]
    [InlineData(AppStatusTone.Success, "app-status-chip-success")]
    [InlineData(AppStatusTone.Warning, "app-status-chip-warning")]
    [InlineData(AppStatusTone.Error, "app-status-chip-error")]
    [InlineData(AppStatusTone.Info, "app-status-chip-info")]
    public void Render_TheToneIsAClassTheStylesheetKnows(AppStatusTone tone, string expected)
    {
        Render(tone).Find("span.app-status-chip").ClassList.Should().Contain(expected);
    }

    /// <summary>WCAG 2.2 1.4.1: colour is never the only carrier. The text is always there, the dot never alone.</summary>
    [Fact]
    public void Render_TheTextIsAlwaysThereAndTheDotIsDecorative()
    {
        var chip = Render(AppStatusTone.Success);

        chip.Find(".app-status-chip").TextContent.Trim().Should().Be("Active");
        chip.Find(".app-status-chip-dot").GetAttribute("aria-hidden").Should().Be("true");
    }

    [Fact]
    public void Render_EveryToneHasARuleInTheStylesheet()
    {
        foreach (var tone in Enum.GetValues<AppStatusTone>().Where(tone => tone != AppStatusTone.Neutral))
        {
            var selector = $".app-status-chip-{tone.ToString().ToLowerInvariant()} .app-status-chip-dot";
            AppCssColours.Declaration(selector, "background").Should().NotBeEmpty();
        }
    }
}
