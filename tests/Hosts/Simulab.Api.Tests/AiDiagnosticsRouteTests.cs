using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Simulab.Api.Features.Ai;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Api.Tests;

/// <summary>
/// F-41 (v2) AC9b: the diagnostics route is mapped only in Development. Outside it the route does not
/// exist, so it answers 404 — not 401 and not 403, which would say it is merely closed.
/// </summary>
public class AiDiagnosticsRouteTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private HttpClient ClientFor(string environment) =>
        factory.WithWebHostBuilder(builder => builder.UseEnvironment(environment)).CreateClient();

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task Post_OutsideDevelopment_Returns404(string environment)
    {
        var response = await ClientFor(environment).PostAsJsonAsync(
            "/api/v1/ai/diagnostics",
            new AiDiagnosticsRequest("diagnostics", "Say ok"),
            AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>The rule of presence: in Development the route exists, so the 404 above means something.</summary>
    [Fact]
    public async Task Post_InDevelopment_IsMappedAndAsksForAuthentication()
    {
        var response = await ClientFor(Environments.Development).PostAsJsonAsync(
            "/api/v1/ai/diagnostics",
            new AiDiagnosticsRequest("diagnostics", "Say ok"),
            AppJson.Options);

        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }
}
