using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Simulab.Web.Tests;

/// <summary>
/// F-94 BR6: outside Development the Web stops at start without the client it signs in as, which the deploy supplies and
/// <c>appsettings.Development.json</c> supplies locally. A test host started in Staging or Production uses this.
/// </summary>
internal static class DeployedClientSettings
{
    internal static IWebHostBuilder WithDeployedClient(this IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Authentication:OpenIddict:ClientId"] = "simulab-web",
                ["Authentication:OpenIddict:ClientSecret"] = "a-test-secret",
            }));
    }
}
