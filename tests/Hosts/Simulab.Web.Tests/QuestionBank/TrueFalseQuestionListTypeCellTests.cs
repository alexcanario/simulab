using Bunit;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Pages.QuestionBank;

namespace Simulab.Web.Tests.QuestionBank;

/// <summary>
/// F-97 AC9, found on screen: the type chip of the list is chip text that must not break letter by letter in a
/// narrow column. bUnit cannot measure layout, so it asserts the chip sits in the one-line cell wrapper and that
/// the stylesheet gives that wrapper its no-wrap rule.
/// </summary>
public sealed class TrueFalseQuestionListTypeCellTests : QuestionPageTestContext
{
    [Fact]
    public void List_TheTypeChip_SitsInAOneLineCell()
    {
        Api.Questions.Add(Draft);

        var page = Render<Questions>();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(1));
        page.Find("tbody tr td .app-cell-nowrap .app-chip").TextContent.Trim().Should().Be("Single choice");
    }

    [Fact]
    public void TheStylesheet_KeepsAChipOfAOneLineCellOnOneLine()
    {
        var css = File.ReadAllText(Path.Combine(FindWebRoot(), "wwwroot", "app.css"));

        css.Should().Contain(".app-cell-nowrap .app-chip");
        css.Should().Contain("white-space: nowrap");
        css.Should().Contain(".app-cell-nowrap .app-chip-text");
    }

    private static string FindWebRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Simulab.slnx")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory!.FullName, "src", "Hosts", "Simulab.Web");
    }
}
