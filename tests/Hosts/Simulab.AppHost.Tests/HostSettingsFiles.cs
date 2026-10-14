using System.Text.Json;

namespace Simulab.AppHost.Tests;

/// <summary>
/// The committed settings a deployed host really loads: its own <c>appsettings.json</c> and
/// <c>appsettings.&lt;Environment&gt;.json</c> (the cloud sets <c>ASPNETCORE_ENVIRONMENT</c>), later file over earlier.
/// The app host's own files are not one of them: they only feed <c>ForwardedHeaders</c> (F-94, what already exists).
/// </summary>
internal static class HostSettingsFiles
{
    /// <summary>The two files of <paramref name="projectDirectory"/> flattened to <c>A:B:C</c> keys; a missing file adds nothing.</summary>
    internal static Dictionary<string, string?> Read(string projectDirectory, string environment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);

        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in new[] { "appsettings.json", $"appsettings.{environment}.json" })
        {
            var path = Path.Combine(projectDirectory, file);
            if (!File.Exists(path))
            {
                continue;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
            Flatten(document.RootElement, prefix: string.Empty, settings);
        }

        return settings;
    }

    private static void Flatten(JsonElement element, string prefix, Dictionary<string, string?> into)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    Flatten(property.Value, prefix.Length == 0 ? property.Name : $"{prefix}:{property.Name}", into);
                }

                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    Flatten(item, $"{prefix}:{index++}", into);
                }

                break;
            case JsonValueKind.Null:
                into[prefix] = null;
                break;
            default:
                into[prefix] = element.ToString();
                break;
        }
    }
}
