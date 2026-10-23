using System.Text.RegularExpressions;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-148 AC5 (BR7, F-125 AC5): one amber action at most per screen, and F-148 delivers it only in the kit and the gallery.
/// No routed page outside the dev gallery puts <c>Color.Tertiary</c> on a button or uses the action shortcut component
/// until its screen item picks its action.
/// </summary>
public sealed partial class ActionColourScopeTests
{
    [Fact]
    public void RoutedPages_PutNoTertiaryColourOnAButtonAndNoActionShortcut()
    {
        var offenders = RazorFiles()
            .Where(file => !IsKitOrGallery(file) && !IsAllowedPage(file))
            .SelectMany(file => TertiaryButtons(File.ReadAllText(file)).Select(tag => $"{Path.GetFileName(file)}: {tag}")
                .Concat(ActionShortcuts(File.ReadAllText(file)).Select(tag => $"{Path.GetFileName(file)}: {tag}")))
            .ToList();

        offenders.Should().BeEmpty("the amber action waits for each screen item (F-126 to F-134)");
    }

    [Fact]
    public void Gallery_ShowsTheActionShortcutAndItsCompactVariant()
    {
        var gallery = File.ReadAllText(Path.Combine(WebRoot(), "Components", "Pages", "Dev", "UiGallery.razor"));

        ActionShortcuts(gallery).Should().HaveCountGreaterThanOrEqualTo(2, "the gallery shows the shortcut and the compact variant (BR7, BR8)");
        ActionShortcuts(gallery).Should().Contain(tag => tag.Contains("Compact", StringComparison.Ordinal));
    }

    /// <summary>The scan finds the component however it is written: self-closing, with children, over several lines.</summary>
    [Fact]
    public void ActionShortcuts_FindsEveryWayToWriteIt()
    {
        const string razor = """
            <AppActionButton Label="@L["Start"]" />
            <AppActionButton
                Label="Go" Compact="true"></AppActionButton>
            <AppLink Href="/x">Not an action</AppLink>
            """;

        ActionShortcuts(razor).Should().HaveCount(2);
    }

    /// <summary>F-126 BR11: the pages that picked their amber action; each screen item (F-127 to F-134) adds its own here.</summary>
    private static readonly string[] AllowedPages = ["/QuestionBank/QuestionForm.razor"];

    private static bool IsAllowedPage(string file) =>
        AllowedPages.Any(page => file.Replace('\\', '/').EndsWith(page, StringComparison.Ordinal));

    private static bool IsKitOrGallery(string file)
    {
        var path = file.Replace('\\', '/');
        return path.Contains("/Components/Pages/Dev/", StringComparison.Ordinal) || path.Contains("/Components/Ui/", StringComparison.Ordinal);
    }

    internal static IReadOnlyList<string> ActionShortcuts(string razor) =>
        ActionTagPattern().Matches(razor)
            .Select(match => string.Join(' ', match.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)))
            .ToList();

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

    [GeneratedRegex(@"<AppActionButton\b(?:[^>""]|""[^""]*"")*>", RegexOptions.Singleline)]
    private static partial Regex ActionTagPattern();
}
