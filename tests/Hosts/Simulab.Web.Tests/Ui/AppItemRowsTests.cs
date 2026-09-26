using Bunit;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-43 BR1: the child items of the record being edited. Until F-35 brings the editions it lives only in the
/// gallery (owner, 2026-09-26), so these tests are the whole of its cover.
/// </summary>
public sealed class AppItemRowsTests : KitTestContext
{
    private IRenderedComponent<AppItemRows<string>> Render(IReadOnlyList<string> items, bool withActions = true) =>
        Render<AppItemRows<string>>(parameters => parameters
            .Add(rows => rows.Items, items)
            .Add(rows => rows.EmptyMessage, "No edition yet.")
            .Add(rows => rows.RowTemplate, (string item) => builder => builder.AddMarkupContent(0, $"<span>{item}</span>"))
            .Add(rows => rows.Actions, withActions
                ? (string item) => builder => builder.AddMarkupContent(0, $"<button>Edit {item}</button>")
                : null));

    [Fact]
    public void Render_Empty_ShowsTheMessageThePageChose()
    {
        var rows = Render([]);

        rows.Find(".app-item-rows-empty").TextContent.Trim().Should().Be("No edition yet.");
        rows.FindAll("li").Should().BeEmpty();
    }

    [Fact]
    public void Render_OneRowPerItem_InTheOrderGiven()
    {
        var rows = Render(["2026", "2023"]);

        rows.FindAll(".app-item-row-content").Select(row => row.TextContent).Should().Equal("2026", "2023");
    }

    [Fact]
    public void Render_EachRowCarriesItsOwnActions()
    {
        var rows = Render(["2026", "2023"]);

        rows.FindAll(".app-item-row-actions button").Select(button => button.TextContent)
            .Should().Equal("Edit 2026", "Edit 2023");
    }

    [Fact]
    public void Render_WithoutActions_NoEmptyActionCellIsLeftBehind()
    {
        Render(["2026"], withActions: false).FindAll(".app-item-row-actions").Should().BeEmpty();
    }
}
