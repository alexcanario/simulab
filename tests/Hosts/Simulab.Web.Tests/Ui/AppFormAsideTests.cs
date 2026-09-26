using Bunit;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-43 BR1, BR4 and UC5: the read-only summary beside a long form. It carries no button — the page's only
/// primary action is Save, at the bottom right of the form card — and it is not a live region, so it does not
/// talk over the reader on every keystroke.
/// </summary>
public sealed class AppFormAsideTests : KitTestContext
{
    private IRenderedComponent<AppFormAside> Render(string? filled = "PMF") =>
        Render<AppFormAside>(parameters => parameters
            .Add(aside => aside.Title, "Summary")
            .Add(aside => aside.EmptyText, "Not filled")
            .Add(aside => aside.Rows,
            [
                new AppAsideRow("Issuing authority", filled),
                new AppAsideRow("Name", null)
            ])
            .Add(aside => aside.ChecklistTitle, "Before saving")
            .Add(aside => aside.DoneText, "done")
            .Add(aside => aside.PendingText, "still missing")
            .Add(aside => aside.Checklist,
            [
                new AppChecklistItem("Issuing authority", true),
                new AppChecklistItem("Name", false)
            ]));

    [Fact]
    public void Render_TheAsideIsNamedByItsOwnTitle()
    {
        var aside = Render();

        var element = aside.Find("aside.app-form-aside");
        element.GetAttribute("aria-labelledby").Should().Be(aside.Find("h2.app-form-aside-title").Id);
    }

    [Fact]
    public void Render_AnEmptyRowSaysSoInsteadOfBeingBlank()
    {
        var aside = Render();

        var values = aside.FindAll(".app-form-aside-row-value");
        values[0].TextContent.Trim().Should().Be("PMF");
        values[1].TextContent.Trim().Should().Be("Not filled");
        values[1].ClassList.Should().Contain("app-form-aside-row-empty");
    }

    /// <summary>A tick is not a word: a screen reader has to hear whether the item is done.</summary>
    [Fact]
    public void Render_EachChecklistItemSaysItsStateInWords()
    {
        var aside = Render();

        aside.FindAll(".app-form-aside-checklist .app-visually-hidden")
            .Select(hidden => hidden.TextContent)
            .Should().Equal("done", "still missing");
    }

    [Fact]
    public void Render_TheAsideHasNoButtonAndDoesNotAnnounceItself()
    {
        var aside = Render();

        aside.FindAll("button").Should().BeEmpty();
        aside.Find("aside.app-form-aside").GetAttribute("aria-live").Should().BeNull();
    }
}
