using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MudBlazor;
using Simulab.Web.Components.Pages.Dev;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>F-183 AC11: the gallery shows the six text kinds, each in the states of the shared contract.</summary>
public sealed class UiGalleryTextKindsTests : KitTestContext
{
    private IRenderedComponent<UiGallery> RenderGallery()
    {
        Services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(Environments.Development));
        Render<MudPopoverProvider>();

        return Render<UiGallery>(parameters => parameters.Add(page => page.SourceDelay, TimeSpan.Zero));
    }

    [Theory]
    [InlineData("Text area (AppTextAreaField)")]
    [InlineData("E-mail (AppEmailField)")]
    [InlineData("Web address (AppUrlField)")]
    [InlineData("Code (AppCodeField)")]
    [InlineData("One-time code (AppOtpField)")]
    [InlineData("Search (AppSearchField)")]
    public void Render_Development_TextKindsSectionNamesEachKind(string kind)
    {
        var gallery = RenderGallery();

        gallery.Find("#gallery-text-kinds").TextContent.Should().Contain(kind);
    }

    [Theory]
    [InlineData("Empty")]
    [InlineData("Filled")]
    [InlineData("Error")]
    [InlineData("Disabled")]
    [InlineData("Read-only")]
    [InlineData("Long value")]
    public void Render_Development_TextKindsSectionShowsEachState(string state)
    {
        var gallery = RenderGallery();

        gallery.Find("#gallery-text-kinds").TextContent.Should().Contain(state);
    }

    [Fact]
    public void Render_Development_TextKindsSectionUsesTheKitControls()
    {
        var gallery = RenderGallery();

        var section = gallery.Find("#gallery-text-kinds");
        section.QuerySelectorAll(".app-text-area-field").Should().NotBeEmpty();
        section.QuerySelectorAll(".app-email-field").Should().NotBeEmpty();
        section.QuerySelectorAll(".app-url-field").Should().NotBeEmpty();
        section.QuerySelectorAll(".app-code-field").Should().NotBeEmpty();
        section.QuerySelectorAll(".app-otp-field").Should().NotBeEmpty();
        section.QuerySelectorAll(".app-search-field").Should().NotBeEmpty();
        gallery.FindComponents<AppOtpField>().Should().NotBeEmpty();
    }
}
