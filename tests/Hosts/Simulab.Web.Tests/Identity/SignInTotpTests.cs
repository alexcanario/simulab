using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Identity;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;

namespace Simulab.Web.Tests.Identity;

/// <summary>F-11 UC3, UC4 through the real sign-in page: the code step after the password.</summary>
public sealed class SignInTotpTests : IdentityPageTestContext
{
    private FakeAuthApi Auth => Services.GetRequiredService<FakeAuthApi>();

    public SignInTotpTests()
    {
        Services.AddSingleton(_ => new FakeAuthApi { RequireTotp = true });
        Services.AddSingleton(provider => new AuthClient(provider.GetRequiredService<FakeAuthApi>().Client(), Options.Create(new OpenIddictClientOptions())));
        Services.AddSingleton<SignInTicketStore>();
    }

    private IRenderedComponent<SignIn> PasswordStep()
    {
        var page = Render<SignIn>();
        page.Find("#sign-in-email").Change("ana@example.com");
        page.Find("#sign-in-password").Change("Estudar#2026!");
        page.Find("button.app-sign-in-submit").Click();
        page.WaitForAssertion(() => page.Find("#sign-in-code"));
        return page;
    }

    // AC4 on screen: the password step leads to the code step, not to a session.
    [Fact]
    public void RightPassword_TwoFactorOn_ShowsTheCodeStep()
    {
        var page = PasswordStep();

        page.Markup.Should().Contain("Enter the code from your authenticator app.");
        page.FindAll("#sign-in-password").Should().BeEmpty();
        page.Find("#sign-in-code").GetAttribute("autocomplete").Should().Be("one-time-code");
        Services.GetRequiredService<NavigationManager>().Uri.Should().NotContain("challenge");
    }

    [Fact]
    public void Code_Right_SendsTheChallengeAndFinishesTheSignIn()
    {
        var page = PasswordStep();
        page.Find("#sign-in-code").Change("123456");

        page.Find("button.app-sign-in-code-submit").Click();

        page.WaitForAssertion(() => Services.GetRequiredService<NavigationManager>().Uri.Should().Contain("/account/sign-in-complete?ticket="));
        Auth.TotpForms.Single().Should().Contain("challenge=challenge-1").And.Contain("code=123456");
    }

    [Fact]
    public void Code_Empty_ShowsItOnTheFieldAndCallsNothing()
    {
        var page = PasswordStep();

        page.Find("button.app-sign-in-code-submit").Click();

        page.Markup.Should().Contain("Enter the code.");
        Auth.TotpForms.Should().BeEmpty();
    }

    // BR9 on screen: a wrong code spends the challenge, so the next try starts from the password.
    [Fact]
    public void Code_Wrong_GoesBackToThePasswordWithTheReason()
    {
        Auth.TotpError = IdentityErrorCodes.TotpCodeInvalid;
        var page = PasswordStep();
        page.Find("#sign-in-code").Change("000000");

        page.Find("button.app-sign-in-code-submit").Click();

        page.WaitForAssertion(() => page.Find("#sign-in-password"));
        page.Markup.Should().Contain("The code was not accepted. Enter your password again to try once more.");
    }

    [Fact]
    public void Code_ChallengeExpired_GoesBackToThePassword()
    {
        Auth.TotpError = IdentityErrorCodes.TotpChallengeInvalid;
        var page = PasswordStep();
        page.Find("#sign-in-code").Change("123456");

        page.Find("button.app-sign-in-code-submit").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("The sign-in took too long. Enter your password again."));
    }

    [Fact]
    public void Code_Locked_ShowsTheTimeLeft()
    {
        Auth.TotpError = IdentityErrorCodes.AccountLocked;
        Auth.TotpErrorDescription = "600";
        var page = PasswordStep();
        page.Find("#sign-in-code").Change("123456");

        page.Find("button.app-sign-in-code-submit").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("Too many attempts. Try again in 10:00."));
    }

    // UC4.
    [Fact]
    public void UseARecoveryCode_SwapsTheFieldAndSendsTheRecoveryCode()
    {
        var page = PasswordStep();

        page.Find("button.app-sign-in-code-mode").Click();

        page.Markup.Should().Contain("Recovery code").And.Contain("Each code works only once.");
        page.Find("#sign-in-code").Change("ABCDE-FGHJK");
        page.Find("button.app-sign-in-code-submit").Click();
        page.WaitForAssertion(() => Auth.TotpForms.Single().Should().Contain("code=ABCDE-FGHJK"));
    }

    [Fact]
    public void Cancel_ReturnsToThePasswordForm()
    {
        var page = PasswordStep();

        page.Find("button.app-sign-in-code-cancel").Click();

        page.Find("#sign-in-password").Should().NotBeNull();
        page.FindAll("#sign-in-code").Should().BeEmpty();
    }
}
