using System.Globalization;
using System.Text;

namespace Simulab.DocGen;

/// <summary>docs/architecture/README.md: links to every generated file.</summary>
internal static class IndexDoc
{
    public static string Render(string appName, IEnumerable<string> files)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"# {appName} — technical documentation\n\nGenerated from the code by `tools/{appName}.DocGen` at every ship. Do not edit by hand: change the code, run the tool.\n\n- Regenerate: `dotnet run --project tools/{appName}.DocGen`\n- Check (exit 1 when stale): `dotnet run --project tools/{appName}.DocGen -- --check`\n\n");
        foreach (var file in files.Where(f => f != "README.md").Order(StringComparer.Ordinal))
        {
            var hint = file.EndsWith(".dbml", StringComparison.Ordinal) ? $" — {EntityModels.SchemaViewerHint}" : "";
            sb.Append(CultureInfo.InvariantCulture, $"- [{file}]({file}){hint}\n");
        }

        return sb.ToString();
    }
}
