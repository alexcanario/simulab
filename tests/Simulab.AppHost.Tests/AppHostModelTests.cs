using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

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
}
