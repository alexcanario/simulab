using System.Diagnostics;

namespace Simulab.AppHost.Tests;

/// <summary>
/// F-62 AC2, AC4, AC5: the app host is run the way a publish runs it (manifest publisher, no Azure sign-in, no CLI)
/// and the deployment files it writes are read. A secret in the machine's user secrets or a sentinel passed on the
/// command line must never reach them.
/// </summary>
public sealed class AzurePublishFilesTests : IAsyncLifetime
{
    private const string LocalPostgresPassword = "postgres";
    private const string LocalOpenIddictSecret = "dev-only-simulab-web-secret-do-not-use-outside-local-dev";
    private const string GoogleSentinel = "google-secret-sentinel-4f1c";
    private const string AiSentinel = "ai-key-sentinel-9b2e";

    private readonly string _output = Path.Combine(Path.GetTempPath(), "simulab-f62-" + Guid.NewGuid().ToString("N"));
    private IReadOnlyDictionary<string, string> _files = new Dictionary<string, string>();

    public async Task InitializeAsync()
    {
        _files = await PublishAsync("Staging");
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_output))
        {
            Directory.Delete(_output, recursive: true);
        }

        return Task.CompletedTask;
    }

    private async Task<Dictionary<string, string>> PublishAsync(string environment)
    {
        var output = Path.Combine(_output, environment);
        var appHost = Path.Combine(AppContext.BaseDirectory, "Simulab.AppHost.dll");

        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in new[]
                 {
                     appHost, "--operation", "publish", "--publisher", "manifest",
                     "--output-path", Path.Combine(output, "manifest.json"),
                     "--Google:ClientId=sentinel.apps.googleusercontent.com", $"--Google:ClientSecret={GoogleSentinel}",
                     $"--Ai:ApiKey={AiSentinel}",
                 })
        {
            start.ArgumentList.Add(argument);
        }

        start.Environment["ASPNETCORE_ENVIRONMENT"] = environment;
        start.Environment["DOTNET_ENVIRONMENT"] = environment;

        using var process = Process.Start(start)!;
        var log = await process.StandardOutput.ReadToEndAsync() + await process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        await process.WaitForExitAsync(timeout.Token);

        process.ExitCode.Should().Be(0, because: log);
        return Directory.GetFiles(output)
            .ToDictionary(file => Path.GetFileName(file), File.ReadAllText);
    }

    private string Bicep(string resource) =>
        _files.Should().ContainKey($"{resource}.module.bicep").WhoseValue;

    /// <summary>AC2: a Container Apps environment, both hosts, the PostgreSQL server with its database, Redis and Key Vault.</summary>
    [Fact]
    public void Publish_WritesTheFilesOfTheContainerAppsEnvironmentAndTheManagedServices()
    {
        _files.Keys.Should().Contain([
            "cae.module.bicep",
            "api-containerapp.module.bicep",
            "web-containerapp.module.bicep",
            "postgres.module.bicep",
            "keyvault.module.bicep",
            "manifest.json",
        ]);
        Bicep("postgres").Should().Contain("Microsoft.DBforPostgreSQL/flexibleServers").And.Contain("name: 'simulab'");
        Bicep("keyvault").Should().Contain("Microsoft.KeyVault/vaults");
        _files["manifest.json"].Should().Contain("\"redis\"");
    }

    /// <summary>
    /// AC4 (BR5): the Api always runs (it carries the job worker); the Web runs at most once and may sleep.
    /// F-54 AC10 (BR7): the Api runs at most once too, until F-73 shares the OpenIddict keys.
    /// </summary>
    [Fact]
    public void Publish_KeepsTheApiAwakeAndBothHostsAtMostOnce()
    {
        var api = Bicep("api-containerapp");
        api.Should().Contain("minReplicas: 1").And.Contain("maxReplicas: 1");

        var web = Bicep("web-containerapp");
        web.Should().Contain("minReplicas: 0").And.Contain("maxReplicas: 1");
    }

    /// <summary>AC5 (BR7): no local password, no OpenIddict secret and none of the secrets given to the host.</summary>
    [Fact]
    public void Publish_WritesNoSecretValue()
    {
        var all = string.Join('\n', _files.Values);

        all.Should().NotContain(LocalOpenIddictSecret)
            .And.NotContain(GoogleSentinel)
            .And.NotContain(AiSentinel)
            .And.NotContain("google-client-secret")
            .And.NotContain("ai-api-key")
            .And.NotContain($"password={LocalPostgresPassword}")
            .And.NotContain($"Password={LocalPostgresPassword}");
        // The OpenIddict secret is a parameter: the files name it and the deployment asks for it.
        Bicep("api-containerapp").Should().Contain("param openiddict_client_secret_value string");
    }

    /// <summary>BR1 (v2): the images go to the Azure Container Registry that the environment creates.</summary>
    [Fact]
    public void Publish_KeepsTheImagesInTheEnvironmentRegistry()
    {
        _files.Keys.Should().Contain("cae-acr.module.bicep");
        Bicep("cae-acr").Should().Contain("Microsoft.ContainerRegistry/registries");
    }

    /// <summary>The Production files differ from Staging in one thing worth a test: the cache is the managed one.</summary>
    [Fact]
    public async Task Publish_ProductionUsesTheManagedRedis()
    {
        var production = await PublishAsync("Production");

        production.Keys.Should().Contain("redis.module.bicep");
        production.Keys.Should().NotContain("redis-containerapp.module.bicep");
        _files.Keys.Should().Contain("redis-containerapp.module.bicep").And.NotContain("redis.module.bicep");
    }
}
