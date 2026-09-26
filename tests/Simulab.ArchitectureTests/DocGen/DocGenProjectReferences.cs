using System.Xml.Linq;

namespace Simulab.ArchitectureTests.DocGen;

/// <summary>
/// F-45: the projects DocGen really sees. The tool discovers DbContexts in the assemblies next to it
/// (<c>EntityModels.Load</c>), and the build copies the whole reference closure into its output folder, so the
/// closure of its own project references — not a list written by hand — is what the tests must load too.
/// </summary>
internal static class DocGenProjectReferences
{
    /// <summary>The assembly names of every project the given project references, directly or through another.</summary>
    public static IReadOnlyList<string> Closure(string projectFile)
    {
        var pending = new Queue<string>();
        pending.Enqueue(Path.GetFullPath(projectFile));
        var closure = new SortedSet<string>(StringComparer.Ordinal);
        while (pending.TryDequeue(out var current))
        {
            foreach (var reference in References(current))
            {
                if (closure.Add(Path.GetFileNameWithoutExtension(reference)))
                {
                    pending.Enqueue(reference);
                }
            }
        }

        return [.. closure];
    }

    /// <summary>
    /// The projects of the closure that no loaded assembly answers for: the tests cannot see what DocGen
    /// documents until the project is referenced by the test project too (BR3).
    /// </summary>
    public static IReadOnlyList<string> Unresolved(IEnumerable<string> closure, IEnumerable<string> loadedAssemblies)
    {
        var loaded = loadedAssemblies.ToHashSet(StringComparer.Ordinal);
        return [.. closure.Where(project => !loaded.Contains(project)).Order(StringComparer.Ordinal)];
    }

    /// <summary>What to do about the unresolved projects, in the words of the file that has to change.</summary>
    public static string UnresolvedMessage(IEnumerable<string> unresolved) =>
        $"DocGen references {string.Join(", ", unresolved)}, which no assembly loaded by the architecture tests answers for. "
        + "Add the project to tests/Simulab.ArchitectureTests/Simulab.ArchitectureTests.csproj and to SolutionAssemblies.All, "
        + "or the tests stay blind to what DocGen documents.";

    private static List<string> References(string projectFile)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(projectFile))!;
        return XDocument.Load(projectFile).Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")!.Value.Replace('\\', Path.DirectorySeparatorChar))
            .Select(relative => Path.GetFullPath(Path.Combine(folder, relative)))
            .ToList();
    }
}
