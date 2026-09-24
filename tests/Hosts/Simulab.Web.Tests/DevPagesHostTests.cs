using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Simulab.Web.Tests;

/// <summary>Through the real Web host: /dev pages exist only in Development.</summary>
public class DevPagesHostTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private HttpClient ClientFor(string environment) =>
        factory.WithWebHostBuilder(builder => builder.UseEnvironment(environment))
            .CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task Get_GalleryOutsideDevelopment_Returns404(string environment)
    {
        var response = await ClientFor(environment).GetAsync("/dev/ui");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("gallery-table");
    }

    /// <summary>F-41 AC9: the diagnostics page is not reachable outside Development either.</summary>
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task Get_AiDiagnosticsOutsideDevelopment_Returns404(string environment)
    {
        var response = await ClientFor(environment).GetAsync("/dev/ai");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("ai-prompt");
    }

    [Fact]
    public async Task Get_GalleryInDevelopment_RendersEverySection()
    {
        var response = await ClientFor(Environments.Development).GetAsync("/dev/ui");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("UI kit gallery");
        foreach (var section in GallerySections.All)
        {
            html.Should().Contain($"id=\"{section}\"");
        }
    }
}
