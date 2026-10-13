using Bunit;
using Simulab.Web.Components.Pages.QuestionBank;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-100, found on screen during the validation: the six filters did not wrap and the last one spilled out of the
/// card, and the edition field said "Type at least 0 characters". bUnit does not lay anything out; what it can pin
/// is that the filters sit in the grid that wraps them and that every one of them is inside it.
/// </summary>
public sealed class QuestionsFilterLayoutTests : QuestionPageTestContext
{
    [Fact]
    public void Toolbar_EverySearchFilterSitsInsideTheGridThatWrapsThem()
    {
        var page = Render<Questions>();
        page.WaitForAssertion(() => page.FindAll(".app-table-filters-grid").Should().ContainSingle());

        var grid = page.Find(".app-table-filters-grid");

        foreach (var id in new[]
                 {
                     "questions-filter-status", "questions-filter-type", "questions-filter-difficulty",
                     "questions-filter-subject", "questions-filter-organizer", "questions-filter-edition"
                 })
        {
            grid.QuerySelectorAll($"#{id}").Should().ContainSingle($"{id} is a filter of the grid");
        }
    }

    // The wide content area is the page's root element, so the layout's container sees it as its direct child.
    [Fact]
    public void Page_RootAsksForTheWideContentArea_AndHoldsTheWholePage()
    {
        var page = Render<Questions>();
        page.WaitForAssertion(() => page.FindAll(".app-page-wide").Should().ContainSingle());

        var root = page.Find(".app-page-wide");

        page.Markup.TrimStart().Should().StartWith("<div class=\"app-page-wide\"", "the marked element is the root of the page");
        root.QuerySelector("h1").Should().NotBeNull();
        root.QuerySelector(".app-table-filters-grid").Should().NotBeNull();
    }

    [Fact]
    public void Edition_SaysWhatIsListed_NotThatZeroCharactersAreNeeded()
    {
        var page = Render<Questions>();
        page.WaitForAssertion(() => page.FindAll("#questions-filter-edition").Should().ContainSingle());

        var description = page.Find("#questions-filter-edition-description").TextContent;

        description.Should().Contain("Drafts and published editions are listed.").And.NotContain("at least 0");
    }
}
