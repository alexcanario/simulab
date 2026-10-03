using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Testing;

namespace Simulab.AppHost.Tests;

/// <summary>
/// F-62 (ADR-0002): in publish mode the app host describes the Azure deployment; in run mode it keeps today's
/// containers. Both are read from the model, so no Azure service is called.
/// </summary>
public class AzurePublishModelTests
{
    private static async Task<IDistributedApplicationTestingBuilder> PublishBuilderAsync(string environment) =>
        await DistributedApplicationTestingBuilder.CreateAsync<Projects.Simulab_AppHost>(
            ["--operation", "publish", "--environment", environment]);

    [Fact]
    public async Task Publish_DescribesTheContainerAppsEnvironmentAndTheManagedServices()
    {
        await using var builder = await PublishBuilderAsync("Staging");

        var names = builder.Resources.Select(resource => resource.Name).ToList();
        names.Should().Contain(["cae", "keyvault", "postgres", "simulab", "redis", "api", "web"]);
        builder.Resources.OfType<AzureProvisioningResource>().Select(resource => resource.Name)
            .Should().Contain(["cae", "keyvault", "postgres"]);
    }

    [Fact]
    public async Task Publish_LeavesLocalOnlyResourcesOut()
    {
        await using var builder = await PublishBuilderAsync("Staging");

        builder.Resources.Select(resource => resource.Name).Should().NotContain(["mailpit", "postgres-password"]);
    }

    [Fact]
    public async Task Publish_StagingRunsRedisAsAContainer_ProductionUsesTheManagedCache()
    {
        await using var staging = await PublishBuilderAsync("Staging");
        await using var production = await PublishBuilderAsync("Production");

        staging.Resources.Single(resource => resource.Name == "redis").Should().BeOfType<RedisResource>();
        production.Resources.Single(resource => resource.Name == "redis").Should().BeAssignableTo<AzureProvisioningResource>();
    }

    [Fact]
    public async Task Run_KeepsTheLocalContainersAndNoAzureProvisioningResource()
    {
        // The Google switch is cleared so the user secrets of the machine running the test do not change the model.
        await using var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Simulab_AppHost>(
            ["--Google:ClientId=", "--Google:ClientSecret=", "--Ai:ApiKey="]);

        builder.Resources.Select(resource => resource.Name)
            .Should().Contain(["postgres", "postgres-password", "simulab", "mailpit", "redis", "api", "web"])
            .And.NotContain(["cae", "keyvault"]);
        builder.Resources.OfType<AzureProvisioningResource>().Should().BeEmpty();
        builder.Resources.Single(resource => resource.Name == "postgres").Should().BeOfType<PostgresServerResource>();
    }
}
