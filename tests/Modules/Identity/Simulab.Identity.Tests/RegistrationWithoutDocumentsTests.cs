using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Simulab.Identity.Tests;

/// <summary>
/// The second half of AC5: a server that cannot read its own legal documents is a configuration
/// problem, not the visitor's, so the version check is skipped instead of blocking sign-up (BR5).
/// </summary>
public sealed class RegistrationWithoutDocumentsTests : IdentityApiTests
{
    private readonly DirectoryInfo _emptyContent = Directory.CreateTempSubdirectory("simulab-no-legal-");

    protected override void ConfigureHost(IWebHostBuilder builder) =>
        builder.ConfigureAppConfiguration(configuration =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Legal:RootPath"] = _emptyContent.FullName,
            }));

    [Fact]
    public async Task Register_NoDocumentOnTheServer_StillSucceeds()
    {
        var request = SignUpForm.Valid("sem.documentos@exemplo.com") with { TermsVersion = "qualquer", PrivacyVersion = "outra" };

        await PostAsync(Client(), "/api/v1/identity/registrations", request, HttpStatusCode.Accepted);

        (await QueryAsync(context => Task.FromResult(context.Users.Count(u => u.Email == request.Email)))).Should().Be(1);
        await RunJobsAsync();
        Emails.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetLegalDocument_NoDocumentOnTheServer_IsNotFound()
    {
        var response = await Client().GetAsync("/api/v1/identity/legal-documents/terms");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
