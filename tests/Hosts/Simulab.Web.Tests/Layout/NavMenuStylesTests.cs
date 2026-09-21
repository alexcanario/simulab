namespace Simulab.Web.Tests.Layout;

/// <summary>
/// Found in F-14 validation: menu items sat side by side because MudBlazor's <c>.mud-tooltip-inline</c> beat our
/// one-class rule. bUnit does not apply CSS, so this pins the rule that keeps one item per line.
/// </summary>
public sealed class NavMenuStylesTests
{
    [Fact]
    public void AppCss_KeepsEveryMenuItemOnItsOwnLine_WithARuleThatOutweighsTheInlineTooltip()
    {
        var css = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hosts", "Simulab.Web", "wwwroot", "app.css")).ReplaceLineEndings("\n");

        css.Should().Contain(".mud-tooltip-root.app-nav-tooltip {\n    display: block;");
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Simulab.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Simulab.slnx not found above the test output.");
    }
}
