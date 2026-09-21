// Generates docs/architecture/ from the code: entity diagrams and data dictionary (EF model), routes (OpenAPI), modules (project references).
// Hand-written text never goes here: every file is overwritten on each run. `--check` fails when the files on disk are stale.
using Simulab.DocGen;

var check = args.Contains("--check");
var root = RepositoryRoot.Find();
var outDir = Path.Combine(root, "docs", "architecture");
var appName = Path.GetFileNameWithoutExtension(Directory.GetFiles(root, "*.slnx").First());
var options = DocGenOptions.Read(Path.Combine(AppContext.BaseDirectory, "docgen.json"));

var generated = DocSet.Generate(root, appName, options, options.Entities || options.DataDictionary ? EntityModels.Load() : []);
var stale = GeneratedDocs.StaleFiles(outDir, generated);
if (check)
{
    if (stale.Count == 0)
    {
        Console.WriteLine("docs/architecture is up to date");
        return 0;
    }

    Console.Error.WriteLine($"docs/architecture is stale ({stale.Count} file(s)): {string.Join(", ", stale)}. Run the generator and commit.");
    return 1;
}

GeneratedDocs.Write(outDir, generated, stale);
var written = stale.Count(generated.ContainsKey);
Console.WriteLine($"docs/architecture: {written} file(s) written, {stale.Count - written} removed, {generated.Count - written} unchanged");
return 0;
