using Bunit;
using MudBlazor;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

public class AppFilterChipTests : KitTestContext
{
    [Fact]
    public void Render_ShowsTheTextAndARemoveButtonNamedAfterIt()
    {
        var removed = false;
        var chip = Render<AppFilterChip>(parameters => parameters
            .Add(p => p.Text, "User: ana@exemplo.com")
            .Add(p => p.OnRemove, () => removed = true));

        chip.Find(".app-filter-chip-text").TextContent.Should().Be("User: ana@exemplo.com");
        var remove = chip.Find("button.app-filter-chip-remove");
        remove.GetAttribute("aria-label").Should().Be("Remove the filter User: ana@exemplo.com");
        chip.FindComponent<MudTooltip>().Instance.Text.Should().Be("Remove the filter User: ana@exemplo.com");

        remove.Click();
        removed.Should().BeTrue();
    }
}
