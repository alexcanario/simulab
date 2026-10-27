using Bunit;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-169: the kit's answer sheet. A nav landmark named by its heading, the progress, a legend, and one group per section
/// with a heading, its range and its cells; a cell is a button named by its number and state.
/// </summary>
public sealed class AppAnswerSheetTests : KitTestContext
{
    private static readonly Guid First = Guid.Parse("0198f0a3-6000-7000-8000-000000000001");
    private static readonly Guid Second = Guid.Parse("0198f0a3-6000-7000-8000-000000000002");
    private static readonly Guid Third = Guid.Parse("0198f0a3-6000-7000-8000-000000000003");

    private readonly List<Guid> _selected = [];
    private int _closed;

    private static IReadOnlyList<AppAnswerSheetSection> Sections() =>
    [
        new("Língua Portuguesa", "pt-BR", 1, 2, [new(First, 1, true), new(Second, 2, false)]),
        new("Outras", null, 4, 4, [new(Third, 4, false)])
    ];

    private IRenderedComponent<AppAnswerSheet> RenderSheet(IReadOnlyList<AppAnswerSheetSection>? sections = null, Guid? current = null, bool closable = false) =>
        Render<AppAnswerSheet>(parameters =>
        {
            parameters
                .Add(sheet => sheet.Id, "sheet")
                .Add(sheet => sheet.Sections, sections ?? Sections())
                .Add(sheet => sheet.Total, 3)
                .Add(sheet => sheet.AnsweredCount, 1)
                .Add(sheet => sheet.CurrentId, current)
                .Add(sheet => sheet.OnSelect, (Guid id) => _selected.Add(id));
            if (closable)
            {
                parameters.Add(sheet => sheet.OnClose, () => _closed++);
            }
        });

    [Fact]
    public void Render_IsANavNamedByItsHeading_WithTheProgressAndTheLegend()
    {
        var sheet = RenderSheet();

        var nav = sheet.Find("nav");
        nav.GetAttribute("aria-labelledby").Should().Be("sheet-title");
        sheet.Find("#sheet-title").TextContent.Should().Be("Answer sheet");
        sheet.Find("h2").Id.Should().Be("sheet-title");
        sheet.Find(".app-answer-sheet-progress").TextContent.Should().Be("1 of 3 answered");
        sheet.Find(".app-answer-legend").TextContent.Should().Contain("Blank").And.Contain("Answered").And.Contain("Current");
    }

    // BR3, BR4: one h3 per section with its label (in the content language) and its range; the cells in a list under it.
    [Fact]
    public void Render_GroupsTheCellsBySection_EachWithItsHeadingAndRange()
    {
        var sheet = RenderSheet();

        sheet.FindAll("h3").Should().HaveCount(2);
        sheet.FindAll(".app-answer-sheet-section-label").Select(label => label.TextContent).Should().Equal("Língua Portuguesa", "Outras");
        sheet.FindAll(".app-answer-sheet-section-label")[0].GetAttribute("lang").Should().Be("pt-BR");
        sheet.FindAll(".app-answer-sheet-section-label")[1].HasAttribute("lang").Should().BeFalse();
        sheet.FindAll(".app-answer-sheet-range").Select(range => range.TextContent).Should().Equal("1–2", "4–4");
        sheet.FindAll("ul.app-answer-cells").Should().HaveCount(2);
        sheet.FindAll(".app-answer-cell").Select(cell => cell.TextContent.Trim()).Should().Equal("1", "2", "4");
    }

    // An answered cell is told by a check and a fill; the name of every cell says its state; the current one is the step.
    [Fact]
    public void Cells_AreNamedByNumberAndState_TheAnsweredOneHasACheck_AndTheCurrentOneIsTheStep()
    {
        var sheet = RenderSheet(current: Second);

        var cells = sheet.FindAll(".app-answer-cell");
        cells.Select(cell => cell.GetAttribute("aria-label")).Should().Equal("Question 1: Answered", "Question 2: Blank", "Question 4: Blank");
        cells[0].ClassList.Should().Contain("app-answer-cell-answered");
        cells[0].QuerySelector(".app-answer-cell-check").Should().NotBeNull("answered is not colour alone");
        cells[1].QuerySelector(".app-answer-cell-check").Should().BeNull();
        sheet.FindAll("[aria-current=step]").Should().ContainSingle().Which.GetAttribute("aria-label").Should().Be("Question 2: Blank");
        cells.Should().OnlyContain(cell => cell.TagName == "BUTTON");
    }

    [Fact]
    public void AClickOnACell_HandsBackItsId()
    {
        var sheet = RenderSheet();

        sheet.FindAll(".app-answer-cell")[2].Click();

        _selected.Should().Equal(Third);
    }

    // The phone's drawer passes OnClose and gets the close button, named and with a tooltip; the side panel has none.
    [Fact]
    public void TheCloseButton_ShowsOnlyWhenAHandlerIsGiven()
    {
        RenderSheet().FindAll(".app-answer-sheet-close").Should().BeEmpty();

        var sheet = RenderSheet(closable: true);

        var close = sheet.Find(".app-answer-sheet-close");
        close.GetAttribute("aria-label").Should().Be("Close the answer sheet");
        close.Click();
        _closed.Should().Be(1);
    }

    // Long text: a section heading wider than the panel wraps inside its label, which has no fixed width; the range stays.
    [Fact]
    public void LongText_ASectionHeadingWrapsInItsLabel()
    {
        var longHeading = string.Join(' ', Enumerable.Repeat("Legislação", 30));
        IReadOnlyList<AppAnswerSheetSection> sections = [new(longHeading, "pt-BR", 1, 2, [new(First, 1, false)])];

        var sheet = RenderSheet(sections);

        var label = sheet.Find(".app-answer-sheet-section-label");
        label.TextContent.Should().Be(longHeading);
        label.HasAttribute("style").Should().BeFalse();
        sheet.Find(".app-answer-sheet-range").TextContent.Should().Be("1–2");
    }
}
