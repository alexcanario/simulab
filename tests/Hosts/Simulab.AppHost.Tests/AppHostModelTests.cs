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
}
