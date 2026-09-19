using System.Net;
using Bunit;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Identity;

namespace Simulab.Web.Tests.Identity;

/// <summary>F-7 `/forgot-password`: one state per answer the Api can give.</summary>
public class ForgotPasswordTests : IdentityPageTestContext
{
    [Fact]
    public void Submit_Empty_ShowsTheFieldMessageAndSendsNothing()
    {
        var page = Render<ForgotPassword>();

        page.Find("button.app-forgot-password-submit").Click();

        page.Markup.Should().Contain("Enter your email.");
        Api.CountOf("/password-reset-requests").Should().Be(0);
    }

    [Fact]
    public void Submit_NotAnEmail_ShowsTheFormatMessageAndSendsNothing()
    {
        var page = Render<ForgotPassword>();
        page.Find("#forgot-password-email").Change("ana@");

        page.Find("button.app-forgot-password-submit").Click();

        page.Markup.Should().Contain("Enter a valid email address.");
        Api.CountOf("/password-reset-requests").Should().Be(0);
    }

    [Fact]
    public void Submit_Address_ShowsTheGenericAnswerAndStartsTheCooldown()
    {
        var page = Render<ForgotPassword>();
        page.Find("#forgot-password-email").Change("ana@exemplo.com");

        page.Find("button.app-forgot-password-submit").Click();

        page.Markup.Should().Contain("If this email belongs to an account, a link to reset the password is on its way.");
        page.Markup.Should().Contain("You can ask for a new link in 60 s.");
        page.Find("button.app-forgot-password-submit").HasAttribute("disabled").Should().BeTrue();
        Api.CountOf("/password-reset-requests").Should().Be(1);
    }

    [Fact]
    public void Submit_ClientLimitReached_ShowsTheRateLimitMessage()
    {
        Api.RequestResetFailure = (HttpStatusCode.TooManyRequests, IdentityErrorCodes.PasswordResetRateLimited);
        var page = Render<ForgotPassword>();
        page.Find("#forgot-password-email").Change("ana@exemplo.com");

        page.Find("button.app-forgot-password-submit").Click();

        page.Markup.Should().Contain("Too many attempts from this device.");
        page.Markup.Should().NotContain("is on its way");
    }

    [Fact]
    public void Page_LinksBackToSignIn()
    {
        var page = Render<ForgotPassword>();

        page.Find("a[href='/sign-in']").TextContent.Should().Be("Back to sign in");
    }
}
