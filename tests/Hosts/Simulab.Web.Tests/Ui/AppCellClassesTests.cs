using System.Text.RegularExpressions;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-44: a table cell class a page writes (<c>app-cell-secondary</c>) must have a rule in <c>app.css</c>.
/// <c>app-cell-secondary</c> had none, so the scope's state or city ran into the scope's name on screen
/// ("MunicipalSalvador"), and no test could see it: bUnit renders markup, not styles.
/// </summary>
public sealed partial class AppCellClassesTests
{
    private static string Root() => RepositoryRoot();

    internal static IReadOnlyList<string> FindUndefined(string css, IEnumerable<string> markup)
    {
        var defined = DefinedPattern().Matches(css).Select(match => match.Value).ToHashSet(StringComparer.Ordinal);

        return [.. markup
            .SelectMany(text => UsedPattern().Matches(text).Select(match => match.Value))
            .Distinct(StringComparer.Ordinal)
            .Where(name => !defined.Contains(name))
            .Order(StringComparer.Ordinal)];
    }

    [Fact]
    public void EveryCellClassAPageWrites_HasARuleInTheStylesheet()
    {
        var web = Path.Combine(Root(), "src", "Hosts", "Simulab.Web");
        var css = File.ReadAllText(Path.Combine(web, "wwwroot", "app.css"));
        var pages = Directory.EnumerateFiles(web, "*.razor", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText)
            .ToList();

        pages.Should().Contain(text => text.Contains("app-cell-secondary", StringComparison.Ordinal), "the guard must see the class it exists for");
        FindUndefined(css, pages).Should().BeEmpty("a cell class with no rule renders as plain inline text");
    }

    [Fact]
    public void FindUndefined_AClassWithNoRule_IsNamed()
    {
        const string css = ".app-data-table .app-cell-actions { width: 1%; }";

        FindUndefined(css, ["""<td class="app-cell-actions"><span class="app-cell-secondary">x</span></td>"""])
            .Should().Equal("app-cell-secondary");
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Simulab.slnx")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the tests must run inside the repository");
        return directory!.FullName;
    }

    [GeneratedRegex(@"app-cell-[A-Za-z0-9-]+")]
    private static partial Regex UsedPattern();

    [GeneratedRegex(@"(?<=\.)app-cell-[A-Za-z0-9-]+")]
    private static partial Regex DefinedPattern();
}
