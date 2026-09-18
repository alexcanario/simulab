using System.Net;
using Simulab.Testing;

namespace Simulab.Api.Tests;

/// <summary>AC11 (F-3): the health endpoint reports the database. Extended by F-5 for Redis.</summary>
public class HealthTests
{
    [Fact]
    public async Task Health_ReachableDatabaseAndRedis_IsHealthy()
    {
        await using var factory = new ApiFactory
        {
            DatabaseConnectionString = await PostgresServer.CreateDatabaseAsync(nameof(HealthTests)),
            RedisConnectionString = await RedisServer.ConnectionStringAsync(),
        };

        var response = await factory.CreateClient().GetAsync("/health", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(CancellationToken.None)).Should().Be("Healthy");
    }

    [Fact]
    public async Task Health_UnreachableDatabase_IsUnhealthy()
    {
        await using var factory = new ApiFactory
        {
            RedisConnectionString = await RedisServer.ConnectionStringAsync(),
        };

        var response = await factory.CreateClient().GetAsync("/health", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Health_UnreachableRedis_IsUnhealthy()
    {
        await using var factory = new ApiFactory
        {
            DatabaseConnectionString = await PostgresServer.CreateDatabaseAsync(nameof(HealthTests)),
        };

        var response = await factory.CreateClient().GetAsync("/health", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
