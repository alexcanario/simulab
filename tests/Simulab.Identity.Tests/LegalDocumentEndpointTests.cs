using System.Net;
using System.Net.Http.Json;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>AC11 and AC12 over HTTP, against the documents that ship with the app.</summary>
public sealed class LegalDocumentEndpointTests : IdentityApiTests
{
    [Theory]
    [InlineData("terms", "en", "Terms of use")]
    [InlineData("terms", "pt-BR", "Termos de Uso")]
    [InlineData("terms", "pt-PT", "Termos de Utilização")]
    [InlineData("privacy", "en", "Privacy policy")]
    [InlineData("privacy", "pt-BR", "Política de Privacidade")]
    [InlineData("privacy", "pt-PT", "Política de Privacidade")]
    public async Task GetLegalDocument_ReturnsTheCurrentVersionInTheRequestLanguage(string topic, string locale, string title)
    {
        var response = await Client(locale).GetAsync($"/api/v1/identity/legal-documents/{topic}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await response.Content.ReadFromJsonAsync<LegalDocumentResponse>(AppJson.Options);

        document!.Title.Should().Be(title);
        document.Locale.Should().Be(locale);
        document.Version.Should().Be(SignUpForm.CurrentVersion);
        document.EffectiveDate.Should().Be(new DateOnly(2026, 9, 17));
        document.BodyHtml.Should().Contain("<h1>");
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
