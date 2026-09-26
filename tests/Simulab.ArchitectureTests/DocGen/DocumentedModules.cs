using Simulab.DocGen;

namespace Simulab.ArchitectureTests.DocGen;

/// <summary>
/// F-45: the modules DocGen has written under <c>docs/architecture/</c>, and the drift between them and the
/// modules the tests load. The committed folders are the truth for what the generator documents: the gate runs
/// <c>DocGen --check</c>, while a <c>bin</c> folder on disk can be from any earlier build.
/// </summary>
internal static class DocumentedModules
{
    private static readonly string[] ModuleFiles = ["schema.dbml", "data-dictionary.md"];

    /// <summary>
    /// A folder counts as a module only when it holds a schema or a data dictionary: <c>System/</c> holds
    /// <c>routes.md</c> alone and is no module. With entities and the data dictionary both off DocGen writes no
    /// module folder at all, so nothing is expected (BR5).
    /// </summary>
    public static IReadOnlyList<string> In(string architectureDirectory, DocGenOptions options)
    {
        if (!options.Entities && !options.DataDictionary)
        {
            return [];
        }

        return
        [
            .. Directory.EnumerateDirectories(architectureDirectory)
                .Where(folder => ModuleFiles.Any(file => File.Exists(Path.Combine(folder, file))))
                .Select(Path.GetFileName)
                .OfType<string>()
                .Order(StringComparer.Ordinal)
        ];
    }

    /// <summary>
    /// One line per module that only one side has, each naming the side and what to do (BR4). Empty means the
    /// generator and the tests see the same modules.
    /// </summary>
    public static IReadOnlyList<string> Drift(IEnumerable<string> documented, IEnumerable<string> loaded)
    {
        var onDisk = documented.ToHashSet(StringComparer.Ordinal);
        var inTests = loaded.ToHashSet(StringComparer.Ordinal);
        return
        [
            .. onDisk.Except(inTests).Order(StringComparer.Ordinal).Select(module =>
                $"{module}: DocGen documents docs/architecture/{module}/ and no test loads its model — its assembly reaches "
                + "DocGen's output folder without a project reference; add the project to tools/Simulab.DocGen/Simulab.DocGen.csproj"),
            .. inTests.Except(onDisk).Order(StringComparer.Ordinal).Select(module =>
                $"{module}: the tests load a model for {module} and docs/architecture/{module}/ does not exist — run "
                + "dotnet run --project tools/Simulab.DocGen and commit what it writes")
        ];
    }
}
