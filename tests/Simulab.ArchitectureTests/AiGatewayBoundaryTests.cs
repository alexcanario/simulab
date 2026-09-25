using System.Reflection;
using System.Xml.Linq;

namespace Simulab.ArchitectureTests;

/// <summary>F-41 AC10, BR1: the Claude API has one door, and only the Ai building block opens it.</summary>
public class AiGatewayBoundaryTests
{
    private const string GatewayAssembly = "Simulab.Ai";

    private const string ClaudeSdk = "Anthropic";

    private static bool IsClaudeSdk(string name) =>
        name.Equals(ClaudeSdk, StringComparison.Ordinal)
        || name.StartsWith(ClaudeSdk + ".", StringComparison.Ordinal);

    [Fact]
    public void OnlyTheAiBuildingBlock_ReferencesTheClaudeSdk()
    {
        var referencing = SolutionAssemblies.All
            .Where(assembly => assembly.GetReferencedAssemblies().Any(reference => IsClaudeSdk(reference.Name!)))
            .Select(assembly => assembly.GetName().Name)
            .ToList();

        // Rule of presence: the gateway really does reference it, so an empty result cannot pass by accident.
        referencing.Should().Equal(GatewayAssembly);
    }

    [Fact]
    public void OnlyTheAiBuildingBlock_DeclaresThePackage()
    {
        var root = SolutionAssemblies.RepositoryRoot();
        var projects = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "tests"), "*.csproj", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToList();

        projects.Should().NotBeEmpty("the rule must have read the real projects");

        var declaring = projects
            .Where(path => XDocument.Load(path)
                .Descendants("PackageReference")
                .Select(reference => reference.Attribute("Include")?.Value ?? string.Empty)
                .Any(IsClaudeSdk))
            .Select(Path.GetFileNameWithoutExtension)
            .ToList();

        declaring.Should().Equal(GatewayAssembly);
    }

    [Fact]
    public void TheGateway_HasExactlyOneImplementation()
    {
        var gateway = typeof(Simulab.Ai.IAiGateway);

        var implementations = SolutionAssemblies.All
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsAbstract: false, IsInterface: false } && type.IsAssignableTo(gateway))
            .Select(type => type.Name)
            .ToList();

        implementations.Should().Equal(nameof(Simulab.Ai.AnthropicAiGateway));
    }

    [Fact]
    public void TheWeb_DoesNotReferenceTheGateway()
    {
        var web = SolutionAssemblies.All.Single(assembly => assembly.GetName().Name == "Simulab.Web");

        References(web).Should().NotContain(GatewayAssembly, "the UI host talks to the Api, never to a building block that carries the model");
    }

    private static IReadOnlyList<string> References(Assembly assembly) =>
        [.. assembly.GetReferencedAssemblies().Select(reference => reference.Name!)];
}
