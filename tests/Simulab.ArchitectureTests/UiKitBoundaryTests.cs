namespace Simulab.ArchitectureTests;

/// <summary>
/// Raw table components and library icon constants live only in the UI kit (rule: ui-project).
/// Razor markup is not visible through reflection, so the rule reads the source files.
/// </summary>
public class UiKitBoundaryTests
{
    private static readonly string[] ForbiddenOutsideKit = ["<MudTable", "<MudDataGrid", "Icons.Material"];

    internal static IReadOnlyList<string> FindViolations(string webRoot)
    {
        var kit = Path.Combine(webRoot, "Components", "Ui") + Path.DirectorySeparatorChar;
        var generated = new[] { $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}" };

        return Directory.EnumerateFiles(webRoot, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.StartsWith(kit, StringComparison.OrdinalIgnoreCase))
            .Where(path => !generated.Any(segment => path.Contains(segment, StringComparison.OrdinalIgnoreCase)))
            .SelectMany(path => ForbiddenOutsideKit
                .Where(token => File.ReadAllText(path).Contains(token, StringComparison.Ordinal))
                .Select(token => $"{Path.GetRelativePath(webRoot, path)} uses {token}"))
            .ToList();
    }

    [Fact]
    public void Web_OutsideKit_UsesNoRawTableOrIconConstant()
    {
        var webRoot = Path.Combine(SolutionAssemblies.RepositoryRoot(), "src", "Simulab.Web");

        FindViolations(webRoot).Should().BeEmpty("pages use AppDataTable and AppIcons from Components/Ui");
    }

    [Fact]
    public void FindViolations_FileOutsideKit_NamesTheFile()
    {
        var root = Directory.CreateTempSubdirectory("simulab-ui-kit-");
        try
        {
            var pages = Directory.CreateDirectory(Path.Combine(root.FullName, "Components", "Pages"));
            var kit = Directory.CreateDirectory(Path.Combine(root.FullName, "Components", "Ui"));
            File.WriteAllText(Path.Combine(pages.FullName, "Bad.razor"), "<MudDataGrid T=\"int\" />");
            File.WriteAllText(Path.Combine(pages.FullName, "BadIcon.cs"), "var icon = Icons.Material.Filled.Add;");
            File.WriteAllText(Path.Combine(pages.FullName, "Good.razor"), "<AppDataTable />");
            File.WriteAllText(Path.Combine(kit.FullName, "AppDataTable.razor"), "<MudDataGrid T=\"int\" /> <MudTable />");

            var violations = FindViolations(root.FullName);

            violations.Should().BeEquivalentTo(
            [
                Path.Combine("Components", "Pages", "Bad.razor") + " uses <MudDataGrid",
                Path.Combine("Components", "Pages", "BadIcon.cs") + " uses Icons.Material",
            ]);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }
}
