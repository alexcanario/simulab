using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Simulab.Web.Tests;

/// <summary>
/// F-94 BR6: the Web refuses to start outside Development without the client id and secret it sends to the token
/// endpoint, and names the key. Before, it started and every sign-in got a 401 <c>invalid_client</c> (F-64 staging).
/// </summary>
public class OpenIddictClientStartTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private HttpClient ClientFor(string environment, string? clientId, string? clientSecret) =>
        factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                // Pinned, so a variable of the machine running the test changes nothing.
                builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Authentication:OpenIddict:ClientId"] = clientId,
                        ["Authentication:OpenIddict:ClientSecret"] = clientSecret,
                    }));
            })
            .CreateClient();

    /// <summary>AC5.</summary>
    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Start_OutsideDevelopmentWithoutClientId_RefusesAndNamesTheKey(string environment)
    {
        var start = () => ClientFor(environment, clientId: null, clientSecret: "a-secret");

        start.Should().Throw<OptionsValidationException>()
            .Which.Message.Should().Contain("Authentication:OpenIddict:ClientId");
    }

    /// <summary>AC5: the secret is checked the same way.</summary>
    [Fact]
    public void Start_OutsideDevelopmentWithoutClientSecret_RefusesAndNamesTheKey()
    {
        var start = () => ClientFor("Staging", clientId: "simulab-web", clientSecret: null);

        start.Should().Throw<OptionsValidationException>()
            .Which.Message.Should().Contain("Authentication:OpenIddict:ClientSecret");
    }

    [Fact]
    public void Start_OutsideDevelopmentWithBothKeys_Starts()
    {
        using var client = ClientFor("Staging", clientId: "simulab-web", clientSecret: "a-secret");

        client.Should().NotBeNull();
    }

    /// <summary>AC6: Development takes both keys from <c>appsettings.Development.json</c>, so it needs no setting of its own.</summary>
    [Fact]
    public void Start_InDevelopmentWithTheCommittedSettings_Starts()
    {
        using var client = factory.WithWebHostBuilder(builder => builder.UseEnvironment(Environments.Development)).CreateClient();

        client.Should().NotBeNull();
    }
}
