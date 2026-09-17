using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Markdig;
using Microsoft.Extensions.Options;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Infrastructure.Content;

/// <summary>
/// Reads the institutional documents from the file system: one manifest and one Markdown file per
/// locale and topic (BR14). Text a lawyer changes is a file, not a deployment.
/// </summary>
public sealed class LegalDocumentProvider(IOptions<LegalContentOptions> options) : ILegalDocumentProvider
{
    /// <summary>The locale used when a document does not exist in the requested one.</summary>
    public const string FallbackLocale = "en";

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        // The source is a file in this repository, not user input, but a legal document has no reason to
        // carry a script or an iframe: raw HTML stays off.
        .DisableHtml()
        .Build();

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);

    public async Task<LegalDocumentResponse?> GetCurrentAsync(LegalTopic topic, string locale, CancellationToken cancellationToken = default)
    {
        var resolved = ResolveLocale(topic, locale);
        if (resolved is null)
        {
            return null;
        }

        var manifestPath = ManifestPath(topic, resolved);
        var stamp = File.GetLastWriteTimeUtc(manifestPath);

        var key = $"{resolved}/{FolderOf(topic)}";
        if (_cache.TryGetValue(key, out var cached) && cached.Stamp == stamp)
        {
            return cached.Document;
        }

        var document = await ReadAsync(topic, resolved, manifestPath, cancellationToken);
        if (document is not null)
        {
            _cache[key] = new CacheEntry(stamp, document);
        }

        return document;
    }

    private async Task<LegalDocumentResponse?> ReadAsync(LegalTopic topic, string locale, string manifestPath, CancellationToken cancellationToken)
    {
        LegalManifest? manifest;
        await using (var stream = File.OpenRead(manifestPath))
        {
            manifest = await JsonSerializer.DeserializeAsync<LegalManifest>(stream, AppJson.Options, cancellationToken);
        }

        var current = manifest?.Versions.FirstOrDefault(version =>
            string.Equals(version.Version, manifest.CurrentVersion, StringComparison.Ordinal));
        if (current is null)
        {
            return null;
        }

        var bodyPath = Path.Combine(TopicDirectory(topic, locale), current.File);
        if (!File.Exists(bodyPath))
        {
            return null;
        }

        var markdown = await File.ReadAllTextAsync(bodyPath, cancellationToken);

        return new LegalDocumentResponse(
            topic,
            locale,
            current.Version,
            DateOnly.TryParse(current.EffectiveDate, CultureInfo.InvariantCulture, out var effective) ? effective : null,
            current.Title,
            Markdown.ToHtml(markdown, Pipeline),
            current.IsPlaceholder);
    }

    /// <summary>The requested locale when it has the document, otherwise <see cref="FallbackLocale"/>, otherwise none.</summary>
    private string? ResolveLocale(LegalTopic topic, string locale)
    {
        if (!string.IsNullOrWhiteSpace(locale) && File.Exists(ManifestPath(topic, locale)))
        {
            return locale;
        }

        return File.Exists(ManifestPath(topic, FallbackLocale)) ? FallbackLocale : null;
    }

    private string TopicDirectory(LegalTopic topic, string locale) =>
        Path.Combine(options.Value.RootPath, locale, FolderOf(topic));

    private string ManifestPath(LegalTopic topic, string locale) =>
        Path.Combine(TopicDirectory(topic, locale), "manifest.json");

    private static string FolderOf(LegalTopic topic) => topic switch
    {
        LegalTopic.Terms => "terms",
        LegalTopic.Privacy => "privacy",
        _ => throw new ArgumentOutOfRangeException(nameof(topic), topic, "Unknown legal topic.")
    };

    private sealed record CacheEntry(DateTime Stamp, LegalDocumentResponse Document);

    private sealed class LegalManifest
    {
        [JsonPropertyName("currentVersion")]
        public string CurrentVersion { get; set; } = string.Empty;

        [JsonPropertyName("versions")]
        public IList<LegalManifestVersion> Versions { get; init; } = [];
    }

    private sealed class LegalManifestVersion
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("effectiveDate")]
        public string EffectiveDate { get; set; } = string.Empty;

        [JsonPropertyName("file")]
        public string File { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("isPlaceholder")]
        public bool IsPlaceholder { get; set; }
    }
}
