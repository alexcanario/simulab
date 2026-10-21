using System.Text.Json;

namespace Simulab.ArchitectureTests;

/// <summary>
/// B-34: MessagePack arrives transitively through Aspire.Hosting (and StreamJsonRpc) in the app host projects, and
/// versions below 2.5.303 carry GHSA-qhrr-8q5h-9q3h. Raising Aspire lifts it; this guard fails when a project resolves
/// MessagePack below the first patched version again. Only this package: a future advisory is still the scan's job.
/// </summary>
public class AspirePackagesTests
{
    private const string Guarded = "MessagePack";

    private static readonly Version Patched = new(2, 5, 303);

    [Fact]
    public void MessagePack_ResolvesToPatchedVersion_InEveryProject()
    {
        var root = SolutionAssemblies.RepositoryRoot();
        var assetFiles = Directory.EnumerateFiles(root, "project.assets.json", SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(Path.GetDirectoryName(path)) == "obj")
            .ToList();

        assetFiles.Should().HaveCountGreaterThan(20, "the rule must have looked at the restored projects of the solution");

        var resolved = assetFiles
            .SelectMany(path => Resolved(path).Select(version => (Project: Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(path))!), Version: version)))
            .ToList();

        resolved.Should().NotBeEmpty("at least one project reaches MessagePack, or the rule reads nothing");
        var offenders = resolved
            .Where(r => r.Version < Patched)
            .Select(r => $"{r.Project}: {Guarded} {r.Version} (needs {Patched} or later)")
            .Distinct()
            .Order()
            .ToList();

        offenders.Should().BeEmpty("raising Aspire (B-34) lifts MessagePack to the first patched version");
    }

    private static IEnumerable<Version> Resolved(string assetsFile)
    {
        using var stream = File.OpenRead(assetsFile);
        using var document = JsonDocument.Parse(stream);
        if (!document.RootElement.TryGetProperty("libraries", out var libraries))
        {
            yield break;
        }

        foreach (var library in libraries.EnumerateObject())
        {
            var separator = library.Name.IndexOf('/', StringComparison.Ordinal);
            if (separator < 0)
            {
                continue;
            }

            if (string.Equals(library.Name[..separator], Guarded, StringComparison.OrdinalIgnoreCase)
                && Version.TryParse(library.Name[(separator + 1)..].Split('-', '+')[0], out var version))
            {
                yield return version;
            }
        }
    }
}
