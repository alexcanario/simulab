using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Simulab.DocGen;

/// <summary>Project reference graph as a Mermaid flowchart, grouped by the folders under src/.</summary>
internal static partial class ModuleMapDoc
{
    public static string Render(string root, string appName)
    {
        var src = Path.Combine(root, "src");
        var projects = Directory.EnumerateFiles(src, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !IsBuildOutput(Path.GetRelativePath(src, p)))
            .Select(p => new Project(
                Path.GetFullPath(p),
                Path.GetFileNameWithoutExtension(p),
                GroupOf(Path.GetRelativePath(src, p)),
                XDocument.Load(p).Descendants("ProjectReference").Select(e => e.Attribute("Include")!.Value).ToList()))
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToList();
        return Render(appName, projects);
    }

    /// <summary>A project: full path, name, group (`Hosts`, `BuildingBlocks`, `Modules/Identity`) and its raw references.</summary>
    internal sealed record Project(string Path, string Name, string Group, IReadOnlyList<string> References);

    internal static string Render(string appName, IReadOnlyList<Project> projects)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"# {appName} — modules and dependencies\n\nGenerated from the project references under `src/`. Do not edit.\n\n```mermaid\nflowchart LR\n");
        foreach (var group in projects.GroupBy(p => p.Group).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            sb.Append(CultureInfo.InvariantCulture, $"    subgraph {Id(group.Key)}[\"{group.Key}\"]\n");
            foreach (var project in group.OrderBy(p => p.Name, StringComparer.Ordinal))
            {
                sb.Append(CultureInfo.InvariantCulture, $"        {Id(project.Name)}[\"{project.Name}\"]\n");
            }

            sb.Append("    end\n");
        }

        var byPath = projects.ToDictionary(p => p.Path, StringComparer.OrdinalIgnoreCase);
        var edges = projects
            .SelectMany(project => project.References
                .Select(reference => System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(project.Path)!, reference.Replace('\\', System.IO.Path.DirectorySeparatorChar))))
                .Where(byPath.ContainsKey)
                .Select(target => $"    {Id(project.Name)} --> {Id(byPath[target].Name)}\n"))
            .Distinct()
            .Order(StringComparer.Ordinal);
        foreach (var edge in edges)
        {
            sb.Append(edge);
        }

        sb.Append("```\n");
        return sb.ToString();
    }

    // src/Hosts/Simulab.Api/Simulab.Api.csproj -> Hosts; src/Modules/Identity/Simulab.Identity.Api/... -> Modules/Identity.
    private static string GroupOf(string relativeToSrc)
    {
        var folders = relativeToSrc.Split(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
        return folders.Length > 3 ? string.Join('/', folders[..^2]) : folders[0];
    }

    private static bool IsBuildOutput(string relative) =>
        relative.Split(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar).Any(f => f is "bin" or "obj");

    private static string Id(string text) => NotIdentifier().Replace(text, "_");

    [GeneratedRegex("[^A-Za-z0-9_]")]
    private static partial Regex NotIdentifier();
}
