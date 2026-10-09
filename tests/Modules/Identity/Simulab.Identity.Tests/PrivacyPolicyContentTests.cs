using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Simulab.Identity.Contracts;
using Simulab.Identity.Infrastructure.Content;

namespace Simulab.Identity.Tests;

/// <summary>F-112: the privacy policy that ships in the content folder names the real region and processors (F-71 wrote the structure).</summary>
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
    public async Task Current_IsVersionThreeAndStillADraft(string locale)
    {
        var document = await Provider().GetCurrentAsync(LegalTopic.Privacy, locale);

        document.Should().NotBeNull();
        document!.Locale.Should().Be(locale);
        document.Version.Should().Be("2026-v3");
        document.IsPlaceholder.Should().BeTrue();
    }

    // AC2: the earlier versions and their files stay, because consent records point at them (F-71 D4).
    [Theory]
    [InlineData("en")]
    [InlineData("pt-BR")]
    [InlineData("pt-PT")]
    public void Manifest_KeepsEarlierVersionsAndTheirFiles(string locale)
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(PolicyPath(locale, "manifest.json")));

        var versions = manifest.RootElement.GetProperty("versions").EnumerateArray()
            .Select(v => v.GetProperty("version").GetString())
            .ToList();

        versions.Should().Equal("2026-v1", "2026-v2", "2026-v3");
        manifest.RootElement.GetProperty("currentVersion").GetString().Should().Be("2026-v3");
        foreach (var version in versions)
        {
            File.Exists(PolicyPath(locale, $"{version}.md")).Should().BeTrue();
        }
    }

    // AC3, with the articles each language writes in its own way.
    [Theory]
    [InlineData("en", "Article 33")]
    [InlineData("pt-BR", "artigo 33")]
    [InlineData("pt-PT", "artigo 33.º")]
    public async Task Current_NamesTheRealRegionAndProcessors(string locale, string lgpdArticle)
    {
        var document = await Provider().GetCurrentAsync(LegalTopic.Privacy, locale);

        document!.BodyHtml.Should()
            .Contain("Central US")
            .And.Contain("Microsoft Azure")
            .And.Contain("Microsoft Azure Communication Services")
            .And.Contain("Anthropic")
            .And.Contain("Data Privacy Framework")
            .And.Contain("2026/179")
            .And.Contain("LGPD")
            .And.Contain(lgpdArticle);
    }

    // AC3: what 2026-v2 said and is no longer true (the region of F-71 and its email provider).
    [Theory]
    [InlineData("en")]
    [InlineData("pt-BR")]
    [InlineData("pt-PT")]
    public async Task Current_NamesNeitherTheOldRegionNorTheOldEmailProvider(string locale)
    {
        var document = await Provider().GetCurrentAsync(LegalTopic.Privacy, locale);

        document!.BodyHtml.Should().NotContain("Brazil South").And.NotContain("SendGrid");
    }

    // AC4 and BR4: a header and the three processors.
    [Theory]
    [InlineData("en")]
    [InlineData("pt-BR")]
    [InlineData("pt-PT")]
    public async Task Current_ListsThreeProcessorsInATable(string locale)
    {
        var document = await Provider().GetCurrentAsync(LegalTopic.Privacy, locale);

        document!.BodyHtml.Should().Contain("<table>");
        Regex.Matches(document.BodyHtml, "<tr>").Should().HaveCount(4, "a header and three processors");
    }

    // AC4 and BR6: the code sends nothing to Anthropic today, so the row says so (one marker per language).
    [Theory]
    [InlineData("en", "Today no data of yours is sent to it")]
    [InlineData("pt-BR", "Hoje nenhum dado seu é enviado a ela")]
    [InlineData("pt-PT", "Hoje nenhum dado seu é enviado a esta empresa")]
    public async Task Current_SaysAnthropicReceivesNothingToday(string locale, string marker)
    {
        var document = await Provider().GetCurrentAsync(LegalTopic.Privacy, locale);

        var anthropicRow = Regex.Matches(document!.BodyHtml, "<tr>.*?</tr>", RegexOptions.Singleline)
            .Select(m => m.Value)
            .Single(row => row.Contains("Anthropic"));
        anthropicRow.Should().Contain(marker);
    }

    // BR3 and AC5: the four sections of version one come first, word for word.
    [Theory]
    [InlineData("en")]
    [InlineData("pt-BR")]
    [InlineData("pt-PT")]
    public void VersionThree_StartsWithTheFourSectionsOfVersionOne(string locale)
    {
        var one = File.ReadAllText(PolicyPath(locale, "2026-v1.md")).ReplaceLineEndings("\n").TrimEnd();
        var three = File.ReadAllText(PolicyPath(locale, "2026-v3.md")).ReplaceLineEndings("\n");

        three.Should().StartWith(one);
    }

    // BR5 and D2: a basis that is not in place yet is said to be so, never written as if it were (one marker per language).
    [Theory]
    [InlineData("en", "being put in place", "test accounts only")]
    [InlineData("pt-BR", "estão sendo formalizadas", "só tem contas de teste")]
    [InlineData("pt-PT", "Estão a ser formalizadas", "só tem contas de teste")]
    public async Task Current_SaysSafeguardsAreBeingPutInPlace(string locale, string inPreparation, string testAccounts)
    {
        var document = await Provider().GetCurrentAsync(LegalTopic.Privacy, locale);

        document!.BodyHtml.Should().Contain(inPreparation).And.Contain(testAccounts);
    }
}
