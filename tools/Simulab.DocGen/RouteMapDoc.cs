using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Simulab.DocGen;

/// <summary>Routes per area from the OpenAPI document the Api build writes to docs/api/.</summary>
internal static partial class RouteMapDoc
{
    public static IEnumerable<(string Area, string Text)> Render(string root)
    {
        var file = FindDocument(root);
        if (file is null)
        {
            return [];
        }

        return Render(File.ReadAllText(file), Path.GetRelativePath(root, file).Replace('\\', '/'));
    }

    /// <summary>
    /// One route map per area of `/api/v1/&lt;area&gt;/...`. Paths outside `/api/v1/` are not listed: the map describes
    /// the versioned API only.
    /// </summary>
    public static IEnumerable<(string Area, string Text)> Render(string openApiJson, string source)
    {
        using var document = JsonDocument.Parse(openApiJson);
        if (!document.RootElement.TryGetProperty("paths", out var paths))
        {
            return [];
        }

        var byArea = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var path in paths.EnumerateObject())
        {
            var segments = path.Name.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 3 || segments[0] != "api" || segments[1] != "v1")
            {
                continue;
            }

            var area = Capitalize(segments[2]);
            foreach (var operation in path.Value.EnumerateObject().Where(o => o.Value.ValueKind == JsonValueKind.Object && o.Name != "parameters"))
            {
                var summary = operation.Value.TryGetProperty("summary", out var s) ? s.GetString()
                    : operation.Value.TryGetProperty("operationId", out var id) ? id.GetString() : "";
                var responses = operation.Value.TryGetProperty("responses", out var r) ? string.Join(", ", r.EnumerateObject().Select(x => x.Name)) : "";
                var secured = operation.Value.TryGetProperty("security", out var security) && security.GetArrayLength() > 0 ? "yes" : "";
                if (!byArea.TryGetValue(area, out var rows))
                {
                    byArea[area] = rows = [];
                }

                rows.Add($"| `{operation.Name.ToUpperInvariant()}` | `{path.Name}` | {summary} | {responses} | {secured} |");
            }
        }

        return byArea.Select(pair =>
        {
            var sb = new StringBuilder();
            sb.Append(CultureInfo.InvariantCulture, $"# {pair.Key} — routes\n\nGenerated from `{source}`. Do not edit.\n\n| Verb | Route | Summary | Responses | Auth |\n|---|---|---|---|---|\n");
            foreach (var row in pair.Value.Order(StringComparer.Ordinal))
            {
                sb.Append(row).Append('\n');
            }

            return (pair.Key, sb.ToString());
        }).ToList();
    }

    // The committed document under docs/api/; build output (bin/, obj/) never counts, a stale copy there could win.
    private static string? FindDocument(string root)
    {
        var folder = Path.Combine(root, "docs", "api");
        return Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder, "*.json").Where(f => DocumentName().IsMatch(Path.GetFileName(f))).Order(StringComparer.Ordinal).FirstOrDefault()
            : null;
    }

    private static string Capitalize(string text) =>
        text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..].Replace('-', ' ');

    [GeneratedRegex(@"^(openapi|.*\.Api)(_v1)?\.json$", RegexOptions.IgnoreCase)]
    private static partial Regex DocumentName();
}
