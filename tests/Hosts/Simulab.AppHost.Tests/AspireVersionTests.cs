using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Simulab.AppHost.Tests;

/// <summary>
/// F-62 AC6: the deploy command runs the Aspire CLI, and a CLI and packages on different versions fail
/// (profile, "Deploy recipe": "Run completed without returning a backchannel"). So every <c>Aspire.*</c> package and the
/// AppHost SDK carry one version. The MailPit community toolkit is left out (F-64, 2026-10-07): it is released on its own
/// schedule (no 13.6.1 on nuget.org) and runs only in the local app host, never in the deploy.
/// </summary>
public class AspireVersionTests
{
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Packages.props")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Directory.Packages.props not found above the test output.");
    }

    [Fact]
    public void EveryAspirePackageAndTheAppHostSdk_CarryTheSameVersion()
    {
        var root = RepositoryRoot();
        var versions = XDocument.Load(Path.Combine(root, "Directory.Packages.props"))
            .Descendants("PackageVersion")
            .Where(package => ((string?)package.Attribute("Include"))?.StartsWith("Aspire.", StringComparison.Ordinal) == true)
            .ToDictionary(package => (string)package.Attribute("Include")!, package => (string)package.Attribute("Version")!);

        var sdk = Regex.Match(File.ReadAllText(Path.Combine(root, "src", "Hosts", "Simulab.AppHost", "Simulab.AppHost.csproj")),
            @"Sdk=""Aspire\.AppHost\.Sdk/(?<version>[^""]+)""");
        sdk.Success.Should().BeTrue("the AppHost project uses the Aspire SDK");
        versions["Aspire.AppHost.Sdk"] = sdk.Groups["version"].Value;

        versions.Values.Distinct().Should().ContainSingle(
            because: "the CLI and every Aspire package must be on one version; found " +
                     string.Join(", ", versions.Select(pair => $"{pair.Key} {pair.Value}")));
    }

    /// <summary>The SDK adds Aspire.Hosting.AppHost itself; a PackageVersion for it breaks the restore (NU1009).</summary>
    [Fact]
    public void AspireHostingAppHost_IsNeverAPackageVersion()
    {
        var content = File.ReadAllText(Path.Combine(RepositoryRoot(), "Directory.Packages.props"));

        content.Should().NotContain("Include=\"Aspire.Hosting.AppHost\"");
    }
}
