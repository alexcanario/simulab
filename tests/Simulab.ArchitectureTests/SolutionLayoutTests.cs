using System.Xml.Linq;

namespace Simulab.ArchitectureTests;

/// <summary>F-12: projects live in group folders, and the solution folders mirror the disk folders.</summary>
public class SolutionLayoutTests
{
    private static readonly string[] GroupFolders =
    [
        "src/Hosts/",
        "src/BuildingBlocks/",
        "src/Modules/",
        "tests/Hosts/",
        "tests/BuildingBlocks/",
        "tests/Modules/",
        "tools/" // F-15: developer tools that never ship (DocGen)
    ];

    // F-33: Simulab.Testing.ApiHost joins them — helpers every module's tests share, not one module's tests.
    private static readonly string[] TestsRootProjects = ["Simulab.ArchitectureTests", "Simulab.Testing", "Simulab.Testing.ApiHost"];

    private static readonly string[] RootFolders = ["src", "tests", "tools"];

    private static List<(string Folder, string Path)> ListedProjects(XDocument solution) =>
        solution.Descendants("Project")
            .Select(project => (
                Folder: project.Parent!.Attribute("Name")!.Value.Trim('/'),
                Path: project.Attribute("Path")!.Value.Replace('\\', '/')))
            .ToList();

    private static XDocument LoadSolution() =>
        XDocument.Load(Path.Combine(SolutionAssemblies.RepositoryRoot(), "Simulab.slnx"));

    /// <summary>The folder of the project folder: `src/Hosts` for `src/Hosts/Simulab.Api/Simulab.Api.csproj`.</summary>
    private static string DiskFolderOf(string projectPath)
    {
        var projectFolder = projectPath[..projectPath.LastIndexOf('/')];
        return projectFolder[..projectFolder.LastIndexOf('/')];
    }

    private static IEnumerable<string> LayoutProblems(IEnumerable<(string Folder, string Path)> projects)
    {
        foreach (var (folder, path) in projects)
        {
            if (folder != DiskFolderOf(path))
            {
                yield return $"{path} is in solution folder /{folder}/";
            }

            var name = Path.GetFileNameWithoutExtension(path);
            var inGroup = GroupFolders.Any(group => path.StartsWith(group, StringComparison.Ordinal));
            var atTestsRoot = TestsRootProjects.Contains(name) && DiskFolderOf(path) == "tests";
            if (!inGroup && !atTestsRoot)
            {
                yield return $"{path} is not under a group folder";
            }
        }
    }

    [Fact]
    public void EveryProject_IsInTheSolutionFolderOfItsDiskFolder()
    {
        var projects = ListedProjects(LoadSolution());

        projects.Should().HaveCountGreaterThan(20, "the rule must see the whole solution");
        LayoutProblems(projects).Should().BeEmpty();
    }

    [Fact]
    public void LayoutProblems_ReportAMismatch()
    {
        (string, string)[] projects =
        [
            ("src/Hosts", "src/BuildingBlocks/Simulab.Email/Simulab.Email.csproj"),
            ("src", "src/Simulab.Loose/Simulab.Loose.csproj")
        ];

        LayoutProblems(projects).Should().HaveCount(2);
    }

    [Fact]
    public void EveryProjectOnDisk_IsListedInTheSolution()
    {
        var root = SolutionAssemblies.RepositoryRoot();
        var listed = ListedProjects(LoadSolution()).Select(project => project.Path).ToHashSet();

        var onDisk = RootFolders
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(root, folder), "*.csproj", SearchOption.AllDirectories))
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .Where(path => !path.Contains("/bin/", StringComparison.Ordinal) && !path.Contains("/obj/", StringComparison.Ordinal))
            .ToList();

        onDisk.Should().NotBeEmpty();
        onDisk.Should().BeSubsetOf(listed);
    }

    [Fact]
    public void BuildFiles_ExistOnlyWhereTheLayoutSays()
    {
        var root = SolutionAssemblies.RepositoryRoot();

        string[] Find(string name) =>
            [.. Directory.EnumerateFiles(root, name, SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
                .Where(path => path.StartsWith("src/", StringComparison.Ordinal)
                               || path.StartsWith("tests/", StringComparison.Ordinal)
                               || !path.Contains('/', StringComparison.Ordinal))];

        Find("Directory.Build.props").Should().BeEquivalentTo("Directory.Build.props", "tests/Directory.Build.props");
        Find("Directory.Packages.props").Should().BeEquivalentTo("Directory.Packages.props");
    }

    [Fact]
    public void EditorConfig_SetsNoRuleToError()
    {
        var lines = File.ReadAllLines(Path.Combine(SolutionAssemblies.RepositoryRoot(), ".editorconfig"));

        lines.Should().Contain(line => line.StartsWith("csharp_prefer_braces", StringComparison.Ordinal));
        lines.Where(line => !line.TrimStart().StartsWith('#'))
            .Should().NotContain(line => line.Contains(":error", StringComparison.Ordinal)
                                         || line.Contains("severity = error", StringComparison.Ordinal));
    }
}
