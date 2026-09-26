using Bunit;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>F-43 BR1: fields side by side inside a section, so a short field does not own a whole row (UC1).</summary>
public sealed class AppFormGridTests : KitTestContext
{
    private IRenderedComponent<AppFormGrid> Render(int columns) =>
        Render<AppFormGrid>(parameters => parameters
            .Add(grid => grid.Columns, columns)
            .Add(grid => grid.ChildContent, builder => builder.AddMarkupContent(0, "<p>a field</p>")));

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Render_TheColumnCountIsOnTheGrid(int columns)
    {
        var grid = Render(columns);

        grid.Find("div.app-form-grid").ClassList.Should().Contain($"app-form-grid-{columns}");
    }

    [Fact]
    public void Render_TheChildrenAreInside()
    {
        Render(2).Find(".app-form-grid").InnerHtml.Should().Contain("a field");
    }

    /// <summary>A count the CSS has no rule for would silently render as one column, which is not a layout choice.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Render_AColumnCountTheGridDoesNotHave_Fails(int columns)
    {
        var render = () => Render(columns);

        render.Should().Throw<ArgumentOutOfRangeException>();
    }
}
