using System.Xml.Linq;
using Simulab.DocGen;

namespace Simulab.ArchitectureTests.DocGen;

/// <summary>F-15: the module diagram from the project references under src/ (AC8).</summary>
public class ModuleMapDocTests
{
    private static readonly string Root = SolutionAssemblies.RepositoryRoot();

    private static List<string> SourceProjects() =>
        [.. Directory.EnumerateFiles(Path.Combine(Root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))];

    private static string Id(string name) => name.Replace('.', '_');

    [Fact]
    public void Render_ListsEveryProjectOnceInItsGroup()
    {
        var text = ModuleMapDoc.Render(Root, "Simulab");

        text.Should().Contain("subgraph Hosts[\"Hosts\"]").And.Contain("subgraph BuildingBlocks[\"BuildingBlocks\"]").And.Contain("subgraph Modules_Identity[\"Modules/Identity\"]");
        var projects = SourceProjects();
        projects.Should().HaveCountGreaterThan(10);
        foreach (var name in projects.Select(Path.GetFileNameWithoutExtension))
        {
            text.Split('\n').Count(line => line.Trim() == $"{Id(name!)}[\"{name}\"]").Should().Be(1, name);
        }
    }

    [Fact]
    public void Render_DrawsOneEdgePerProjectReference()
    {
        var text = ModuleMapDoc.Render(Root, "Simulab");

        var expected = SourceProjects()
            .SelectMany(project => XDocument.Load(project).Descendants("ProjectReference")
                .Select(reference => $"    {Id(Path.GetFileNameWithoutExtension(project))} --> {Id(Path.GetFileNameWithoutExtension(reference.Attribute("Include")!.Value.Replace('\\', '/')))}"))
            .Distinct()
            .ToList();
        expected.Should().Contain("    Simulab_Api --> Simulab_Identity_Api");
        text.Split('\n').Where(line => line.Contains(" --> ", StringComparison.Ordinal)).Should().BeEquivalentTo(expected);
    }
}
