using System.Text.Json;

namespace Simulab.ArchitectureTests;

/// <summary>
/// B-29: System.Security.Cryptography.Xml and Pkcs arrive transitively (DataProtection, MimeKit) and 10.0.0 to 10.0.12
/// carry published advisories. Directory.Packages.props pins both at the first patched version; this guard fails when a
/// project resolves one of them below it again. Only these two packages: a future advisory is still the scan's job.
/// </summary>
public class CryptographyPackagesTests
{
    private static readonly string[] Guarded = ["System.Security.Cryptography.Xml", "System.Security.Cryptography.Pkcs"];

    private static readonly Version Patched = new(10, 0, 13);

    [Fact]
    public void XmlAndPkcs_ResolveToPatchedVersion_InEveryProject()
    {
        var root = SolutionAssemblies.RepositoryRoot();
        var assetFiles = Directory.EnumerateFiles(root, "project.assets.json", SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(Path.GetDirectoryName(path)) == "obj")
            .ToList();

        assetFiles.Should().HaveCountGreaterThan(20, "the rule must have looked at the restored projects of the solution");

        var resolved = assetFiles
            .SelectMany(path => Resolved(path).Select(r => (Project: Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(path))!), r.Package, r.Version)))
            .ToList();

        resolved.Should().NotBeEmpty("at least one project reaches the guarded packages, or the rule reads nothing");
        var offenders = resolved
            .Where(r => r.Version < Patched)
            .Select(r => $"{r.Project}: {r.Package} {r.Version} (needs {Patched} or later)")
            .Distinct()
            .Order()
            .ToList();

        offenders.Should().BeEmpty("Directory.Packages.props pins both packages at the first patched version (B-29)");
    }

    private static IEnumerable<(string Package, Version Version)> Resolved(string assetsFile)
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

            var name = library.Name[..separator];
            if (Guarded.Contains(name, StringComparer.OrdinalIgnoreCase)
                && Version.TryParse(library.Name[(separator + 1)..].Split('-', '+')[0], out var version))
            {
                yield return (name, version);
            }
        }
    }
}
