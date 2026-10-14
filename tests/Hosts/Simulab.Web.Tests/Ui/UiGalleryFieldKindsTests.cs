using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MudBlazor;
using Simulab.Web.Components.Pages.Dev;

namespace Simulab.Web.Tests.Ui;

/// <summary>F-114 AC12: the gallery shows the three field kinds, each with the states it has.</summary>
public sealed class UiGalleryFieldKindsTests : KitTestContext
{
    private IRenderedComponent<UiGallery> RenderGallery()
    {
        Services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(Environments.Development));
        Render<MudPopoverProvider>();

        return Render<UiGallery>(parameters => parameters.Add(page => page.SourceDelay, TimeSpan.Zero));
    }

    [Fact]
    public void Render_Development_FieldKindsSectionNamesTheThreeKinds()
    {
        var gallery = RenderGallery();

        var section = gallery.Find("#gallery-field-kinds");
        section.TextContent.Should().Contain("Arrow combobox (AppSelectField)");
        section.TextContent.Should().Contain("Magnifier lookup (AppLookupField)");
        section.TextContent.Should().Contain("Free text (AppTextField)");
    }

    [Fact]
    public void Render_Development_FieldKindsSectionShowsTheArrowAndTheMagnifier()
    {
        var gallery = RenderGallery();

        var section = gallery.Find("#gallery-field-kinds");
        section.QuerySelectorAll("button[aria-label='Show the options']").Should().NotBeEmpty("the arrow kind");
        section.QuerySelectorAll("button[aria-label='Search']").Should().NotBeEmpty("the magnifier kind");
    }

    [Theory]
    [InlineData("Empty")]
    [InlineData("With value")]
    [InlineData("Disabled")]
    [InlineData("Error")]
    [InlineData("Long text")]
    [InlineData("Loading")]
    [InlineData("Open list")]
    [InlineData("No results")]
    [InlineData("Leading icon")]
    public void Render_Development_FieldKindsSectionCaptionsEveryState(string state)
    {
        var gallery = RenderGallery();

        gallery.Find("#gallery-field-kinds").TextContent.Should().Contain(state);
    }
}
