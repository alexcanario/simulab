using System.Reflection;
using System.Xml.Linq;

namespace Simulab.ArchitectureTests;

/// <summary>F-60: the app version is one `&lt;Version&gt;` on Simulab.Api.csproj, and nowhere else.</summary>
public class AppVersionTests
{
    private const string ApiProject = "src/Hosts/Simulab.Api/Simulab.Api.csproj";

    private static readonly string[] Roots = ["src", "tests", "tools"];

    private static List<string> BuildFiles(string repositoryRoot) =>
        Roots
            .SelectMany(root => Directory.EnumerateFiles(Path.Combine(repositoryRoot, root), "*.*", SearchOption.AllDirectories))
            .Where(file => file.EndsWith(".csproj", StringComparison.Ordinal) || file.EndsWith(".props", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/'))
            .ToList();

    private static bool CarriesVersion(string repositoryRoot, string relativePath) =>
        XDocument.Load(Path.Combine(repositoryRoot, relativePath)).Descendants("Version").Any();

    private static IEnumerable<string> VersionOutsideTheApi(string repositoryRoot, IEnumerable<string> files) =>
        files.Where(file => file != ApiProject && CarriesVersion(repositoryRoot, file));

    [Fact]
    public void ApiProject_CarriesOneSemanticVersion()
    {
        var root = SolutionAssemblies.RepositoryRoot();

        var versions = XDocument.Load(Path.Combine(root, ApiProject)).Descendants("Version").ToList();

        versions.Should().ContainSingle(because: "the app version lives on Simulab.Api.csproj once");
        versions[0].Value.Should().MatchRegex(@"^\d+\.\d+\.\d+$", "/agile:ship bumps it as MAJOR.MINOR.PATCH");
    }

    [Fact]
    public void NoOtherProjectOrPropsFile_CarriesAVersion()
    {
        var root = SolutionAssemblies.RepositoryRoot();
        var files = BuildFiles(root);

        files.Should().HaveCountGreaterThan(20, "the rule must see the whole solution");
        files.Should().Contain(ApiProject);
        VersionOutsideTheApi(root, files).Should().BeEmpty("only Simulab.Api.csproj carries the app version");
    }

    [Fact]
    public void VersionOutsideTheApi_NamesTheOffendingFile()
    {
        var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "Offender.csproj"), "<Project><PropertyGroup><Version>1.0.0</Version></PropertyGroup></Project>");
            File.WriteAllText(Path.Combine(root, "Clean.props"), "<Project><PropertyGroup /></Project>");

            VersionOutsideTheApi(root, ["Offender.csproj", "Clean.props"]).Should().Equal("Offender.csproj");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ApiAssembly_CarriesTheVersionInItsInformationalVersion()
    {
        var assembly = typeof(Simulab.Api.Features.System.SystemInfoResponse).Assembly;

        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        var projectVersion = XDocument.Load(Path.Combine(SolutionAssemblies.RepositoryRoot(), ApiProject)).Descendants("Version").Single().Value;

        informational.Should().StartWith(projectVersion);
    }
}
