using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MudBlazor;
using Simulab.Web.Components.Pages.Dev;

namespace Simulab.Web.Tests.Ui;

public class UiGalleryTests : KitTestContext
{
    private IRenderedComponent<UiGallery> RenderGallery(string environment, Action? beforeRender = null)
    {
        Services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(environment));
        beforeRender?.Invoke();
        Render<MudPopoverProvider>();
        return Render<UiGallery>(parameters => parameters.Add(p => p.SourceDelay, TimeSpan.Zero));
    }

    [Fact]
    public void Render_Development_ShowsHeaderAndEverySection()
    {
        var gallery = RenderGallery(Environments.Development);

        gallery.Find("h1").TextContent.Should().Be("UI kit gallery");
        foreach (var section in GallerySections.All)
        {
            gallery.FindAll($"#{section}").Should().ContainSingle(section);
        }

        gallery.WaitForAssertion(() => gallery.FindAll("#gallery-table tbody tr.mud-table-row").Should().HaveCount(25));
        gallery.Markup.Should().Contain("AppIcons.Edit");
    }

    /// <summary>
    /// F-34: an id that appears twice breaks every label and aria-describedby that points at it, and the
    /// browser silently resolves to the first one. Found on screen, when a section id repeated a field id.
    /// </summary>
    [Fact]
    public void Render_Development_EveryIdOnThePageIsUnique()
    {
        var gallery = RenderGallery(Environments.Development);

        var duplicates = gallery.FindAll("[id]")
            .Select(element => element.Id)
            .Where(id => !string.IsNullOrEmpty(id))
            .GroupBy(id => id!, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        duplicates.Should().BeEmpty();
    }

    [Fact]
    public void Render_Production_RendersNothingAndSignalsNotFound()
    {
        var notFound = false;
        var gallery = RenderGallery(Environments.Production, () =>
            Services.GetRequiredService<Bunit.TestDoubles.BunitNavigationManager>().OnNotFound += (_, _) => notFound = true);

        gallery.Markup.Trim().Should().BeEmpty();
        notFound.Should().BeTrue();
    }

    [Fact]
    public void Render_Development_HasNoThemeSwitchOfItsOwn()
    {
        var gallery = RenderGallery(Environments.Development);

        gallery.FindAll(".gallery-theme-switch, .app-theme-switch").Should().BeEmpty();
        gallery.Markup.Should().NotContain("Switch to dark mode");
    }
}
