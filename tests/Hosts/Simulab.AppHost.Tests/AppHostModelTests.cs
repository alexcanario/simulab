using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Simulab.AppHost.Tests;

/// <summary>AC10: the app host runs PostgreSQL with a data volume and Mailpit, and the Api waits for both.</summary>
public class AppHostModelTests : IAsyncLifetime
{
    private DistributedApplicationModel _model = null!;
    private IDistributedApplicationTestingBuilder? _builder;

    public async Task InitializeAsync()
    {
        _builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Simulab_AppHost>();
        _model = new DistributedApplicationModel(_builder.Resources);
    }

    public async Task DisposeAsync()
    {
        if (_builder is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }

    private IResource Resource(string name) =>
        _model.Resources.Should().ContainSingle(resource => resource.Name == name).Which;

    [Fact]
    public void Model_HasPostgresWithADataVolumeAndTheAppDatabase()
    {
        var postgres = Resource("postgres");

        postgres.Annotations.OfType<ContainerMountAnnotation>()
            .Should().ContainSingle(mount => mount.Type == ContainerMountType.Volume && mount.Source == "simulab-postgres-data");
        Resource("simulab").Should().BeAssignableTo<IResourceWithConnectionString>();
    }

    [Fact]
    public void Model_HasMailpit()
    {
        Resource("mailpit").Should().BeAssignableTo<IResourceWithConnectionString>();
    }

    [Fact]
    public void Api_ReferencesAndWaitsForTheDatabaseAndMailpit()
    {
        var api = Resource("api");

        var references = api.Annotations.OfType<EnvironmentCallbackAnnotation>();
        references.Should().NotBeEmpty();
        var waits = api.Annotations.OfType<WaitAnnotation>().Select(wait => wait.Resource.Name).ToList();
        waits.Should().Contain(["simulab", "mailpit"]);
    }

    // F-89: the files users upload live in Azurite with a named data volume, and only the Api reaches them.
    [Fact]
    public void Model_HasAzuriteWithADataVolume_AndOnlyTheApiReferencesTheBlobs()
    {
        Resource("blob").Annotations.OfType<ContainerMountAnnotation>()
            .Should().ContainSingle(mount => mount.Type == ContainerMountType.Volume && mount.Source == "simulab-azurite-data");
        Resource("blobs").Should().BeAssignableTo<IResourceWithConnectionString>();
        Resource("api").Annotations.OfType<WaitAnnotation>().Select(wait => wait.Resource.Name).Should().Contain("blobs");
        Resource("web").Annotations.OfType<WaitAnnotation>().Select(wait => wait.Resource.Name).Should().NotContain("blobs");
    }

    [Fact]
    public void Web_WaitsForTheApi()
    {
        Resource("web").Annotations.OfType<WaitAnnotation>().Select(wait => wait.Resource.Name)
            .Should().Contain("api");
    }

    /// <summary>B-1: the environment values, the supported way since `GetEnvironmentVariableValuesAsync` was marked obsolete.</summary>
    private async Task<Dictionary<string, string>> EnvironmentOfAsync(IResource resource)
    {
        var options = new DistributedApplicationExecutionContextOptions(DistributedApplicationOperation.Publish)
        {
            Services = _builder!.Services.BuildServiceProvider(),
        };

        var result = await ExecutionConfigurationBuilder.Create(resource)
            .WithEnvironmentVariablesConfig()
            .BuildAsync(new DistributedApplicationExecutionContext(options));

        return result.EnvironmentVariables.ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    /// <summary>
    /// F-4 retro: the MailPit connection string carries the container's own SMTP port, which the Api cannot
    /// reach from the host. The Api must get the SMTP address from the mapped endpoint instead.
    /// </summary>
    [Fact]
    public async Task Api_GetsTheSmtpAddressFromTheMappedEndpoint()
    {
        var environment = await EnvironmentOfAsync(Resource("api"));

        environment.Should().ContainKey("ConnectionStrings__mailpit");
        environment["ConnectionStrings__mailpit"].Should().StartWith("smtp://")
            .And.Contain("mailpit.bindings.smtp")
            .And.NotContain("mailpit.connectionString");
    }

    [Fact]
    public async Task Api_GetsTheVerificationLinkOfTheWeb()
    {
        var environment = await EnvironmentOfAsync(Resource("api"));

        environment.Should().ContainKey("Identity__VerificationUrl");
        environment["Identity__VerificationUrl"].Should().Contain("web.bindings.https").And.EndWith("/verify-email");
    }

    // Local network access: Lan:PublicBaseUrl opens the Web to the network and points every email link at it.
    [Fact]
    public async Task Lan_PublicBaseUrl_OpensTheWebToTheNetworkAndPointsTheEmailLinksAtIt()
    {
        await using var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Simulab_AppHost>(
            ["--Lan:PublicBaseUrl=https://192.168.1.68:7125/"]);
        var model = new DistributedApplicationModel(builder.Resources);
        var web = model.Resources.Single(resource => resource.Name == "web");
        var api = model.Resources.Single(resource => resource.Name == "api");

        web.Annotations.OfType<EndpointAnnotation>().Where(endpoint => endpoint.Name is "http" or "https")
            .Should().HaveCount(2).And.OnlyContain(endpoint => endpoint.TargetHost == "0.0.0.0");

        var options = new DistributedApplicationExecutionContextOptions(DistributedApplicationOperation.Publish)
        {
            Services = builder.Services.BuildServiceProvider(),
        };
        var result = await ExecutionConfigurationBuilder.Create(api)
            .WithEnvironmentVariablesConfig()
            .BuildAsync(new DistributedApplicationExecutionContext(options));
        var environment = result.EnvironmentVariables.ToDictionary(pair => pair.Key, pair => pair.Value);

        environment["Identity__VerificationUrl"].Should().Be("https://192.168.1.68:7125/verify-email");
        environment["Identity__PasswordResetUrl"].Should().Be("https://192.168.1.68:7125/reset-password");
        environment["Identity__ForgotPasswordUrl"].Should().Be("https://192.168.1.68:7125/forgot-password");
        environment["Identity__SignUpUrl"].Should().Be("https://192.168.1.68:7125/sign-up");
    }

    [Fact]
    public async Task Lan_Enabled_UsesTheMachineNameWhenNoAddressIsGiven()
    {
        await using var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Simulab_AppHost>(
            ["--Lan:Enabled=true"]);
        var web = new DistributedApplicationModel(builder.Resources).Resources.Single(resource => resource.Name == "web");

        web.Annotations.OfType<EndpointAnnotation>().Where(endpoint => endpoint.Name is "http" or "https")
            .Should().OnlyContain(endpoint => endpoint.TargetHost == "0.0.0.0");
    }

    [Fact]
    public void Web_StaysOnLocalhostWithoutLanPublicBaseUrl()
    {
        Resource("web").Annotations.OfType<EndpointAnnotation>()
            .Should().OnlyContain(endpoint => endpoint.TargetHost == "localhost");
    }
}
