using System.Net;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-29 AC17 on the Api: with <c>Identity:GoogleSignInEnabled</c> off, the whole group is unmapped, so the
/// three link routes do not exist. The host here is the default one — no <c>GoogleTokens.Configure</c>.
/// </summary>
public sealed class GoogleLinkSwitchedOffTests : IdentityApiTests
{
    [Theory]
    [InlineData("get")]
    [InlineData("link")]
    [InlineData("unlink")]
    public async Task EveryRoute_SwitchedOff_Is404(string route)
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);
        session.AccessToken.Should().NotBeNull(session.Error);

        using var response = route switch
        {
            "get" => await GoogleLinkApi.GetAsync(client, session.AccessToken),
            "link" => await GoogleLinkApi.LinkAsync(client, session.AccessToken, "any-token"),
            _ => await GoogleLinkApi.UnlinkAsync(client, session.AccessToken, SignUpForm.ValidPassword),
        };

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, await response.Content.ReadAsStringAsync());
    }
}
