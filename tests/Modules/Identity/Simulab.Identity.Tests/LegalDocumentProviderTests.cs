using Microsoft.Extensions.Options;
using Simulab.Identity.Contracts;
using Simulab.Identity.Infrastructure.Content;

namespace Simulab.Identity.Tests;

/// <summary>AC11 and AC12, on a folder the test writes: manifest, rendering, sanitization, fallback and the draft flag.</summary>
public sealed class LegalDocumentProviderTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("simulab-legal-");

    private LegalDocumentProvider Provider() =>
        new(Options.Create(new LegalContentOptions { RootPath = _root.FullName }));

    private void Write(string locale, string topic, string markdown, string version = "2026-v1", bool placeholder = false)
    {
        var folder = Directory.CreateDirectory(Path.Combine(_root.FullName, locale, topic));
        File.WriteAllText(Path.Combine(folder.FullName, $"{version}.md"), markdown);
        File.WriteAllText(Path.Combine(folder.FullName, "manifest.json"), $$"""
            {
              "currentVersion": "{{version}}",
              "versions": [
                {
                  "version": "{{version}}",
                  "effectiveDate": "2026-09-17",
                  "file": "{{version}}.md",
                  "title": "Terms of use",
                  "isPlaceholder": {{(placeholder ? "true" : "false")}}
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task GetCurrent_ReturnsTheCurrentVersionRenderedToHtml()
    {
        Write("en", "terms", "# Title\n\nA paragraph with **bold** text.");

        var document = await Provider().GetCurrentAsync(LegalTopic.Terms, "en");

        document.Should().NotBeNull();
        document!.Version.Should().Be("2026-v1");
        document.EffectiveDate.Should().Be(new DateOnly(2026, 9, 17));
        document.Title.Should().Be("Terms of use");
        document.Locale.Should().Be("en");
        document.IsPlaceholder.Should().BeFalse();
        document.BodyHtml.Should().Contain("<h1>").And.Contain("<strong>bold</strong>");
    }

    [Fact]
    public async Task GetCurrent_RawHtmlInTheSource_IsNotRenderedAsMarkup()
    {
        Write("en", "terms", "Careful: <script>alert('x')</script> and <iframe src=\"x\"></iframe>.");

        var document = await Provider().GetCurrentAsync(LegalTopic.Terms, "en");

        document!.BodyHtml.Should().NotContain("<script").And.NotContain("<iframe");
        document.BodyHtml.Should().Contain("&lt;script&gt;", "the tag is shown as text, not run");
    }

    [Fact]
    public async Task GetCurrent_PlaceholderVersion_SaysSo()
    {
        Write("pt-BR", "privacy", "Rascunho.", placeholder: true);

        var document = await Provider().GetCurrentAsync(LegalTopic.Privacy, "pt-BR");

        document!.IsPlaceholder.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrent_LocaleWithoutTheDocument_FallsBackToEnglish()
    {
        Write("en", "terms", "English only.");

        var document = await Provider().GetCurrentAsync(LegalTopic.Terms, "pt-PT");

        document!.Locale.Should().Be("en");
    }

    [Fact]
    public async Task GetCurrent_NoDocumentAtAll_ReturnsNull()
    {
        (await Provider().GetCurrentAsync(LegalTopic.Terms, "en")).Should().BeNull();
    }

    [Fact]
    public async Task GetCurrent_ManifestChangedOnDisk_ReturnsTheNewVersion()
    {
        Write("en", "terms", "First.", version: "2026-v1");
        var provider = Provider();
        (await provider.GetCurrentAsync(LegalTopic.Terms, "en"))!.Version.Should().Be("2026-v1");

        // The manifest is the file a lawyer changes; no deployment and no restart (BR14).
        File.SetLastWriteTimeUtc(Path.Combine(_root.FullName, "en", "terms", "manifest.json"), DateTime.UtcNow.AddSeconds(5));
        Write("en", "terms", "Second.", version: "2026-v2");

        (await provider.GetCurrentAsync(LegalTopic.Terms, "en"))!.Version.Should().Be("2026-v2");
    }

    public void Dispose()
    {
        _root.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }
}
