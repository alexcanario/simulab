using System.Text.RegularExpressions;

namespace Simulab.ArchitectureTests;

/// <summary>
/// B-34: the Aspire packages, the app host SDK and the Aspire CLI of the deploy workflow must be one version
/// (docs/infra.md: "the Aspire CLI on the same version as the Aspire packages"). A bump that touches one and
/// forgets another breaks the app host or the staging deploy, far from where it was edited.
/// </summary>
public partial class AspireVersionTests
{
    [Fact]
    public void AspireVersion_IsTheSame_InPackagesSdkAndDeployWorkflow()
    {
        var root = SolutionAssemblies.RepositoryRoot();

        var packages = PackageVersionRegex().Matches(File.ReadAllText(Path.Combine(root, "Directory.Packages.props")))
            .Select(m => (Name: m.Groups["name"].Value, Version: m.Groups["version"].Value))
            .Where(p => p.Name.StartsWith("Aspire.", StringComparison.Ordinal))
            .ToList();
        var sdk = SdkRegex().Match(File.ReadAllText(Path.Combine(root, "src", "Hosts", "Simulab.AppHost", "Simulab.AppHost.csproj")));
        var cli = CliRegex().Match(File.ReadAllText(Path.Combine(root, ".github", "workflows", "deploy.yml")));

        packages.Should().HaveCountGreaterThan(5, "the rule must have read the Aspire package entries");
        sdk.Success.Should().BeTrue("the app host project declares the Aspire.AppHost.Sdk version");
        cli.Success.Should().BeTrue("the deploy workflow declares ASPIRE_CLI_VERSION");

        var versions = packages.Select(p => $"package {p.Version}")
            .Append($"sdk {sdk.Groups["version"].Value}")
            .Append($"cli {cli.Groups["version"].Value}")
            .Select(v => v.Split(' ')[1])
            .Distinct()
            .ToList();

        versions.Should().ContainSingle("Directory.Packages.props, the app host SDK and ASPIRE_CLI_VERSION must read the same Aspire version");
    }

    [GeneratedRegex("""<PackageVersion\s+Include="(?<name>[^"]+)"\s+Version="(?<version>[^"]+)"\s*/>""")]
    private static partial Regex PackageVersionRegex();

    [GeneratedRegex(@"Sdk=""Aspire\.AppHost\.Sdk/(?<version>[^""]+)""")]
    private static partial Regex SdkRegex();

    [GeneratedRegex(@"ASPIRE_CLI_VERSION:\s*""(?<version>[^""]+)""")]
    private static partial Regex CliRegex();
}
