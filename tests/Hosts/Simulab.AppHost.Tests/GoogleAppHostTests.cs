using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Simulab.AppHost.Tests;

/// <summary>
/// F-20 BR1 through the app host: Google sign-in is on locally only when the OAuth client is in the host's configuration,
/// and then both hosts get the switch and the client id, and only the Web gets the secret.
/// </summary>
public class GoogleAppHostTests
{
    [Fact]
    public async Task ClientConfigured_TurnsGoogleOnForBothHostsAndGivesTheSecretOnlyToTheWeb()
    {
        await using var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Simulab_AppHost>(
            ["--Google:ClientId=local.apps.googleusercontent.com", "--Google:ClientSecret=local-secret"]);

        var api = await EnvironmentOfAsync(builder, "api");
        var web = await EnvironmentOfAsync(builder, "web");

        api["Identity__GoogleSignInEnabled"].Should().Be("true");
        api["Authentication__Google__ClientId"].Should().Be("local.apps.googleusercontent.com");
        api.Should().NotContainKey("Authentication__Google__ClientSecret");
        web["Identity__GoogleSignInEnabled"].Should().Be("true");
        web["Authentication__Google__ClientId"].Should().Be("local.apps.googleusercontent.com");
        web.Should().ContainKey("Authentication__Google__ClientSecret");
        builder.Resources.OfType<ParameterResource>()
            .Should().ContainSingle(parameter => parameter.Name == "google-client-secret").Which.Secret.Should().BeTrue();
    }

    [Fact]
    public async Task NoClient_LeavesGoogleOff()
    {
        await using var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Simulab_AppHost>(
            ["--Google:ClientId=", "--Google:ClientSecret="]);

        (await EnvironmentOfAsync(builder, "api")).Should().NotContainKey("Identity__GoogleSignInEnabled");
        (await EnvironmentOfAsync(builder, "web")).Should().NotContainKey("Identity__GoogleSignInEnabled");
    }

    /// <summary>The environment values, read the way <see cref="AppHostModelTests"/> reads them (B-1).</summary>
    private static async Task<Dictionary<string, string>> EnvironmentOfAsync(IDistributedApplicationTestingBuilder builder, string name)
    {
        var resource = builder.Resources.Single(resource => resource.Name == name);
        var options = new DistributedApplicationExecutionContextOptions(DistributedApplicationOperation.Publish)
        {
            Services = builder.Services.BuildServiceProvider(),
        };

        var result = await ExecutionConfigurationBuilder.Create(resource)
            .WithEnvironmentVariablesConfig()
            .BuildAsync(new DistributedApplicationExecutionContext(options));

        return result.EnvironmentVariables.ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}
