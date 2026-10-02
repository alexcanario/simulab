using System.Xml.Linq;

namespace Simulab.ArchitectureTests;

/// <summary>
/// F-46: the production projects the architecture rules run against, read from <c>Simulab.slnx</c> instead of a list
/// written by hand. The functions are pure (solution text and names in, names and messages out) so they are tested
/// without the real file.
/// </summary>
internal static class ProductionProjects
{
    private static readonly string[] ProductionRoots = ["src/", "tools/"];

    /// <summary>The assembly name of every <c>Project</c> of the solution under <c>src/</c> or <c>tools/</c> (BR1, BR2).</summary>
    public static IReadOnlyList<string> Listed(XDocument solution) =>
        [.. solution.Descendants("Project")
            .Select(project => project.Attribute("Path")!.Value.Replace('\\', '/'))
            .Where(path => ProductionRoots.Any(root => path.StartsWith(root, StringComparison.Ordinal)))
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Order(StringComparer.Ordinal)];

    /// <summary>The listed projects the rules look at: everything production except the exempt ones (BR1, BR4).</summary>
    public static IReadOnlyList<string> Selected(IEnumerable<string> listed, IEnumerable<string> exempt) =>
        [.. listed.Except(exempt, StringComparer.Ordinal)];

    /// <summary>The exemptions whose project the solution no longer lists (BR6).</summary>
    public static IReadOnlyList<string> StaleExemptions(IEnumerable<string> listed, IEnumerable<string> exempt) =>
        [.. exempt.Except(listed, StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    /// <summary>The selected projects whose assembly cannot be loaded by the test project (BR5).</summary>
    public static IReadOnlyList<string> Unresolved(IEnumerable<string> selected, Func<string, bool> canLoad) =>
        [.. selected.Where(project => !canLoad(project)).Order(StringComparer.Ordinal)];

    /// <summary>What to do about the unresolved projects, in the words of the file that has to change.</summary>
    public static string UnresolvedMessage(IEnumerable<string> unresolved) =>
        $"The solution lists {string.Join(", ", unresolved)}, which the architecture tests cannot load. "
        + "Add a ProjectReference to the project in tests/Simulab.ArchitectureTests/Simulab.ArchitectureTests.csproj, "
        + "or the rules stay blind to it (or list it as exempt, with its reason, in SolutionAssemblies.ExemptProjects).";

    /// <summary>What to do about an exemption that outlived its project.</summary>
    public static string StaleExemptionMessage(IEnumerable<string> stale) =>
        $"SolutionAssemblies.ExemptProjects exempts {string.Join(", ", stale)}, which Simulab.slnx no longer lists. "
        + "Remove the entry: an exemption never outlives its project.";
}
