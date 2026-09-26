using Bunit;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-43 BR1 and UC3: everything a failed save refused, once, at the top of the card. It is announced, it can
/// be focused, and each line jumps to its field — through the shell helper, because an in-page link would
/// leave the page under <c>&lt;base href="/"&gt;</c> (rule: ui-project).
/// </summary>
public sealed class AppErrorSummaryTests : KitTestContext
{
    private static readonly IReadOnlyList<AppErrorSummaryItem> Two =
    [
        new("exam-name", "Name"),
        new("exam-scope", "Scope")
    ];

    private IRenderedComponent<AppErrorSummary> Render(IReadOnlyList<AppErrorSummaryItem> items) =>
        Render<AppErrorSummary>(parameters => parameters
            .Add(summary => summary.Id, "exam-errors")
            .Add(summary => summary.Title, "Fill the required fields before saving:")
            .Add(summary => summary.Items, items));

    [Fact]
    public void Render_Empty_NothingIsShown()
    {
        Render([]).Markup.Trim().Should().BeEmpty();
    }

    [Fact]
    public void Render_TheSummaryIsAnnouncedAndCanTakeFocus()
    {
        var summary = Render(Two);

        var alert = summary.Find("#exam-errors");
        alert.GetAttribute("role").Should().Be("alert");
        alert.GetAttribute("tabindex").Should().Be("-1");
    }

    [Fact]
    public void Render_OneLinePerRefusedField_PointingAtIt()
    {
        var summary = Render(Two);

        var links = summary.FindAll(".app-error-summary-list a");
        links.Select(link => link.TextContent).Should().Equal("Name", "Scope");
        links.Select(link => link.GetAttribute("href")).Should().Equal("#exam-name", "#exam-scope");
    }

    [Fact]
    public void Click_ALine_MovesFocusThroughTheShellInsteadOfFollowingTheLink()
    {
        var call = JSInterop.SetupVoid("simulabShell.focusElement", "exam-scope");
        var summary = Render(Two);

        summary.FindAll(".app-error-summary-list a")[1].Click();

        call.Invocations.Should().ContainSingle();
    }
}
