using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Identity;

namespace Simulab.Web.Tests.Identity;

/// <summary>The verification screen: one state per answer the API can give.</summary>
public class VerifyEmailTests : IdentityPageTestContext
{
    /// <summary>The token reaches the page through the query string, the way the link in the email does.</summary>
    private IRenderedComponent<VerifyEmail> RenderWithToken(string? token = "a-token")
    {
        var navigation = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        navigation.NavigateTo(token is null ? "/verify-email" : $"/verify-email?token={Uri.EscapeDataString(token)}");
        return Render<VerifyEmail>();
    }

    [Fact]
    public void ValidToken_ShowsVerifiedAndNoSignInLink()
    {
        var page = RenderWithToken();

        page.Markup.Should().Contain("Email verified");
        page.Markup.Should().Contain("Your account is active");
        // F-5 adds the sign-in button; a dead link would fail validation on screen.
        page.FindAll("a[href='/sign-in']").Should().BeEmpty();
    }

    [Fact]
    public void ExpiredToken_OffersAResend()
    {
        Api.VerifyFailure = (HttpStatusCode.Gone, IdentityErrorCodes.VerificationExpired);

        var page = RenderWithToken();

        page.Markup.Should().Contain("This link expired");
        page.Find("#verify-email-address").Should().NotBeNull();

        page.Find("#verify-email-address").Change("ana@exemplo.com");
        var buttons = page.FindAll("button");
        buttons[^1].Click();

        Api.CountOf("/resend").Should().Be(1);
        page.Markup.Should().Contain("a new link is on its way");
    }

    [Fact]
    public void InvalidToken_LinksBackToSignUp()
    {
        Api.VerifyFailure = (HttpStatusCode.BadRequest, IdentityErrorCodes.VerificationInvalid);

        var page = RenderWithToken();

        page.Markup.Should().Contain("This link is not valid");
        page.Find("a[href='/sign-up']").ClassList.Should().Contain("app-link", "B-6: a kit link, AA in both themes");
    }

    [Fact]
    public void NoTokenInTheLink_IsTreatedAsInvalidAndCallsNothing()
    {
        var page = RenderWithToken(null);

        page.Markup.Should().Contain("This link is not valid");
        Api.CountOf("/email-verifications").Should().Be(0);
    }

    [Fact]
    public void ServerError_ShowsTheGenericMessageWithTryAgain()
    {
        Api.VerifyFailure = (HttpStatusCode.InternalServerError, "something.unknown");

        var page = RenderWithToken();

        page.Markup.Should().Contain("Something went wrong. Please try again.");
        page.Markup.Should().Contain("Try again");
        page.Markup.Should().NotContain("something.unknown");
    }
}
