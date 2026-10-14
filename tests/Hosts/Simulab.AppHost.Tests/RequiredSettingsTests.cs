using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Simulab.AppHost.Tests;

/// <summary>
/// F-94: every setting the Api and the Web need outside Development has a source in the publish model of Staging and of
/// Production, or in the host's committed settings. The next missing one fails here, and not as a 401 on the staging
/// (F-64: the Web had <c>Authentication:OpenIddict:ClientId</c> only in <c>appsettings.Development.json</c>).
/// </summary>
public class RequiredSettingsTests
{
    private static readonly Lazy<IReadOnlyList<RequiredOptionsScanner.RequiredOption>> Scanned =
        new(() => RequiredOptionsScanner.Find(RequiredSettingsCatalog.HostAssemblies()));

    /// <summary>AC2 (BR1, BR2, BR4, BR5): today's publish model satisfies every required setting of both hosts.</summary>
    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task Publish_EveryRequiredSettingHasASource(string environment)
    {
        await using var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Simulab_AppHost>(
            ["--operation", "publish", "--environment", environment]);

        var problems = new List<string>();
        foreach (var (host, resourceName) in new[] { (SettingsHost.Api, "api"), (SettingsHost.Web, "web") })
        {
            var resource = builder.Resources.Single(candidate => candidate.Name == resourceName);
            var projectDirectory = Path.GetDirectoryName(resource.Annotations.OfType<IProjectMetadata>().Single().ProjectPath)!;

            var outcome = SettingsSourceCheck.Check(
                host,
                environment,
                RequiredSettingsCatalog.For(host, Scanned.Value),
                await PublishedVariablesAsync(builder, resource),
                HostSettingsFiles.Read(projectDirectory, environment));
            problems.AddRange(outcome.Problems);
        }

        problems.Should().BeEmpty("every required setting needs a value in the publish or in the host's committed settings");
    }

    /// <summary>AC3 (BR3, UC2): an options class with a required key and no default is named by a host, or exempt with a reason.</summary>
    [Fact]
    public void Options_EveryRequiredOptionIsNamedByAHostOrExempt()
    {
        var named = RequiredSettingsCatalog.OptionTypes.Values.SelectMany(types => types)
            .Concat(RequiredSettingsCatalog.ExemptOptionTypes.Keys)
            .ToHashSet(StringComparer.Ordinal);

        var unnamed = Scanned.Value
            .Where(option => !named.Contains(option.Type.FullName!))
            .Select(option => $"{option.Type.FullName}: {string.Join(", ", option.FullKeys)}")
            .ToList();

        unnamed.Should().BeEmpty(
            "an options class with a [Required] property and no default must be named in RequiredSettingsCatalog.OptionTypes for the host that binds it, "
            + "or exempt with a reason");
    }

    /// <summary>AC8 (BR8): a scan that silently matches nothing cannot pass.</summary>
    [Fact]
    public void Options_TheScanFindsTheKnownClassesAndAKeyPerHost()
    {
        var found = Scanned.Value.Select(option => option.Type.FullName).ToList();
        found.Should().Contain(["Simulab.Email.EmailOptions", "Simulab.Web.Services.Auth.OpenIddictClientOptions"]);

        foreach (var host in Enum.GetValues<SettingsHost>())
        {
            RequiredSettingsCatalog.For(host, Scanned.Value).Should().NotBeEmpty($"{host} has required settings");
        }
    }

    /// <summary>Every class the catalog names exists, so a rename is not silently skipped by the name match.</summary>
    [Fact]
    public void Catalog_EveryNamedOptionsClassExists()
    {
        var existing = RequiredSettingsCatalog.HostAssemblies().SelectMany(assembly => assembly.GetTypes())
            .Select(type => type.FullName)
            .ToHashSet(StringComparer.Ordinal);

        var missing = RequiredSettingsCatalog.OptionTypes.Values.SelectMany(types => types)
            .Concat(RequiredSettingsCatalog.ExemptOptionTypes.Keys)
            .Where(name => !existing.Contains(name))
            .ToList();

        missing.Should().BeEmpty();
    }

    private static async Task<Dictionary<string, string?>> PublishedVariablesAsync(
        IDistributedApplicationTestingBuilder builder, IResource resource)
    {
        var options = new DistributedApplicationExecutionContextOptions(DistributedApplicationOperation.Publish)
        {
            Services = builder.Services.BuildServiceProvider(),
        };

        var result = await ExecutionConfigurationBuilder.Create(resource)
            .WithEnvironmentVariablesConfig()
            .BuildAsync(new DistributedApplicationExecutionContext(options));

        return result.EnvironmentVariables.ToDictionary(pair => pair.Key, pair => (string?)Convert.ToString(pair.Value), StringComparer.Ordinal);
    }
}
