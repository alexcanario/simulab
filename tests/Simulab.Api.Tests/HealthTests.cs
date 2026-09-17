using System.Net;
using Simulab.Testing;

namespace Simulab.Api.Tests;

/// <summary>AC11: the health endpoint reports the database.</summary>
public class HealthTests
{
    [Fact]
    public async Task Health_ReachableDatabase_IsHealthy()
    {
        await using var factory = new ApiFactory
        {
            DatabaseConnectionString = await PostgresServer.CreateDatabaseAsync(nameof(HealthTests)),
        };

        var response = await factory.CreateClient().GetAsync("/health", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(CancellationToken.None)).Should().Be("Healthy");
    }

    [Fact]
    public async Task Health_UnreachableDatabase_IsUnhealthy()
    {
        await using var factory = new ApiFactory();

        var response = await factory.CreateClient().GetAsync("/health", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
