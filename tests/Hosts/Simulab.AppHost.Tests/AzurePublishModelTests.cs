using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

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

    /// <summary>F-66 AC5: the model has the email template and the Api's identity.</summary>
    [Fact]
    public async Task Publish_DescribesTheEmailTemplateAndTheApiIdentity()
    {
        await using var builder = await PublishBuilderAsync("Staging");

        builder.Resources.Select(resource => resource.Name).Should().Contain(["email", "api-identity"]);
        builder.Resources.Single(resource => resource.Name == "email").Should().BeAssignableTo<AzureBicepResource>();
    }

    /// <summary>F-66 AC2 (BR2): a local run keeps Mailpit and sets no cloud email setting.</summary>
    [Fact]
    public async Task Run_KeepsMailpitAndSetsNoCloudEmailSetting()
    {
        await using var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Simulab_AppHost>(
            ["--Google:ClientId=", "--Google:ClientSecret=", "--Ai:ApiKey="]);

        builder.Resources.Select(resource => resource.Name).Should().Contain("mailpit").And.NotContain(["email", "api-identity"]);
        // The callbacks are run without resolving them: a reference to an endpoint has no port before the host runs.
        var api = builder.Resources.Single(resource => resource.Name == "api");
        var context = new EnvironmentCallbackContext(new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));
        foreach (var callback in api.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await callback.Callback(context);
        }

        context.EnvironmentVariables.Keys.Should().NotContain(
            ["Email__Provider", "Email__AzureCommunicationServices__Endpoint", "Email__FromAddress"]);
    }

    /// <summary>F-64: the cloud Web has no appsettings.Development.json, so the client id it sends to the token endpoint comes from the deploy.</summary>
    [Fact]
    public async Task Publish_TheWebIsToldTheClientIdTheApiSeeds()
    {
        await using var builder = await PublishBuilderAsync("Staging");

        var web = builder.Resources.Single(resource => resource.Name == "web");
        var options = new DistributedApplicationExecutionContextOptions(DistributedApplicationOperation.Publish)
        {
            Services = builder.Services.BuildServiceProvider(),
        };

        var result = await ExecutionConfigurationBuilder.Create(web)
            .WithEnvironmentVariablesConfig()
            .BuildAsync(new DistributedApplicationExecutionContext(options));

        result.EnvironmentVariables.ToDictionary(pair => pair.Key, pair => pair.Value)
            .Should().Contain("Authentication__OpenIddict__ClientId", "simulab-web");
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
