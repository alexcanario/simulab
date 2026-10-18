using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.Layout;

/// <summary>
/// F-125, through the real Web host: the fonts and the brand files are served by the app (AC8), the page head links
/// the Control Alt Systems app icon as the favicon (AC10), and no page asks another host for a font.
/// </summary>
public class BrandAssetsHostTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private HttpClient Client() =>
        factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development").WithDeployedClient()).CreateClient();

    public static TheoryData<string> Fonts() => new(BrandFontTests.Faces.Select(face => face.File));

    [Theory]
    [MemberData(nameof(Fonts))]
    public async Task Get_Font_IsServedByTheAppAsWoff2(string file)
    {
        var response = await Client().GetAsync($"/fonts/{file}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("font/woff2");
    }

    [Theory]
    [InlineData("/brand/app-icon.svg")]
    [InlineData("/brand/keys-symbol.svg")]
    [InlineData("/brand/keys-symbol-dark.svg")]
    public async Task Get_BrandFile_IsServedAsSvg(string path)
    {
        var response = await Client().GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/svg+xml");
    }

    [Fact]
    public async Task Get_Home_LinksTheAppIconAsTheFaviconAndNoFontHost()
    {
        var html = await Client().GetStringAsync("/");

        // Blazor writes the '+' of the media type as &#x2B;; the browser reads both the same.
        html.Should().MatchRegex(@"<link rel=""icon"" type=""image/svg(\+|&#x2B;)xml"" href=""brand/app-icon[^""]*\.svg""");
        html.Should().NotContainAny("fonts.googleapis", "fonts.gstatic");
    }
}
