using System.Net;
using System.Net.Http.Json;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>AC11 and AC12 over HTTP, against the documents that ship with the app.</summary>
public sealed class LegalDocumentEndpointTests : IdentityApiTests
{
    [Theory]
    [InlineData("terms", "en", "Terms of use", "2026-v1", "2026-09-17")]
    [InlineData("terms", "pt-BR", "Termos de Uso", "2026-v1", "2026-09-17")]
    [InlineData("terms", "pt-PT", "Termos de Utilização", "2026-v1", "2026-09-17")]
    [InlineData("privacy", "en", "Privacy policy", "2026-v3", "2026-10-09")]
    [InlineData("privacy", "pt-BR", "Política de Privacidade", "2026-v3", "2026-10-09")]
    [InlineData("privacy", "pt-PT", "Política de Privacidade", "2026-v3", "2026-10-09")]
    public async Task GetLegalDocument_ReturnsTheCurrentVersionInTheRequestLanguage(
        string topic, string locale, string title, string version, string effectiveDate)
    {
        var response = await Client(locale).GetAsync($"/api/v1/identity/legal-documents/{topic}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await response.Content.ReadFromJsonAsync<LegalDocumentResponse>(AppJson.Options);

        document!.Title.Should().Be(title);
        document.Locale.Should().Be(locale);
        document.Version.Should().Be(version);
        document.EffectiveDate.Should().Be(DateOnly.Parse(effectiveDate, System.Globalization.CultureInfo.InvariantCulture));
        // The page shows the title from the manifest, so the body starts at its first section.
        document.BodyHtml.Should().Contain("<h2>").And.NotContain("<h1>");
        // AC12: the texts that ship are drafts until the owner replaces them.
        document.IsPlaceholder.Should().BeTrue();
    }

    [Fact]
    public async Task GetLegalDocument_UnknownTopic_IsNotFound()
    {
        var response = await Client().GetAsync("/api/v1/identity/legal-documents/cookies");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.LegalDocumentNotFound);
    }
}
