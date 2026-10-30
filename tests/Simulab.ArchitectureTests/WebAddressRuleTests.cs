namespace Simulab.ArchitectureTests;

/// <summary>
/// F-190 BR1: the rule "an absolute http or https address with a host" has one copy, <c>AppUrlField.IsValid</c>.
/// A page that needs it calls the control and keeps its own error code. Razor markup is not visible through
/// reflection, so the rule reads the source files.
/// </summary>
public class WebAddressRuleTests
{
    private static readonly string[] SchemeTests = ["UriSchemeHttp", "\"http\"", "\"https\""];

    internal static IReadOnlyList<string> FindSchemeTestsInPages(string pagesRoot) =>
    [
        .. Directory.EnumerateFiles(pagesRoot, "*.razor", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => SchemeTests.Any(test => File.ReadAllText(path).Contains(test, StringComparison.Ordinal)))
            .Select(path => Path.GetRelativePath(pagesRoot, path))
    ];

    [Fact]
    public void Pages_DoNotTestTheHttpSchemeThemselves()
    {
        var pages = Path.Combine(SolutionAssemblies.RepositoryRoot(), "src", "Hosts", "Simulab.Web", "Components", "Pages");

        FindSchemeTestsInPages(pages).Should()
            .BeEmpty("a page asks AppUrlField.IsValid, the one copy of the address rule, and keeps its own error code");
    }

    [Fact]
    public void FindSchemeTestsInPages_APageTestingTheScheme_IsNamed()
    {
        var root = Directory.CreateTempSubdirectory("simulab-address-rule-");
        try
        {
            File.WriteAllText(Path.Combine(root.FullName, "Own.razor"), "uri.Scheme == Uri.UriSchemeHttps");
            File.WriteAllText(Path.Combine(root.FullName, "Calls.razor"), "AppUrlField.IsValid(value)");

            FindSchemeTestsInPages(root.FullName).Should().Equal("Own.razor");
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }
}
