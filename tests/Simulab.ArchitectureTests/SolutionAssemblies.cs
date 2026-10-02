using System.Reflection;
using System.Xml.Linq;

namespace Simulab.ArchitectureTests;

/// <summary>The production assemblies the rules run against, and the repository root.</summary>
internal static class SolutionAssemblies
{
    /// <summary>Projects of the solution that the rules do not look at, each with its reason (F-46, BR4).</summary>
    public static readonly IReadOnlyDictionary<string, string> ExemptProjects = new Dictionary<string, string>
    {
        ["Simulab.AppHost"] = "The Aspire orchestrator: referencing it would bring the Aspire.Hosting packages into the architecture tests. "
                              + "ArchitectureOverviewTests checks it from its source.",
    };

    private static readonly Lazy<IReadOnlyList<Assembly>> Derived = new(Derive);

    /// <summary>Every production project of <c>Simulab.slnx</c> except the exempt ones, loaded by name (F-46).</summary>
    public static IReadOnlyList<Assembly> All => Derived.Value;

    public static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Simulab.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Simulab.slnx was not found above the test output folder.");
    }

    private static IReadOnlyList<Assembly> Derive()
    {
        var solution = XDocument.Load(Path.Combine(RepositoryRoot(), "Simulab.slnx"));
        var listed = ProductionProjects.Listed(solution);

        var stale = ProductionProjects.StaleExemptions(listed, ExemptProjects.Keys);
        if (stale.Count > 0)
        {
            throw new InvalidOperationException(ProductionProjects.StaleExemptionMessage(stale));
        }

        var selected = ProductionProjects.Selected(listed, ExemptProjects.Keys);
        var assemblies = new Dictionary<string, Assembly>(StringComparer.Ordinal);
        var unresolved = ProductionProjects.Unresolved(selected, project => TryLoad(project, assemblies));
        if (unresolved.Count > 0)
        {
            throw new InvalidOperationException(ProductionProjects.UnresolvedMessage(unresolved));
        }

        return [.. selected.Select(project => assemblies[project])];
    }

    private static bool TryLoad(string name, Dictionary<string, Assembly> loaded)
    {
        try
        {
            loaded[name] = Assembly.Load(new AssemblyName(name));
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }
}
