using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MudBlazor;
using Simulab.Web.Components.Pages.Dev;

namespace Simulab.Web.Tests.Ui;

/// <summary>F-181 AC9: the gallery shows the number controls, each in the states of the shared contract.</summary>
public sealed class UiGalleryNumbersTests : KitTestContext
{
    private IRenderedComponent<UiGallery> RenderGallery()
    {
        Services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(Environments.Development));
        Render<MudPopoverProvider>();

        return Render<UiGallery>(parameters => parameters.Add(page => page.SourceDelay, TimeSpan.Zero));
    }

    [Theory]
    [InlineData("Integer (AppIntegerField)")]
    [InlineData("Decimal (AppDecimalField)")]
    [InlineData("Score (AppScoreField)")]
    [InlineData("Year (AppYearField)")]
    [InlineData("Number and score text (AppNumberText, AppScoreText)")]
    public void Render_Development_NumbersSectionNamesEachKind(string kind)
    {
        var gallery = RenderGallery();

        gallery.Find("#gallery-numbers").TextContent.Should().Contain(kind);
    }

    [Theory]
    [InlineData("Empty")]
    [InlineData("Filled")]
    [InlineData("Error")]
    [InlineData("Disabled")]
    [InlineData("Read-only")]
    [InlineData("Long label")]
    [InlineData("With stepper")]
    public void Render_Development_NumbersSectionShowsEachState(string state)
    {
        var gallery = RenderGallery();

        gallery.Find("#gallery-numbers").TextContent.Should().Contain(state);
    }

    [Fact]
    public void Render_Development_NumbersSectionUsesTheKitControls()
    {
        var gallery = RenderGallery();

        var section = gallery.Find("#gallery-numbers");
        section.QuerySelectorAll(".app-integer-field").Should().NotBeEmpty();
        section.QuerySelectorAll(".app-decimal-field").Should().NotBeEmpty();
        section.QuerySelectorAll(".app-score-field").Should().NotBeEmpty();
        section.QuerySelectorAll(".app-year-field").Should().NotBeEmpty();
        section.QuerySelectorAll(".app-number-step").Should().NotBeEmpty();
        section.QuerySelectorAll("input[type=number]").Should().BeEmpty("a number is a text input with a numeric keyboard (BR2)");
    }
}
