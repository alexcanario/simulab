using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Simulab.Web.Services.Auth;

namespace Simulab.Web.Tests;

/// <summary>
/// F-94 BR6: the Web refuses to start outside Development without the client id and secret it sends to the token
/// endpoint, and names the key. Before, it started and every sign-in got a 401 <c>invalid_client</c> (F-64 staging).
/// The refusal is checked on the registration the host uses, through <see cref="IStartupValidator"/>, which is what the
/// host runs at start: a failed start of a <c>WebApplicationFactory</c> sometimes ends in an
/// <see cref="ObjectDisposedException"/> raised inside the framework, so it cannot carry an assertion.
/// </summary>
public class OpenIddictClientStartTests
{
    private static void ValidateAtStart(string? clientId, string? clientSecret)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:OpenIddict:ClientId"] = clientId,
                ["Authentication:OpenIddict:ClientSecret"] = clientSecret,
            })
            .Build();
        using var provider = new ServiceCollection().AddOpenIddictClientOptions(configuration).BuildServiceProvider();

        provider.GetRequiredService<IStartupValidator>().Validate();
    }

    /// <summary>AC5.</summary>
    [Fact]
    public void Start_WithoutClientId_RefusesAndNamesTheKey()
    {
        var start = () => ValidateAtStart(clientId: null, clientSecret: "a-secret");

        start.Should().Throw<OptionsValidationException>()
            .Which.Message.Should().Contain("Authentication:OpenIddict:ClientId");
    }

    /// <summary>AC5: the secret is checked the same way.</summary>
    [Fact]
    public void Start_WithoutClientSecret_RefusesAndNamesTheKey()
    {
        var start = () => ValidateAtStart(clientId: "simulab-web", clientSecret: null);

        start.Should().Throw<OptionsValidationException>()
            .Which.Message.Should().Contain("Authentication:OpenIddict:ClientSecret");
    }

    [Fact]
    public void Start_WithBothKeys_Passes()
    {
        var start = () => ValidateAtStart(clientId: "simulab-web", clientSecret: "a-secret");

        start.Should().NotThrow();
    }

    /// <summary>The real host, outside Development, starts when the deploy supplies both keys.</summary>
    [Fact]
    public void Host_OutsideDevelopmentWithBothKeys_Starts()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseEnvironment("Staging").WithDeployedClient());

        using var client = factory.CreateClient();

        client.Should().NotBeNull();
    }

    /// <summary>AC6: Development takes both keys from <c>appsettings.Development.json</c>, so it needs no setting of its own.</summary>
    [Fact]
    public void Host_InDevelopmentWithTheCommittedSettings_Starts()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment(Environments.Development));

        using var client = factory.CreateClient();

        client.Should().NotBeNull();
    }
}
