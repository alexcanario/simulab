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

    private async Task<Dictionary<string, string>> PublishAsync(string environment, params string[] extraArguments)
    {
        var output = Path.Combine(_output, environment + extraArguments.Length);
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
                 }.Concat(extraArguments))
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

    /// <summary>The template the manifest points at: the one the deploy reads.</summary>
    private string EmailTemplate()
    {
        using var manifest = System.Text.Json.JsonDocument.Parse(_files["manifest.json"]);
        var path = manifest.RootElement.GetProperty("resources").GetProperty("email").GetProperty("path").GetString()!;
        return File.ReadAllText(path);
    }

    /// <summary>F-66 AC5 (BR8, BR9): the template creates the services in Brazil and one role assignment for the Api only.</summary>
    [Fact]
    public void Publish_Email_CreatesTheServicesInBrazilAndOneRoleAssignmentOnTheCommunicationService()
    {
        var template = EmailTemplate();

        template.Should().Contain("Microsoft.Communication/emailServices@")
            .And.Contain("'AzureManagedDomain'")
            .And.Contain("domainManagement: 'AzureManaged'")
            .And.Contain("Microsoft.Communication/communicationServices@")
            .And.Contain("linkedDomains")
            .And.Contain("displayName: 'Simulab'")
            .And.Contain("principalType: 'ServicePrincipal'")
            .And.Contain("scope: communicationService")
            .And.Contain("principalId: apiPrincipalId")
            .And.Contain("'09976791-48a7-449e-bb21-39d1a415f350'");
        // Brazil on both resources: the domain only links when the two data locations match.
        template.Should().Contain("param dataLocation string = 'Brazil'");
        System.Text.RegularExpressions.Regex.Matches(template, "dataLocation: dataLocation").Should().HaveCount(2);
        System.Text.RegularExpressions.Regex.Matches(template, "Microsoft.Authorization/roleAssignments@").Should().HaveCount(1);
        template.Should().NotContain("listKeys").And.NotContain("@secure");
    }

    /// <summary>
    /// F-64 validation: <c>aspire deploy</c> sends <c>location</c> to every Bicep template and Azure refuses the
    /// deployment when the template does not declare it ("parameters were supplied, but do not correspond"). The
    /// publish does not write it into the manifest, so only the template can be checked.
    /// </summary>
    [Fact]
    public void Publish_Email_DeclaresTheLocationParameterTheDeployCommandSends()
    {
        EmailTemplate().Should().Contain("param location string");
    }

    /// <summary>F-66 AC5 (BR1, BR9): the template is told the Api's identity and nothing else; no email secret or parameter exists.</summary>
    [Fact]
    public void Publish_Email_IsBoundToTheApiIdentityAndAsksForNoSecret()
    {
        using var manifest = System.Text.Json.JsonDocument.Parse(_files["manifest.json"]);
        var resources = manifest.RootElement.GetProperty("resources");

        var email = resources.GetProperty("email");
        email.GetProperty("type").GetString().Should().Be("azure.bicep.v0");
        email.GetProperty("params").EnumerateObject().Select(parameter => parameter.Name).Should().Equal("apiPrincipalId");
        email.GetProperty("params").GetProperty("apiPrincipalId").GetString().Should().Be("{api-identity.outputs.principalId}");

        resources.EnumerateObject().Where(resource => resource.Name.Contains("email", StringComparison.OrdinalIgnoreCase))
            .Select(resource => resource.Name).Should().Equal("email");
        resources.EnumerateObject().Where(resource => resource.Value.TryGetProperty("type", out var type)
                && type.GetString() == "parameter.v0" && resource.Name.Contains("email", StringComparison.OrdinalIgnoreCase))
            .Should().BeEmpty();
    }

    /// <summary>F-66 AC5 (BR9): the Api gets the three settings and its identity's client id; the Web gets no email setting.</summary>
    [Fact]
    public void Publish_Email_GivesTheApiItsSettingsAndTheWebNone()
    {
        var api = Bicep("api-containerapp");
        api.Should().Contain("'Email__Provider'").And.Contain("'AzureCommunicationServices'")
            .And.Contain("'Email__AzureCommunicationServices__Endpoint'")
            .And.Contain("'Email__FromAddress'")
            .And.Contain("name: 'AZURE_CLIENT_ID'");
        // D16 (F-64): the Web has an identity of its own now (the key ring), so "no AZURE_CLIENT_ID" no longer says it sends no email.
        Bicep("web-containerapp").Should().NotContain("Email__");
    }

    /// <summary>
    /// F-66: the Api's own identity is the one the email role is given to and the one it signs in with. D16 (F-64): the Web has
    /// an identity too, for its key ring only; the email template is told the Api's alone (the params test above).
    /// </summary>
    [Fact]
    public void Publish_Email_UsesTheIdentityOfTheApi()
    {
        _files.Keys.Where(file => file.EndsWith("-identity.module.bicep", StringComparison.Ordinal))
            .Should().BeEquivalentTo("api-identity.module.bicep", "web-identity.module.bicep");
        Bicep("api-containerapp").Should().Contain("api_identity_outputs_clientid");
    }

    /// <summary>F-64 AC1 (BR5, D11): Staging applies the migrations on start; Production keeps the default.</summary>
    [Fact]
    public async Task Publish_StagingAppliesTheMigrationsOnStartAndProductionDoesNot()
    {
        Bicep("api-containerapp").Should().MatchRegex(@"name: 'Database__ApplyMigrationsOnStart'\s+value: 'true'");

        var production = await PublishAsync("Production");

        production["api-containerapp.module.bicep"].Should().NotContain("Database__ApplyMigrationsOnStart");
    }

    /// <summary>
    /// F-64 AC6 (BR3, BR6, BR7, D17): no AI key, no seeded admin password and no blanket "believe every proxy" switch on either host;
    /// the Api reads its secrets from Key Vault and the Web does not.
    /// </summary>
    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task Publish_PassesNoSecretAndNoBlanketForwardedHeaders(string environment)
    {
        var files = environment == "Staging" ? _files : await PublishAsync(environment);
        var api = files["api-containerapp.module.bicep"];
        var web = files["web-containerapp.module.bicep"];

        api.Should().NotContain("Identity__SeedAdmin__Password").And.NotContain("Ai__ApiKey");
        web.Should().NotContain("Identity__SeedAdmin__Password").And.NotContain("Ai__ApiKey");
        api.Should().NotContain("ASPNETCORE_FORWARDEDHEADERS_ENABLED");
        web.Should().NotContain("ASPNETCORE_FORWARDEDHEADERS_ENABLED");
        // The rule of presence: the Api does get the vault, so the absences above are not an empty file.
        api.Should().Contain("ConnectionStrings__keyvault");
        web.Should().NotContain("ConnectionStrings__keyvault");
    }

    /// <summary>
    /// F-64 D20 (2026-10-08): the PostgreSQL administrator login and password are deploy-time parameters, the same on every
    /// deploy. Generated by Aspire they changed with every <c>--clear-cache</c> deploy while the server kept its first login,
    /// and the Api failed with 28P01. The password is a secret, the login is not, and neither has a generated default.
    /// </summary>
    [Fact]
    public void Publish_TakesThePostgresLoginAndPasswordFromParametersThatAreNeverGenerated()
    {
        using var manifest = System.Text.Json.JsonDocument.Parse(_files["manifest.json"]);
        var resources = manifest.RootElement.GetProperty("resources");

        var login = resources.GetProperty("postgres-admin-user").GetProperty("inputs").GetProperty("value");
        var password = resources.GetProperty("postgres-admin-password").GetProperty("inputs").GetProperty("value");
        login.TryGetProperty("secret", out _).Should().BeFalse("a login is not a secret");
        password.GetProperty("secret").GetBoolean().Should().BeTrue();
        login.TryGetProperty("default", out _).Should().BeFalse("a generated login is what broke the second deploy");
        password.TryGetProperty("default", out _).Should().BeFalse("a generated password is what broke the second deploy");

        var parameters = resources.GetProperty("postgres").GetProperty("params");
        parameters.GetProperty("administratorLogin").GetString().Should().Be("{postgres-admin-user.value}");
        parameters.GetProperty("administratorLoginPassword").GetString().Should().Be("{postgres-admin-password.value}");

        // No value is written anywhere: the parameters are filled at deploy time.
        _files.Values.Should().NotContain(file => file.Contains("administratorLoginPassword: '"), "the password is a template parameter");
    }

    /// <summary>F-64 BR6, D12: the ingress addresses of the app host's configuration reach both hosts; with none, neither gets a setting.</summary>
    [Fact]
    public async Task Publish_PassesTheListedProxiesToBothHosts()
    {
        var listed = await PublishAsync("Staging", "--ForwardedHeaders:KnownProxies:0=10.0.0.5", "--ForwardedHeaders:KnownNetworks:0=10.0.0.0/24");

        foreach (var host in new[] { "api", "web" })
        {
            listed[$"{host}-containerapp.module.bicep"].Should()
                .MatchRegex(@"name: 'ForwardedHeaders__KnownProxies__0'\s+value: '10\.0\.0\.5'")
                .And.MatchRegex(@"name: 'ForwardedHeaders__KnownNetworks__0'\s+value: '10\.0\.0\.0/24'");
            // The test's content root has no Staging settings file (it is not copied to the output), so the default publish passes none.
            Bicep($"{host}-containerapp").Should().NotContain("ForwardedHeaders__");
        }
    }

    /// <summary>
    /// F-64 D12: the committed Staging settings list the ingress address read on the first staging (the web logged
    /// <c>::ffff:100.100.0.17</c>, 2026-10-07), as one proxy and no network. <c>aspire deploy</c> reads this file from the
    /// app host's folder; the test reads it from the source tree because the publish test's content root does not have it.
    /// </summary>
    [Fact]
    public void StagingSettings_ListTheIngressAddressMeasuredOnTheFirstStaging()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Simulab.slnx")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the repository root holds Simulab.slnx");
        using var settings = System.Text.Json.JsonDocument.Parse(
            File.ReadAllText(Path.Combine(directory!.FullName, "src", "Hosts", "Simulab.AppHost", "appsettings.Staging.json")));
        var section = settings.RootElement.GetProperty("ForwardedHeaders");

        section.GetProperty("KnownProxies").EnumerateArray().Select(address => address.GetString()).Should().Equal("100.100.0.17");
        section.GetProperty("KnownNetworks").GetArrayLength().Should().Be(0);
    }

    /// <summary>
    /// F-64 AC3 (BR4, D9 v2): both hosts get the blob container of their keys and the Key Vault key that encrypts them; the Web
    /// never gets the database (the architect's finding: with one generated login it would become an administrator).
    /// </summary>
    [Fact]
    public void Publish_GivesBothHostsTheirKeyRingAndTheWebNoDatabase()
    {
        foreach (var host in new[] { "api", "web" })
        {
            Bicep($"{host}-containerapp").Should().Contain("ConnectionStrings__keys")
                .And.Contain("DataProtection__KeyVaultKeyId");
        }

        Bicep("api-containerapp").Should().Contain("ConnectionStrings__simulab");
        Bicep("web-containerapp").Should().NotContain("ConnectionStrings__simulab");
        foreach (var account in new[] { "storage-api", "storage-web" })
        {
            Bicep(account).Should().Contain("Standard_LRS").And.Contain("name: 'keys'");
        }
    }

    /// <summary>
    /// F-64 review: each host has a storage account of its own, so the Web can neither read nor overwrite the Api's key ring;
    /// and the Api, which reads its secrets from the vault, has the Key Vault Secrets User role (the Web has not).
    /// </summary>
    [Fact]
    public void Publish_KeepsEachHostsKeyRingApartAndGivesOnlyTheApiTheSecrets()
    {
        const string SecretsUser = "4633458b-17de-408a-b874-0445c86b69e6";
        _files.Keys.Where(file => file.StartsWith("storage", StringComparison.Ordinal)).Should()
            .BeEquivalentTo("storage-api.module.bicep", "storage-web.module.bicep");
        _files.Keys.Where(file => file.StartsWith("web-roles-storage", StringComparison.Ordinal)).Should()
            .ContainSingle("the Web's role assignments name its own account only");
        Bicep("web-roles-storage-web").Should().Contain("storage_web");
        Bicep("web-roles-storage-web").Should().NotContain("storage_api");
        Bicep("api-roles-keyvault").Should().Contain(SecretsUser);
        _files.Keys.Where(file => file.StartsWith("web-roles-keyvault", StringComparison.Ordinal)).Should().ContainSingle();
        Bicep("web-roles-keyvault").Should().NotContain(SecretsUser);
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
