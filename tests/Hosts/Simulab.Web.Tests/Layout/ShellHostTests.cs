using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Simulab.Web.Theme;

namespace Simulab.Web.Tests.Layout;

/// <summary>Through the real Web host: the first response already reflects the preference cookies.</summary>
public class ShellHostTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private async Task<string> GetHomeAsync(string environment = "Development", string? cookie = null)
    {
        var client = factory.WithWebHostBuilder(builder => builder.UseEnvironment(environment)).CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        if (cookie is not null)
        {
            request.Headers.Add("Cookie", cookie);
        }

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.Content.ReadAsStringAsync();
    }

    private static readonly string DarkBackground = SimulabTheme.Create().PaletteDark.Background.ToString(MudBlazor.Utilities.MudColorOutputFormats.RGBA);
    private static readonly string LightBackground = SimulabTheme.Create().PaletteLight.Background.ToString(MudBlazor.Utilities.MudColorOutputFormats.RGBA);

    [Fact]
    public async Task Get_DarkThemeCookie_RendersDarkPalette()
    {
        var html = await GetHomeAsync(cookie: "simulab.theme=dark");

        html.Should().Contain($"--mud-palette-background: {DarkBackground}", Exactly.Once());
    }

    [Fact]
    public async Task Get_NoThemeCookie_RendersLightPaletteFirst()
    {
        var html = await GetHomeAsync();

        html.Should().Contain($"--mud-palette-background: {LightBackground}", Exactly.Once());
    }

    [Fact]
    public async Task Get_CollapsedNavigationCookie_RendersMenuCollapsed()
    {
        var collapsed = await GetHomeAsync(cookie: "simulab.nav=collapsed");
        var expanded = await GetHomeAsync();

        collapsed.Should().Contain("Expand menu").And.NotContain("mud-drawer--open");
        expanded.Should().Contain("Collapse menu").And.Contain("mud-drawer--open");
    }

    [Fact]
    public async Task Get_Home_SkipLinkComesFirstAndMainExists()
    {
        var html = await GetHomeAsync();

        var body = html[html.IndexOf("<body", StringComparison.Ordinal)..];
        body.IndexOf("app-skip-link", StringComparison.Ordinal).Should().BeLessThan(body.IndexOf("<button", StringComparison.Ordinal));
        body.Should().Contain("id=\"main-content\"").And.Contain("href=\"#main-content\"");
        html.Should().Contain("js/shell");
    }

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Production", false)]
    public async Task Get_Home_DevelopmentSectionOnlyInDevelopment(string environment, bool expected)
    {
        var html = await GetHomeAsync(environment);

        html.Contains("data-route=\"/dev/ui\"", StringComparison.Ordinal).Should().Be(expected);
        html.Should().Contain("data-route=\"/\"");
    }
}
