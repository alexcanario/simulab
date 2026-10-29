using System.Globalization;
using Bunit;
using MudBlazor;
using Simulab.Web.Components.Pages.Dev;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>F-181 AC7, BR7: a number as text, and a score as text with its unit.</summary>
public sealed class NumberTextTests : KitTestContext
{
    [Fact]
    public void NumberText_UsesTheCultureAndTheDecimals()
    {
        CultureInfo.CurrentCulture = new CultureInfo("pt-BR");

        var text = Render<AppNumberText>(parameters => parameters
            .Add(component => component.Value, 1234.5m)
            .Add(component => component.Decimals, 1));

        text.Find(".app-number-text").TextContent.Should().Be("1.234,5");
    }

    [Fact]
    public void NumberText_NoValue_IsADash()
    {
        var text = Render<AppNumberText>(parameters => parameters.Add(component => component.Value, (decimal?)null));

        text.Find(".app-number-text").TextContent.Should().Be("—");
    }

    [Fact]
    public void ScoreText_ShowsTheValueAndThePointsUnit()
    {
        CultureInfo.CurrentCulture = new CultureInfo("pt-BR");

        var text = Render<AppScoreText>(parameters => parameters
            .Add(component => component.Value, 2.5m)
            .Add(component => component.Decimals, 1));

        text.Find(".app-score-text").TextContent.Should().Be("2,5 pts");
    }

    [Fact]
    public void ScoreText_NoValue_IsADashWithoutTheUnit()
    {
        var text = Render<AppScoreText>(parameters => parameters
            .Add(component => component.Value, (decimal?)null)
            .Add(component => component.Decimals, 1));

        text.Find(".app-score-text").TextContent.Should().Be("—");
    }
}

/// <summary>F-181 AC8, BR7: the score column is the number column with the decimals the meaning asks for.</summary>
public sealed class AppScoreColumnTests : KitTestContext
{
    [Fact]
    public void Column_RightAlignsTheCellAndFormatsWithItsDecimals()
    {
        Render<MudPopoverProvider>();
        var source = new GallerySource(number => new GallerySample(number, $"Item {number}", "d", number, 2.5m, DateTimeOffset.UnixEpoch), TimeSpan.Zero);

        var table = Render<AppDataTable<GallerySample>>(parameters => parameters
            .Add(p => p.LoadAsync, (query, token) => source.LoadAsync(query, token))
            .Add(p => p.EmptyActionText, "Add")
            .Add(p => p.OnEmptyAction, () => { })
            .Add(p => p.Columns, builder =>
            {
                builder.OpenComponent<AppScoreColumn<GallerySample, decimal>>(0);
                builder.AddComponentParameter(1, nameof(AppScoreColumn<GallerySample, decimal>.Property),
                    (System.Linq.Expressions.Expression<Func<GallerySample, decimal>>)(x => x.Score));
                builder.AddComponentParameter(2, nameof(AppScoreColumn<GallerySample, decimal>.Decimals), 2);
                builder.CloseComponent();
            }));

        table.WaitForAssertion(() => table.FindAll("tbody td.app-cell-number").Should().NotBeEmpty());
        table.Find("tbody td.app-cell-number").TextContent.Trim().Should().Be("2.50");
    }
}
