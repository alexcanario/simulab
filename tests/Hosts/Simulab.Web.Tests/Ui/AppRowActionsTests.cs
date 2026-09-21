using Bunit;
using MudBlazor;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

public class AppRowActionsTests : KitTestContext
{
    [Fact]
    public void Render_EditAndDelete_ShowsEditThenDeleteWithTooltipsAndNames()
    {
        var edited = false;
        var actions = Render<AppRowActions>(parameters => parameters
            .Add(p => p.ItemName, "ABC")
            .Add(p => p.OnEdit, () => edited = true)
            .Add(p => p.OnDelete, () => { }));

        var buttons = actions.FindAll("button.app-row-action");
        buttons.Select(b => b.GetAttribute("aria-label")).Should().Equal("Edit: ABC", "Delete: ABC");
        actions.FindComponents<MudTooltip>().Select(t => t.Instance.Text).Should().Equal("Edit", "Delete");
        actions.FindAll(".app-row-more").Should().BeEmpty();

        buttons[0].Click();
        edited.Should().BeTrue();
    }

    [Fact]
    public void Render_DeleteDisabledReason_ShowsDeleteDisabledWithTheReasonAsTooltip()
    {
        var deleted = false;
        var actions = Render<AppRowActions>(parameters => parameters
            .Add(p => p.ItemName, "ABC")
            .Add(p => p.OnEdit, () => { })
            .Add(p => p.OnDelete, () => deleted = true)
            .Add(p => p.DeleteDisabledReason, "Held by 2 users"));

        var delete = actions.FindAll("button.app-row-action")[1];
        delete.HasAttribute("disabled").Should().BeTrue();
        delete.GetAttribute("aria-label").Should().Be("Delete: ABC");
        actions.FindComponents<MudTooltip>().Select(t => t.Instance.Text).Should().Equal("Edit", "Held by 2 users");
        deleted.Should().BeFalse();
    }

    [Fact]
    public void Render_FiveActions_ShowsThreeAndPutsTheRestInOverflowMenu()
    {
        var popovers = Render<MudPopoverProvider>();
        var clicked = new List<string>();
        var actions = Render<AppRowActions>(parameters => parameters
            .Add(p => p.OnEdit, () => { })
            .Add(p => p.OnDelete, () => { })
            .Add(p => p.MoreActions, new[]
            {
                new AppRowAction("View", AppIcons.View, () => { clicked.Add("View"); return Task.CompletedTask; }),
                new AppRowAction("Duplicate", AppIcons.Copy, () => { clicked.Add("Duplicate"); return Task.CompletedTask; }),
                new AppRowAction("Archive", AppIcons.Archive, () => { clicked.Add("Archive"); return Task.CompletedTask; }),
            }));

        actions.FindAll("button.app-row-action").Select(b => b.GetAttribute("aria-label")).Should().Equal("Edit", "Delete", "View");
        AppRowActions.MaxVisible.Should().Be(3);

        var menu = actions.FindComponent<MudMenu>();
        menu.Instance.AriaLabel.Should().Be("More actions");
        actions.FindComponents<MudTooltip>().Select(t => t.Instance.Text).Should().Contain("More actions");

        actions.Find(".app-row-more button").Click();
        popovers.WaitForAssertion(() => popovers.FindAll(".app-row-more-item").Select(i => i.TextContent.Trim()).Should().Equal("Duplicate", "Archive"));

        popovers.FindAll(".app-row-more-item")[1].Click();
        // B-11: Click() can return before the item's handler runs while the popover is still rendering.
        popovers.WaitForAssertion(() => clicked.Should().Equal("Archive"));
    }

    [Fact]
    public void Render_DeleteAction_UsesErrorColor()
    {
        var actions = Render<AppRowActions>(parameters => parameters
            .Add(p => p.OnEdit, () => { })
            .Add(p => p.OnDelete, () => { }));

        var buttons = actions.FindComponents<MudIconButton>();
        buttons[0].Instance.Color.Should().Be(Color.Default);
        buttons[1].Instance.Color.Should().Be(Color.Error);
    }
}
