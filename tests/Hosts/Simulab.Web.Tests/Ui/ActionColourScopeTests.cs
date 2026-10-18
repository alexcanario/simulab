using System.Text.RegularExpressions;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-125 AC5 (BR5): one orange action at most per screen, and F-125 delivers it only in the kit and the gallery. No routed
/// page outside the dev gallery puts <c>Color.Tertiary</c> on a button until its screen item picks its action.
/// </summary>
public sealed partial class ActionColourScopeTests
{
    [Fact]
    public void RoutedPages_PutNoTertiaryColourOnAButton()
    {
        var offenders = RazorFiles()
            .Where(file => !file.Replace('\\', '/').Contains("/Components/Pages/Dev/", StringComparison.Ordinal))
            .SelectMany(file => TertiaryButtons(File.ReadAllText(file)).Select(tag => $"{Path.GetFileName(file)}: {tag}"))
            .ToList();

        offenders.Should().BeEmpty("the orange action waits for each screen item (F-126 to F-134)");
    }

    [Fact]
    public void Gallery_ShowsTheActionVariant()
    {
        var gallery = File.ReadAllText(Path.Combine(WebRoot(), "Components", "Pages", "Dev", "UiGallery.razor"));

        TertiaryButtons(gallery).Should().NotBeEmpty("the gallery shows the action variant (BR5)");
    }

    /// <summary>Each offending way: a filled tertiary button, an icon button, and the colour set on a separate line.</summary>
    [Fact]
    public void TertiaryButtons_FindsEveryWayToWriteIt()
    {
        const string razor = """
            <MudButton Variant="Variant.Filled" Color="Color.Tertiary">Start</MudButton>
            <MudIconButton Icon="@AppIcons.Add" Color="Color.Tertiary" />
            <MudButton Variant="Variant.Filled"
                       OnClick="@(() => Go())"
                       Color="Color.Tertiary">Go</MudButton>
            <MudButton Color="Color.Primary">Save</MudButton>
            <MudText Color="Color.Tertiary">Not a button</MudText>
            """;

        TertiaryButtons(razor).Should().HaveCount(3);
    }

    internal static IReadOnlyList<string> TertiaryButtons(string razor) =>
        ButtonTagPattern().Matches(razor)
            .Select(match => match.Value)
            .Where(tag => tag.Contains("Color.Tertiary", StringComparison.Ordinal))
            .Select(tag => string.Join(' ', tag.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)))
            .ToList();

    private static IEnumerable<string> RazorFiles() =>
        Directory.EnumerateFiles(WebRoot(), "*.razor", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    private static string WebRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Simulab.slnx")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the tests must run inside the repository");
        return Path.Combine(directory!.FullName, "src", "Hosts", "Simulab.Web");
    }

    // A quoted attribute may hold '>' (OnClick="@(() => Go())"), so quotes are read whole.
    [GeneratedRegex(@"<Mud(Icon)?Button\b(?:[^>""]|""[^""]*"")*>", RegexOptions.Singleline)]
    private static partial Regex ButtonTagPattern();
}
