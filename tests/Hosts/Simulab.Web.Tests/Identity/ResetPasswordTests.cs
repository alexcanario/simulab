using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Identity;

namespace Simulab.Web.Tests.Identity;

/// <summary>F-7 `/reset-password`: the link is checked on load, then each answer has its own state.</summary>
public class ResetPasswordTests : IdentityPageTestContext
{
    private IRenderedComponent<ResetPassword> RenderWithToken(string? token = "a-token")
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(token is null ? "/reset-password" : $"/reset-password?token={Uri.EscapeDataString(token)}");
        return Render<ResetPassword>();
    }

    private static void Fill(IRenderedComponent<ResetPassword> page, string password = "Revisar#2026!x", string confirm = "Revisar#2026!x")
    {
        page.Find("#reset-password-new").Change(password);
        page.Find("#reset-password-confirm").Change(confirm);
    }

    [Fact]
    public void ValidLink_ShowsTheFormAndNeverTheToken()
    {
        var page = RenderWithToken("secret-token-value");

        page.Find("#reset-password-new").Should().NotBeNull();
        page.Markup.Should().NotContain("secret-token-value");
        Api.CountOf("/password-reset-token-checks").Should().Be(1);
    }

    [Fact]
    public void ExpiredLink_OffersANewLinkBeforeAnyPasswordIsTyped()
    {
        Api.CheckFailure = (HttpStatusCode.Gone, IdentityErrorCodes.PasswordResetExpired);

        var page = RenderWithToken();

        page.Markup.Should().Contain("This link expired");
        page.FindAll("#reset-password-new").Should().BeEmpty();
        page.Find("#reset-password-email").Change("ana@exemplo.com");
        page.Find("button.app-reset-password-new-link").Click();
        page.Markup.Should().Contain("a link to reset the password is on its way");
        Api.CountOf("/password-reset-requests").Should().Be(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("used-token")]
    public void InvalidOrMissingLink_OffersToAskForANewOne(string? token)
    {
        Api.CheckFailure = (HttpStatusCode.BadRequest, IdentityErrorCodes.PasswordResetInvalid);

        var page = RenderWithToken(token);

        page.Markup.Should().Contain("This link is not valid");
        page.Find("a[href='/forgot-password']").TextContent.Should().Be("Ask for a new link");
    }

    [Fact]
    public void Submit_Mismatch_IsRefusedBeforeReachingTheApi()
    {
        var page = RenderWithToken();
        Fill(page, confirm: "Outra#Senha2026");

        page.Find("button.app-reset-password-submit").Click();

        page.Markup.Should().Contain("The two passwords are not the same.");
        Api.CountOf("/password-resets").Should().Be(0);
    }

    [Theory]
    [InlineData(IdentityErrorCodes.PasswordResetTooWeak, "This password does not follow the rules above.")]
    [InlineData(IdentityErrorCodes.PasswordResetSameAsCurrent, "Choose a password different from the current one.")]
    public void Submit_RefusedPassword_ShowsItOnTheField(string code, string message)
    {
        Api.ResetFailure = (HttpStatusCode.BadRequest, code);
        var page = RenderWithToken();
        Fill(page);

        page.Find("button.app-reset-password-submit").Click();

        page.Markup.Should().Contain(message);
        page.Find("#reset-password-new").GetAttribute("aria-invalid").Should().Be("true");
    }

    [Fact]
    public void Submit_Success_GoesToSignInWithTheAlertAndNoPersonalData()
    {
        var page = RenderWithToken();
        Fill(page);

        page.Find("button.app-reset-password-submit").Click();

        Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith(ResetPassword.SignInAfterResetPath);
    }
}
