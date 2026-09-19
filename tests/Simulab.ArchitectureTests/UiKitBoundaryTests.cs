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
        var webRoot = Path.Combine(SolutionAssemblies.RepositoryRoot(), "src", "Hosts", "Simulab.Web");

        FindViolations(webRoot).Should().BeEmpty("pages use AppDataTable and AppIcons from Components/Ui");
    }

    /// <summary>MudNavLink with OnClick renders a div instead of a link (F-2): no href, not a link for assistive technology.</summary>
    internal static IReadOnlyList<string> FindNavLinksWithClick(string webRoot) =>
        [.. Directory.EnumerateFiles(webRoot, "*.razor", SearchOption.AllDirectories)
            .Where(path => System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(path), @"<MudNavLink\b[^>]*\bOnClick\s*="))
            .Select(path => Path.GetRelativePath(webRoot, path))];

    [Fact]
    public void Razor_MudNavLink_HasNoOnClick()
    {
        var webRoot = Path.Combine(SolutionAssemblies.RepositoryRoot(), "src", "Hosts", "Simulab.Web");

        Directory.EnumerateFiles(webRoot, "*.razor", SearchOption.AllDirectories)
            .Should().Contain(path => File.ReadAllText(path).Contains("<MudNavLink", StringComparison.Ordinal), "the rule must match at least one nav link");
        FindNavLinksWithClick(webRoot).Should().BeEmpty("a nav link must stay a link; react to LocationChanged instead");
    }

    [Fact]
    public void FindNavLinksWithClick_LinkWithOnClick_NamesTheFile()
    {
        var root = Directory.CreateTempSubdirectory("simulab-nav-link-");
        try
        {
            File.WriteAllText(Path.Combine(root.FullName, "Bad.razor"), "<MudNavLink Href=\"/\"\n  OnClick=\"Close\">Home</MudNavLink>");
            File.WriteAllText(Path.Combine(root.FullName, "Good.razor"), "<MudNavLink Href=\"/\">Home</MudNavLink> <MudButton OnClick=\"Save\" />");

            FindNavLinksWithClick(root.FullName).Should().Equal("Bad.razor");
        }
        finally
        {
            root.Delete(recursive: true);
        }
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

    /// <summary>
    /// B-6: a library link is the primary blue, under AA contrast on the dark card and on the light background.
    /// Pages use the kit's AppLink; only app-bar links, whose colour comes from the bar, keep MudLink with Color.Inherit.
    /// </summary>
    internal static IReadOnlyList<string> FindRawLinks(string webRoot)
    {
        var kit = Path.Combine(webRoot, "Components", "Ui") + Path.DirectorySeparatorChar;
        return [.. Directory.EnumerateFiles(webRoot, "*.razor", SearchOption.AllDirectories)
            .Where(path => !path.StartsWith(kit, StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(path), @"<MudLink\b[^>]*>")
                .Where(match => !match.Value.Contains("Color=\"Color.Inherit\"", StringComparison.Ordinal))
                .Select(match => $"{Path.GetRelativePath(webRoot, path)}: {match.Value}"))];
    }

    [Fact]
    public void Razor_OutsideKit_UsesAppLinkNotALibraryLink()
    {
        var webRoot = Path.Combine(SolutionAssemblies.RepositoryRoot(), "src", "Hosts", "Simulab.Web");

        // Rule of presence: the app-bar exception exists, so the scan sees links at all.
        Directory.EnumerateFiles(webRoot, "*.razor", SearchOption.AllDirectories)
            .Should().Contain(path => File.ReadAllText(path).Contains("<MudLink", StringComparison.Ordinal), "the rule must match at least one link");
        FindRawLinks(webRoot).Should().BeEmpty("a page link is AppLink, underlined in the text colour (AA in both themes)");
    }
}
