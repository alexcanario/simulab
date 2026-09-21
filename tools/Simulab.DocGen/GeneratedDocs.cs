namespace Simulab.DocGen;

/// <summary>Compares the generated documents with the files on disk and writes the difference.</summary>
internal static class GeneratedDocs
{
    /// <summary>
    /// Relative paths that differ from <paramref name="generated"/>: new or changed files, and files on disk the
    /// generator no longer produces (every file under the folder is generated). Line endings are ignored.
    /// </summary>
    public static IReadOnlyList<string> StaleFiles(string outDir, IReadOnlyDictionary<string, string> generated)
    {
        var stale = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var (relative, text) in generated)
        {
            var file = Path.Combine(outDir, relative);
            var current = File.Exists(file) ? File.ReadAllText(file).Replace("\r\n", "\n", StringComparison.Ordinal) : null;
            if (current != text)
            {
                stale.Add(relative);
            }
        }

        if (Directory.Exists(outDir))
        {
            foreach (var file in Directory.EnumerateFiles(outDir, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(outDir, file).Replace('\\', '/');
                if (!generated.ContainsKey(relative))
                {
                    stale.Add(relative);
                }
            }
        }

        return [.. stale];
    }

    /// <summary>Writes the stale generated files and deletes the ones the generator no longer produces.</summary>
    public static void Write(string outDir, IReadOnlyDictionary<string, string> generated, IEnumerable<string> stale)
    {
        foreach (var relative in stale)
        {
            var file = Path.Combine(outDir, relative);
            if (generated.TryGetValue(relative, out var text))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                File.WriteAllText(file, text);
            }
            else
            {
                File.Delete(file);
            }
        }
    }
}
