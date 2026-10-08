using System.Text.RegularExpressions;

namespace Simulab.ArchitectureTests;

/// <summary>
/// F-56: an Api test host pins every configuration key a developer's local settings can feed. A key left unpinned fails
/// only on the machine that has it set (F-38: <c>Identity:SeedAdmin:Password</c> made ten tests fail), so this reads the
/// test sources instead: each host derived from <c>WebApplicationFactory&lt;Program&gt;</c> sets each pinned key to empty
/// (BR2), and nothing else uses that factory directly (BR3), so there is always a place to pin. Test projects that
/// reference only the Web are not looked at (BR6), and neither is this project, which holds samples of the shapes.
/// </summary>
public class ApiTestHostPinTests
{
    /// <summary>Every key a test host pins, with the reason (BR1).</summary>
    internal static readonly IReadOnlyDictionary<string, string> PinnedKeys = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Identity:SeedAdmin:Password"] = "a password in user secrets or appsettings.Development.json makes the host seed an administrator against a database the test may not have (F-38, F-52)",
        ["Ai:ApiKey"] = "a real Claude key in a test host would spend money; empty makes the gateway answer ai.not_configured (F-41)",
    };

    /// <summary>The hosts that exist today; finding fewer means the scan matches nothing (BR4).</summary>
    private static readonly string[] KnownHosts = ["ApiFactory", "SimulabApiFactory"];

    private const string ApiProject = "Simulab.Api.csproj";

    private static readonly Regex HostDeclaration = new(
        @"\bclass\s+(?<name>\w+)\s*:\s*WebApplicationFactory\s*<\s*Program\s*>",
        RegexOptions.CultureInvariant);

    private static readonly Regex AnyUse = new(@"WebApplicationFactory\s*<\s*Program\s*>", RegexOptions.CultureInvariant);

    /// <summary>The key set to empty: <c>["key"] = string.Empty</c>, <c>["key"] = ""</c> or <c>UseSetting("key", string.Empty)</c>.</summary>
    internal static bool PinsToEmpty(string source, string key)
    {
        var quoted = Regex.Escape(key);
        return Regex.IsMatch(
            source,
            $@"(\[\s*""{quoted}""\s*\]\s*=\s*(string\.Empty|""""))|(UseSetting\s*\(\s*""{quoted}""\s*,\s*(string\.Empty|"""")\s*\))",
            RegexOptions.CultureInvariant);
    }

    /// <summary>The names of the host classes declared in a source text.</summary>
    internal static IReadOnlyList<string> HostNames(string source) =>
        [.. HostDeclaration.Matches(source).Select(match => match.Groups["name"].Value)];

    /// <summary>The pinned keys a source that declares a host leaves unset (BR2); empty when it declares none.</summary>
    internal static IReadOnlyList<string> MissingKeys(string source) =>
        HostNames(source).Count == 0 ? [] : [.. PinnedKeys.Keys.Where(key => !PinsToEmpty(source, key))];

    /// <summary>True when the source uses the factory somewhere other than as the base class of a host (BR3).</summary>
    internal static bool UsesFactoryDirectly(string source) =>
        AnyUse.Count(source) > HostDeclaration.Count(source);

    /// <summary>Test projects that reference the Api (found by their project reference, not by name), except this one.</summary>
    internal static IReadOnlyList<string> ApiTestProjectFolders(string testsRoot, string ownFolder) =>
    [
        .. Directory.EnumerateFiles(testsRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(project => !Path.GetDirectoryName(project)!.Equals(ownFolder, StringComparison.OrdinalIgnoreCase))
            .Where(project => File.ReadAllText(project).Contains(ApiProject, StringComparison.Ordinal))
            .Select(project => Path.GetDirectoryName(project)!),
    ];

    private static IEnumerable<string> Sources(string folder)
    {
        var generated = new[] { $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}" };
        return Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
            .Where(path => !generated.Any(segment => path.Contains(segment, StringComparison.OrdinalIgnoreCase)));
    }

    private static (string TestsRoot, IReadOnlyList<string> Files) ApiTestSources()
    {
        var testsRoot = Path.Combine(SolutionAssemblies.RepositoryRoot(), "tests");
        var own = Path.Combine(testsRoot, "Simulab.ArchitectureTests");
        return (testsRoot, [.. ApiTestProjectFolders(testsRoot, own).SelectMany(Sources)]);
    }

    [Fact]
    public void ApiTestHosts_PinEveryKey()
    {
        var (testsRoot, files) = ApiTestSources();
        var found = new List<string>();
        var problems = new List<string>();

        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            found.AddRange(HostNames(source));
            var relative = Path.GetRelativePath(testsRoot, file);
            problems.AddRange(MissingKeys(source).Select(key => $"{relative}: the test host does not set '{key}' to empty ({PinnedKeys[key]})"));
        }

        string.Join("\n", problems).Should().BeEmpty("every Api test host pins every key developer settings can feed (F-56 BR2)");
        found.Should().Contain(KnownHosts, "a scan that finds no host proves nothing (F-56 BR4)");
    }

    [Fact]
    public void ApiTestProjects_NeverUseTheFactoryDirectly()
    {
        var (testsRoot, files) = ApiTestSources();

        var problems = files
            .Where(file => UsesFactoryDirectly(File.ReadAllText(file)))
            .Select(file => $"{Path.GetRelativePath(testsRoot, file)}: uses WebApplicationFactory<Program> directly; use ApiFactory (Simulab.Api.Tests) or SimulabApiFactory (Simulab.Testing.ApiHost), which pin the keys");

        string.Join("\n", problems).Should().BeEmpty("a direct use has nowhere to pin the keys (F-56 BR3)");
        files.Should().NotBeEmpty("a scan that reads no test source proves nothing (F-56 BR4)");
    }

    [Fact]
    public void BothKnownHosts_PinTheSeedAdminPasswordAndTheAiKey()
    {
        var (_, files) = ApiTestSources();
        var sources = files.Select(File.ReadAllText).ToList();

        foreach (var name in KnownHosts)
        {
            var source = sources.SingleOrDefault(text => HostNames(text).Contains(name));
            source.Should().NotBeNull($"{name} is a known Api test host (F-56 BR4)");
            PinnedKeys.Keys.Should().OnlyContain(key => PinsToEmpty(source!, key), $"{name} sets every pinned key to empty (F-56 BR5)");
        }
    }

    [Fact]
    public void PinnedKeys_AreTheTwoOfTheRule()
    {
        PinnedKeys.Keys.Should().BeEquivalentTo("Identity:SeedAdmin:Password", "Ai:ApiKey");
    }

    private const string PinningHost = """
        public sealed class SampleFactory : WebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseSetting("Ai:ApiKey", string.Empty);
                builder.ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Identity:SeedAdmin:Password"] = "",
                }));
            }
        }
        """;

    [Fact]
    public void MissingKeys_AHostThatPinsBoth_ReportsNothing()
    {
        MissingKeys(PinningHost).Should().BeEmpty();
    }

    [Fact]
    public void MissingKeys_AHostThatForgetsOneKey_NamesIt()
    {
        var forgetful = PinningHost.Replace("\"Ai:ApiKey\"", "\"Ai:Model\"", StringComparison.Ordinal);

        MissingKeys(forgetful).Should().Equal("Ai:ApiKey");
    }

    [Fact]
    public void MissingKeys_AKeySetToAValue_IsNotPinned()
    {
        var valued = PinningHost.Replace("[\"Identity:SeedAdmin:Password\"] = \"\"", "[\"Identity:SeedAdmin:Password\"] = \"Secret-12345!\"", StringComparison.Ordinal);

        MissingKeys(valued).Should().Equal("Identity:SeedAdmin:Password");
    }

    [Fact]
    public void MissingKeys_ASourceWithoutAHost_ReportsNothing()
    {
        MissingKeys("public class SomeTests { }").Should().BeEmpty();
    }

    [Fact]
    public void UsesFactoryDirectly_ADeclaredHost_IsNotDirectUse()
    {
        UsesFactoryDirectly(PinningHost).Should().BeFalse();
    }

    [Theory]
    [InlineData("public class T : IClassFixture<WebApplicationFactory<Program>> { }")]
    [InlineData("var factory = new WebApplicationFactory<Program>();")]
    [InlineData("private readonly WebApplicationFactory<Program> _factory;")]
    [InlineData("public void Run(WebApplicationFactory<Program> factory) { }")]
    public void UsesFactoryDirectly_AFixtureFieldParameterOrNew_IsDirectUse(string source)
    {
        UsesFactoryDirectly(source).Should().BeTrue();
    }

    [Fact]
    public void ApiTestProjectFolders_FindsByProjectReference_AndLeavesWebAndOwnProjectOut()
    {
        var root = Directory.CreateTempSubdirectory("f56-").FullName;
        try
        {
            WriteProject(root, "ApiTests", @"..\src\Simulab.Api\Simulab.Api.csproj");
            WriteProject(root, "WebTests", @"..\src\Simulab.Web\Simulab.Web.csproj");
            WriteProject(root, "Simulab.ArchitectureTests", @"..\src\Simulab.Api\Simulab.Api.csproj");

            var own = Path.Combine(root, "Simulab.ArchitectureTests");

            ApiTestProjectFolders(root, own).Should().Equal(Path.Combine(root, "ApiTests"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void WriteProject(string root, string name, string reference)
    {
        var folder = Directory.CreateDirectory(Path.Combine(root, name)).FullName;
        File.WriteAllText(
            Path.Combine(folder, $"{name}.csproj"),
            $"<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectReference Include=\"{reference}\" /></ItemGroup></Project>");
    }
}
