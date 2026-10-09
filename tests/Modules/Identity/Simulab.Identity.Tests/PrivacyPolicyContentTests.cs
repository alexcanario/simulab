using System.Text.Json;
using Microsoft.Extensions.Options;
using Simulab.Identity.Contracts;
using Simulab.Identity.Infrastructure.Content;

namespace Simulab.Identity.Tests;

/// <summary>F-71: the privacy policy that ships in the content folder names where data lives and who processes it.</summary>
public sealed class PrivacyPolicyContentTests
{
    private static readonly string Root = new LegalContentOptions().RootPath;

    private static LegalDocumentProvider Provider() => new(Options.Create(new LegalContentOptions()));

    private static string PolicyPath(string locale, string file) => Path.Combine(Root, locale, "privacy", file);

    // AC1.
    [Theory]
    [InlineData("en")]
    [InlineData("pt-BR")]
    [InlineData("pt-PT")]
    public async Task Current_IsVersionTwoAndStillADraft(string locale)
    {
        var document = await Provider().GetCurrentAsync(LegalTopic.Privacy, locale);

        document.Should().NotBeNull();
        document!.Locale.Should().Be(locale);
        document.Version.Should().Be("2026-v2");
        document.IsPlaceholder.Should().BeTrue();
    }

    // AC2.
    [Theory]
    [InlineData("en")]
    [InlineData("pt-BR")]
    [InlineData("pt-PT")]
    public void Manifest_KeepsVersionOneAndItsFile(string locale)
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(PolicyPath(locale, "manifest.json")));

        var versions = manifest.RootElement.GetProperty("versions").EnumerateArray()
            .Select(v => v.GetProperty("version").GetString())
            .ToList();

        versions.Should().Equal("2026-v1", "2026-v2");
        manifest.RootElement.GetProperty("currentVersion").GetString().Should().Be("2026-v2");
        File.Exists(PolicyPath(locale, "2026-v1.md")).Should().BeTrue();
        File.Exists(PolicyPath(locale, "2026-v2.md")).Should().BeTrue();
    }

    // AC3, with the articles each language writes in its own way.
    [Theory]
    [InlineData("en", "Article 33")]
    [InlineData("pt-BR", "artigo 33")]
    [InlineData("pt-PT", "artigo 33.º")]
    public async Task Current_NamesTheRegionTheProcessorsAndTheTransferBasis(string locale, string lgpdArticle)
    {
        var document = await Provider().GetCurrentAsync(LegalTopic.Privacy, locale);

        document!.BodyHtml.Should()
            .Contain("Brazil South")
            .And.Contain("Microsoft Azure")
            .And.Contain("Anthropic")
            .And.Contain("SendGrid")
            .And.Contain("2026/179")
            .And.Contain("LGPD")
            .And.Contain(lgpdArticle);
    }

    // BR3: the four sections of version one come first, word for word.
    [Theory]
    [InlineData("en")]
    [InlineData("pt-BR")]
    [InlineData("pt-PT")]
    public void VersionTwo_StartsWithTheFourSectionsOfVersionOne(string locale)
    {
        var one = File.ReadAllText(PolicyPath(locale, "2026-v1.md")).ReplaceLineEndings("\n").TrimEnd();
        var two = File.ReadAllText(PolicyPath(locale, "2026-v2.md")).ReplaceLineEndings("\n");

        two.Should().StartWith(one);
    }

    // BR4: the table names, for each processor, what it does, what it receives and where.
    [Theory]
    [InlineData("en")]
    [InlineData("pt-BR")]
    [InlineData("pt-PT")]
    public async Task Current_ListsThreeProcessorsInATable(string locale)
    {
        var document = await Provider().GetCurrentAsync(LegalTopic.Privacy, locale);

        document!.BodyHtml.Should().Contain("<table>");
        System.Text.RegularExpressions.Regex.Matches(document.BodyHtml, "<tr>").Should().HaveCount(4, "a header and three processors");
    }
}
