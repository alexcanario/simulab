using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using MudBlazor;
using Simulab.Web.Components.Pages.Dev;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

public class UiGalleryTests : KitTestContext
{
    private sealed class FakeEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Simulab.Web";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private IRenderedComponent<UiGallery> RenderGallery(string environment, Action? beforeRender = null)
    {
        Services.AddSingleton<IHostEnvironment>(new FakeEnvironment(environment));
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
            gallery.FindAll($"#{section}").Should().ContainSingle(section);
        gallery.WaitForAssertion(() => gallery.FindAll("#gallery-table tbody tr.mud-table-row").Should().HaveCount(25));
        gallery.Markup.Should().Contain("AppIcons.Edit");
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
    public void ThemeSwitch_Clicked_TogglesDarkMode()
    {
        var gallery = RenderGallery(Environments.Development);
        var theme = Services.GetRequiredService<ThemeState>();

        gallery.Find(".gallery-theme-switch").Click();

        theme.IsDarkMode.Should().BeTrue();
        gallery.Find(".gallery-theme-switch").GetAttribute("aria-label").Should().Be("Switch to light mode");
    }
}
