using Bunit;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-126 BR6 and AC2, AC7, AC8: what the question form needs of the amber key — an icon the page chooses, an id on
/// the button, a busy state that is the disabled look with progress in place of the icon, and the visible reason
/// the button points at. The look of the key itself stays with <see cref="AppActionButtonTests"/>.
/// </summary>
public sealed class AppActionButtonFormTests : KitTestContext
{
    private IRenderedComponent<AppActionButton> RenderAction(
        string icon = AppIcons.Publish,
        string? id = null,
        bool busy = false,
        bool disabled = false,
        string? describedBy = null,
        int rest = 0) =>
        Render<AppActionButton>(parameters => parameters
            .Add(p => p.Label, "Publish")
            .Add(p => p.Compact, true)
            .Add(p => p.Icon, icon)
            .Add(p => p.Id, id)
            .Add(p => p.Busy, busy)
            .Add(p => p.Disabled, disabled)
            .Add(p => p.DescribedBy, describedBy)
            .Add(p => p.IconRestRotation, rest));

    [Fact]
    public void Icon_IsTheOneThePageChose()
    {
        var publish = RenderAction(AppIcons.Publish).Find(".app-action-icon").InnerHtml;
        var restart = RenderAction(AppIcons.Restart).Find(".app-action-icon").InnerHtml;

        publish.Should().NotBe(restart, "the icon depends on what the action means, so it is a parameter and not a fixed Restart");
        publish.Should().Contain("<svg");
    }

    [Fact]
    public void Id_IsOnTheButtonItself()
    {
        var action = RenderAction(id: "question-publish");

        action.Find("button.app-action-key").Id.Should().Be("question-publish");
        action.FindAll("[id='question-publish']").Should().ContainSingle("the id is on the one clickable element, nowhere else");
    }

    [Fact]
    public void NoId_LeavesTheButtonWithoutOne()
    {
        RenderAction().Find("button.app-action-key").HasAttribute("id").Should().BeFalse();
    }

    [Fact]
    public void DescribedBy_IsAriaDescribedByOnTheButton()
    {
        var action = RenderAction(disabled: true, describedBy: "question-publication-reason");

        action.Find("button.app-action-key").GetAttribute("aria-describedby").Should().Be("question-publication-reason");
        RenderAction().Find("button.app-action-key").HasAttribute("aria-describedby").Should().BeFalse();
    }

    [Fact]
    public void Busy_DisablesTheKeyShowsProgressInPlaceOfTheIconAndKeepsTheLabel()
    {
        var action = RenderAction(busy: true);

        var button = action.Find("button.app-action-key");
        button.HasAttribute("disabled").Should().BeTrue("a busy key cannot be pressed again");
        button.QuerySelectorAll(".mud-progress-circular").Should().ContainSingle();
        button.QuerySelectorAll(".app-action-icon svg.mud-icon-root").Should().BeEmpty("the progress takes the icon's place");
        button.TextContent.Trim().Should().Be("Publish");
        action.Find(".app-action").ClassList.Should().Contain("app-action-disabled", "busy and disabled share the sunken look");
    }

    [Fact]
    public void NotBusy_ShowsTheIconAndNoProgress()
    {
        var action = RenderAction();

        action.FindAll(".mud-progress-circular").Should().BeEmpty();
        action.FindAll(".app-action-icon svg").Should().NotBeEmpty();
        action.Find("button.app-action-key").HasAttribute("disabled").Should().BeFalse();
    }

    /// <summary>Owner, screen review, 2026-10-23: the Publish arrow rests pointing down and turns up while the key is pressed.</summary>
    [Fact]
    public void IconRestRotation_TurnsTheRestingIconAndIsOffByDefault()
    {
        var plain = RenderAction().Find(".app-action-icon");
        var rested = RenderAction(rest: 180).Find(".app-action-icon");

        plain.HasAttribute("style").Should().BeFalse("the default key keeps the press spin of F-148");
        rested.OuterHtml.Should().NotBe(plain.OuterHtml);
        rested.OuterHtml.Should().Contain("180");
    }

    [Fact]
    public void Busy_OnADisabledKey_StillShowsProgress()
    {
        var action = RenderAction(busy: true, disabled: true);

        action.FindAll(".mud-progress-circular").Should().ContainSingle();
        action.Find("button.app-action-key").HasAttribute("disabled").Should().BeTrue();
    }
}
