using Bunit;

namespace Simulab.Web.Tests.Attempts;

/// <summary>
/// F-169 `/attempts/{id}` on a phone (390 px, below md): one column, the way out as an icon, and the answer sheet in a
/// drawer that the bar's button opens, with a close button; a cell shows its question and closes the drawer.
/// </summary>
public sealed class TakeAttemptPhoneTests() : AttemptTestContext(390)
{
    private IRenderedComponent<Simulab.Web.Components.Layout.ExamSessionLayout> RenderOnAPhone()
    {
        Api.Content = AttemptSamples.InProgress();
        var layout = RenderSimulator();
        layout.WaitForAssertion(() => layout.FindAll(".app-session-sheet-button").Should().ContainSingle());
        return layout;
    }

    // AC4 on a phone: the sheet is not beside the question; the bar's button, named with the progress, opens it.
    [Fact]
    public void TheSheetIsADrawer_OpenedByTheBarsButton_WithACloseButton()
    {
        var layout = RenderOnAPhone();

        layout.FindAll("nav.app-answer-sheet").Should().BeEmpty("the drawer is closed");
        layout.FindAll(".app-session-aside").Should().BeEmpty();
        var button = layout.Find(".app-session-sheet-button");
        button.GetAttribute("aria-label").Should().Be("Answer sheet (0 of 6 answered)");

        button.Click();

        layout.WaitForAssertion(() => layout.FindAll("nav.app-answer-sheet").Should().ContainSingle());
        layout.Find(".app-answer-sheet-close").GetAttribute("aria-label").Should().Be("Close the answer sheet");
        layout.FindAll(".app-answer-cell").Should().HaveCount(6);

        layout.Find(".app-answer-sheet-close").Click();

        layout.WaitForAssertion(() => layout.FindAll("nav.app-answer-sheet").Should().BeEmpty());
    }

    [Fact]
    public void AClickOnACell_ShowsItsQuestion_AndClosesTheDrawer()
    {
        var layout = RenderOnAPhone();
        layout.Find(".app-session-sheet-button").Click();
        layout.WaitForAssertion(() => layout.FindAll(".app-answer-cell").Should().HaveCount(6));

        layout.FindAll(".app-answer-cell").Single(cell => cell.TextContent.Trim() == "6").Click();

        layout.WaitForAssertion(() =>
        {
            Heading(layout).Should().Be("Question 6");
            layout.FindAll("nav.app-answer-sheet").Should().BeEmpty();
        });
    }

    // The drawer closes on Esc.
    [Fact]
    public void TheDrawer_ClosesOnEscape()
    {
        var layout = RenderOnAPhone();
        layout.Find(".app-session-sheet-button").Click();
        layout.WaitForAssertion(() => layout.FindAll("nav.app-answer-sheet").Should().ContainSingle());

        layout.Find("nav.app-answer-sheet").ParentElement!.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        layout.WaitForAssertion(() => layout.FindAll("nav.app-answer-sheet").Should().BeEmpty());
    }

    // On a phone the way out is the back icon, named and with its tooltip text from the same resource.
    [Fact]
    public void TheWayOut_IsAnIconNamedLeaveAndContinueLater()
    {
        var layout = RenderOnAPhone();

        layout.FindAll("a.app-session-leave").Should().BeEmpty();
        var leave = layout.Find("a.app-session-leave-icon");
        leave.GetAttribute("aria-label").Should().Be("Leave and continue later");
        leave.GetAttribute("href").Should().Be($"/catalog/exams/{AttemptSamples.ExamId}");
    }
}
