using System.Text.RegularExpressions;

namespace Simulab.ArchitectureTests;

/// <summary>
/// F-23: the hand-written C4 overview (<c>docs/architecture-overview.md</c>) stays in step with the app host. Every
/// resource the app host starts is a node of the containers diagram, with the resource name as its id (BR4), and a
/// node in the <c>planned</c> class is not a resource yet (BR5): once it is built, the mark has to go.
/// </summary>
public class ArchitectureOverviewTests
{
    private const string OverviewPath = "docs/architecture-overview.md";

    /// <summary>A resource added on the builder: <c>builder.AddRedis("redis")</c>, <c>builder.AddProject&lt;T&gt;("api")</c>.</summary>
    private static readonly Regex AppHostResource = new(
        @"\bbuilder\s*\.\s*Add(?<kind>\w+)\s*(<[^>]+>)?\s*\(\s*""(?<name>[^""]+)""",
        RegexOptions.CultureInvariant);

    private static readonly Regex MermaidBlock = new(
        @"^```mermaid\s*\n(?<body>.*?)^```", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.CultureInvariant);

    /// <summary>A node declaration: <c>web["..."]</c>, <c>redis[("...")]</c>; a <c>subgraph</c> line is not one.</summary>
    private static readonly Regex NodeDeclaration = new(@"^\s*(?<id>\w+)[\[(]", RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly Regex ClassStatement = new(
        @"^\s*class\s+(?<ids>[\w,]+)\s+(?<class>\w+)\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant);

    /// <summary>The names of the resources the app host starts. Parameters are not resources; databases hang off a server.</summary>
    internal static IReadOnlySet<string> AppHostResources(string appHostSource) =>
        AppHostResource.Matches(appHostSource)
            .Where(match => match.Groups["kind"].Value != "Parameter")
            .Select(match => match.Groups["name"].Value)
            .ToHashSet(StringComparer.Ordinal);

    internal static IReadOnlyList<string> MermaidDiagrams(string markdown) =>
        [.. MermaidBlock.Matches(markdown).Select(match => match.Groups["body"].Value)];

    internal static IReadOnlySet<string> NodeIds(string diagram) =>
        NodeDeclaration.Matches(diagram).Select(match => match.Groups["id"].Value).ToHashSet(StringComparer.Ordinal);

    internal static IReadOnlySet<string> ClassMembers(string diagram, string className) =>
        ClassStatement.Matches(diagram)
            .Where(match => match.Groups["class"].Value == className)
            .SelectMany(match => match.Groups["ids"].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>Where the containers diagram and the app host disagree, one line each, naming the resource or node.</summary>
    internal static IReadOnlyList<string> DriftProblems(string appHostSource, string containersDiagram)
    {
        var resources = AppHostResources(appHostSource);
        var nodes = NodeIds(containersDiagram);
        var planned = ClassMembers(containersDiagram, "planned");

        return
        [
            .. resources.Where(resource => !nodes.Contains(resource)).Order(StringComparer.Ordinal)
                .Select(resource => $"app host resource '{resource}' is not a node of the containers diagram"),
            .. planned.Where(resources.Contains).Order(StringComparer.Ordinal)
                .Select(node => $"node '{node}' is marked planned, but the app host starts it"),
        ];
    }

    private static string ReadRepositoryFile(string relativePath) =>
        File.ReadAllText(Path.Combine(SolutionAssemblies.RepositoryRoot(), relativePath)).Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string Overview() => ReadRepositoryFile(OverviewPath);

    private static string AppHost() => ReadRepositoryFile("src/Hosts/Simulab.AppHost/AppHost.cs");

    private static string ContextDiagram() => MermaidDiagrams(Overview())[0];

    private static string ContainersDiagram() => MermaidDiagrams(Overview())[1];

    [Fact]
    public void Overview_ContainersDiagram_MatchesTheAppHost()
    {
        DriftProblems(AppHost(), ContainersDiagram()).Should().BeEmpty(
            "every app host resource is a container node, and a built piece is no longer marked planned (F-23 BR4, BR5)");
    }

    [Fact]
    public void AppHostResources_TheRealAppHost_FindsEveryStartedResource()
    {
        // Guards the guard: a parser that finds nothing would let the drift test pass for free.
        AppHostResources(AppHost()).Should().BeEquivalentTo(["postgres", "mailpit", "redis", "api", "web"]);
    }

    [Fact]
    public void AppHostResources_ParametersAndDatabases_AreNotResources()
    {
        const string source = """
            var password = builder.AddParameter("postgres-password", "postgres", secret: true);
            var postgres = builder.AddPostgres("postgres", password: password);
            var database = postgres.AddDatabase("simulab");
            var api = builder.AddProject<Projects.Simulab_Api>("api");
            """;

        AppHostResources(source).Should().BeEquivalentTo(["postgres", "api"]);
    }

    [Fact]
    public void DriftProblems_ResourceMissingFromTheDiagram_NamesTheResource()
    {
        const string appHost = """
            var redis = builder.AddRedis("redis");
            var azurite = builder.AddAzureStorage("storage");
            """;
        const string diagram = """
            flowchart TB
                redis[("Redis")]
                class redis container
            """;

        DriftProblems(appHost, diagram).Should().Equal("app host resource 'storage' is not a node of the containers diagram");
    }

    [Fact]
    public void DriftProblems_PlannedNodeThatIsBuilt_NamesTheNode()
    {
        const string appHost = """
            var blob = builder.AddAzureStorage("blob");
            """;
        const string diagram = """
            flowchart TB
                blob[("File storage")]
                claude["Claude API"]
                class claude,blob planned
            """;

        DriftProblems(appHost, diagram).Should().Equal("node 'blob' is marked planned, but the app host starts it");
    }

    [Fact]
    public void Overview_Sections_AppearOnceEachWithTwoDiagrams()
    {
        var overview = Overview();

        MermaidDiagrams(overview).Should().HaveCount(2, "context and containers; components are the generated modules.md (F-23 BR2)");
        foreach (var heading in new[] { "## 1. System context\n", "## 2. Containers\n", "## 3. Inside the Api\n" })
        {
            Regex.Count(overview, Regex.Escape(heading)).Should().Be(1, $"'{heading.Trim()}' is one section");
        }
    }

    [Fact]
    public void Overview_ContextDiagram_ShowsRolesSystemAndExternals()
    {
        var diagram = ContextDiagram();

        NodeIds(diagram).Should().BeEquivalentTo(["student", "curator", "admin", "simulab", "email", "claude", "google"]);
        ClassMembers(diagram, "person").Should().BeEquivalentTo(["student", "curator", "admin"]);
        ClassMembers(diagram, "planned").Should().BeEquivalentTo(["claude"]);
        ClassMembers(diagram, "optional").Should().BeEquivalentTo(["google"]);
    }

    [Fact]
    public void Overview_ContainersDiagram_ShowsContainersAndPlannedPieces()
    {
        var diagram = ContainersDiagram();

        NodeIds(diagram).Should().BeEquivalentTo(["people", "web", "redis", "api", "postgres", "blob", "mailpit", "claude"]);
        ClassMembers(diagram, "container").Should().BeEquivalentTo(["web", "api", "postgres", "redis"]);
        ClassMembers(diagram, "planned").Should().BeEquivalentTo(["blob", "claude"]);
        Overview().Should().Contain("(architecture/modules.md)", "the component level is the generated module map (F-23 BR2)");
    }

    [Fact]
    public void Overview_LivesOutsideTheGeneratedFolder_AndIsLinkedFromClaudeMdAndInfra()
    {
        // DocGen deletes every file under docs/architecture/ it does not generate (F-23 BR1).
        OverviewPath.Should().NotStartWith("docs/architecture/");
        File.Exists(Path.Combine(SolutionAssemblies.RepositoryRoot(), OverviewPath)).Should().BeTrue();
        ReadRepositoryFile("CLAUDE.md").Should().Contain("`docs/architecture-overview.md`");
        ReadRepositoryFile("docs/infra.md").Should().Contain("(architecture-overview.md)");
    }
}
