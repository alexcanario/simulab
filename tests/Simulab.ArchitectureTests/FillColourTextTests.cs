using System.Text.RegularExpressions;

namespace Simulab.ArchitectureTests;

/// <summary>
/// F-17 BR5: secondary, tertiary, success, warning and info are fill colours. As text on a card or on the page
/// they read under 4.5:1 (success 3.33:1, warning 2.74:1 in the light theme), so no screen writes text in them;
/// their contrast text on the fill is fine. A palette test cannot see how a screen uses a colour, so this reads
/// the source files.
/// </summary>
public class FillColourTextTests
{
    private const string FillNames = "Secondary|Tertiary|Success|Warning|Info";

    /// <summary>A text component in a fill colour; a filled button paints the colour as its background, so it is allowed.</summary>
    private static readonly Regex TextComponent = new(@"<(MudText|MudLink|MudButton)\b[^>]*>", RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private static readonly Regex FillColourParameter = new($@"\bColor\s*=\s*""Color\.({FillNames})""", RegexOptions.CultureInvariant);

    /// <summary>A CSS <c>color</c> declaration in a fill colour or its tones; its contrast text is allowed.</summary>
    private static readonly Regex FillColourDeclaration = new(
        $@"(?<![-\w])color\s*:\s*var\(\s*--mud-palette-({FillNames.ToLowerInvariant()})(-darken|-lighten)?\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    internal static IReadOnlyList<string> FindFillColourText(string webRoot)
    {
        var generated = new[] { $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}" };

        return [.. Directory.EnumerateFiles(webRoot, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
            .Where(path => !generated.Any(segment => path.Contains(segment, StringComparison.OrdinalIgnoreCase)))
            .SelectMany(path => Violations(File.ReadAllText(path)).Select(found => $"{Path.GetRelativePath(webRoot, path)}: {found}"))];
    }

    private static IEnumerable<string> Violations(string source)
    {
        var components = TextComponent.Matches(source)
            .Select(match => match.Value)
            .Where(tag => FillColourParameter.IsMatch(tag))
            .Where(tag => !tag.Contains("Variant=\"Variant.Filled\"", StringComparison.Ordinal));
        var declarations = FillColourDeclaration.Matches(source).Select(match => match.Value);
        return components.Concat(declarations);
    }

    [Fact]
    public void Web_WritesNoTextInAFillColour()
    {
        var webRoot = Path.Combine(SolutionAssemblies.RepositoryRoot(), "src", "Hosts", "Simulab.Web");

        FindFillColourText(webRoot).Should().BeEmpty("secondary, tertiary, success, warning and info are fills, under AA as text (F-17 BR5)");
    }

    [Fact]
    public void FindFillColourText_TextInAFillColour_NamesTheFile()
    {
        var root = Directory.CreateTempSubdirectory("simulab-fill-text-");
        try
        {
            File.WriteAllText(Path.Combine(root.FullName, "Text.razor"), "<MudText Typo=\"Typo.body1\"\n  Color=\"Color.Success\">Saved</MudText>");
            File.WriteAllText(Path.Combine(root.FullName, "Link.razor"), "<MudLink Href=\"/\" Color=\"Color.Info\">More</MudLink>");
            File.WriteAllText(Path.Combine(root.FullName, "Outlined.razor"), "<MudButton Variant=\"Variant.Outlined\" Color=\"Color.Warning\">Retry</MudButton>");
            File.WriteAllText(Path.Combine(root.FullName, "Style.css"), ".note { color: var(--mud-palette-tertiary); }");
            File.WriteAllText(Path.Combine(root.FullName, "Hover.css"), ".note:hover { color:var(--mud-palette-secondary-darken) }");
            File.WriteAllText(
                Path.Combine(root.FullName, "Good.razor"),
                "<MudButton Variant=\"Variant.Filled\" Color=\"Color.Success\">Save</MudButton> <MudText Color=\"Color.Primary\">Title</MudText> <MudIcon Color=\"Color.Warning\" />");
            File.WriteAllText(
                Path.Combine(root.FullName, "Good.css"),
                ".dot { background-color: var(--mud-palette-success); border-color: var(--mud-palette-info); color: var(--mud-palette-success-contrast-text); }");

            FindFillColourText(root.FullName).Select(found => found.Split(':')[0]).Should().BeEquivalentTo(
                ["Text.razor", "Link.razor", "Outlined.razor", "Style.css", "Hover.css"]);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }
}
