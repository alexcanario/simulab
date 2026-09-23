using System.Net;
using System.Net.Http.Json;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>F-20 AC1 on the Api: with <c>Identity:GoogleSignInEnabled</c> off (the v1 default), nothing of the feature answers.</summary>
public sealed class GoogleSignInSwitchedOffTests : IdentityApiTests
{
    [Fact]
    public async Task Grant_SwitchedOff_IsAnUnsupportedGrantType()
    {
        var token = GoogleTokens.Issue(GoogleTokens.NewSubject(), "ana@gmail.com");

        var grant = await GoogleTokens.GrantAsync(Client(), token);

        grant.Error.Should().Be("unsupported_grant_type");
        grant.AccessToken.Should().BeNull();
    }

    [Fact]
    public async Task Registration_SwitchedOff_IsNotFound()
    {
        var token = GoogleTokens.Issue(GoogleTokens.NewSubject(), "ana@gmail.com");

        using var response = await Client().PostAsJsonAsync("/api/v1/identity/google-registrations", GoogleTokens.Registration(token), AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
