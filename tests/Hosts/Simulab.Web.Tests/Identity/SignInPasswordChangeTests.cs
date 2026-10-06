using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Identity;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;

namespace Simulab.Web.Tests.Identity;

/// <summary>F-53 UC2 through the real sign-in page: the "Choose a new password" step after the password, or after the code.</summary>
public sealed class SignInPasswordChangeTests : IdentityPageTestContext
{
    private const string NewPassword = "Revisar#2026!x";

    private FakeAuthApi Auth => Services.GetRequiredService<FakeAuthApi>();

    public SignInPasswordChangeTests()
    {
        Services.AddSingleton(_ => new FakeAuthApi { RequirePasswordChange = true });
        Services.AddSingleton(provider => new AuthClient(provider.GetRequiredService<FakeAuthApi>().Client(), Options.Create(new OpenIddictClientOptions())));
        Services.AddSingleton<SignInTicketStore>();
    }

    private IRenderedComponent<SignIn> PasswordStep()
    {
        var page = Render<SignIn>();
        page.Find("#sign-in-email").Change("admin@simulab.local");
        page.Find("#sign-in-password").Change("Estudar#2026!");
        page.Find("button.app-sign-in-submit").Click();
        page.WaitForAssertion(() => page.Find("#sign-in-new-password"));
        return page;
    }

    private static void Fill(IRenderedComponent<SignIn> page, string password, string confirm)
    {
        page.Find("#sign-in-new-password").Change(password);
        page.Find("#sign-in-confirm-password").Change(confirm);
    }

    // AC20: the right password of a marked account leads to the new-password step, not to a session.
    [Fact]
    public void RightPassword_MarkedAccount_ShowsTheNewPasswordStep()
    {
        var page = PasswordStep();

        page.Find("h1").TextContent.Should().Be("Choose a new password");
        page.Find("#sign-in-new-password").GetAttribute("autocomplete").Should().Be("new-password");
        page.Find("#sign-in-confirm-password").GetAttribute("autocomplete").Should().Be("new-password");
        page.Markup.Should().Contain("At least 12 characters");
        page.FindAll("#sign-in-password").Should().BeEmpty();
        Services.GetRequiredService<NavigationManager>().Uri.Should().NotContain("challenge");
    }

    // AC20: a valid new password lands the user signed in, carrying the challenge the sign-in handed out.
    [Fact]
    public void NewPassword_Valid_SendsTheChallengeAndFinishesTheSignIn()
    {
        var page = PasswordStep();
        Fill(page, NewPassword, NewPassword);

        page.Find("button.app-sign-in-change-submit").Click();

        page.WaitForAssertion(() => Services.GetRequiredService<NavigationManager>().Uri.Should().Contain("/account/sign-in-complete?ticket="));
        Auth.PasswordChangeForms.Should().ContainSingle().Which.Should().Contain("challenge=change-1").And.Contain("new_password=");
    }

    // AC20: after two-factor, the code step leads to the same step.
    [Fact]
    public void RightCode_MarkedAccount_ShowsTheNewPasswordStep()
    {
        Auth.RequireTotp = true;
        Auth.TotpNeedsPasswordChange = true;
        var page = Render<SignIn>();
        page.Find("#sign-in-email").Change("admin@simulab.local");
        page.Find("#sign-in-password").Change("Estudar#2026!");
        page.Find("button.app-sign-in-submit").Click();
        page.WaitForAssertion(() => page.Find("#sign-in-code"));
        page.Find("#sign-in-code").Change("123456");

        page.Find("button.app-sign-in-code-submit").Click();

        page.WaitForAssertion(() => page.Find("#sign-in-new-password"));
        page.FindAll("#sign-in-code").Should().BeEmpty();
        page.Find("h1").TextContent.Should().Be("Choose a new password");
    }

    // AC20: a weak or unchanged password shows its message in the step, which stays.
    [Theory]
    [InlineData(IdentityErrorCodes.PasswordChangeTooWeak, "This password does not follow the rules above.")]
    [InlineData(IdentityErrorCodes.PasswordChangeSameAsCurrent, "Choose a password different from the current one.")]
    public void NewPassword_Refused_ShowsItsMessageAndStaysInTheStep(string code, string message)
    {
        Auth.PasswordChangeError = code;
        var page = PasswordStep();
        Fill(page, NewPassword, NewPassword);

        page.Find("button.app-sign-in-change-submit").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain(message));
        page.Find("#sign-in-new-password").Should().NotBeNull();
        page.FindAll("#sign-in-password").Should().BeEmpty();
    }

    [Fact]
    public void NewPassword_Mismatch_ShowsItOnTheFieldAndCallsNothing()
    {
        var page = PasswordStep();
        Fill(page, NewPassword, NewPassword + "x");

        page.Find("button.app-sign-in-change-submit").Click();

        page.Markup.Should().Contain("The two passwords are not the same.");
        Auth.PasswordChangeForms.Should().BeEmpty();
    }

    [Fact]
    public void NewPassword_Empty_ShowsTheRequiredMessagesAndCallsNothing()
    {
        var page = PasswordStep();

        page.Find("button.app-sign-in-change-submit").Click();

        page.Markup.Should().Contain("Enter a password.").And.Contain("Repeat the password.");
        Auth.PasswordChangeForms.Should().BeEmpty();
    }

    // AC20: an expired challenge brings back the password step with its message.
    [Fact]
    public void NewPassword_ChallengeExpired_GoesBackToThePasswordWithTheReason()
    {
        Auth.PasswordChangeError = IdentityErrorCodes.PasswordChangeChallengeInvalid;
        var page = PasswordStep();
        Fill(page, NewPassword, NewPassword);

        page.Find("button.app-sign-in-change-submit").Click();

        page.WaitForAssertion(() => page.Find("#sign-in-password"));
        page.Markup.Should().Contain("The sign-in took too long. Enter your password again.");
        page.FindAll("#sign-in-new-password").Should().BeEmpty();
    }

    // An error the step does not know stays in the step, in the step's own error area.
    [Fact]
    public void NewPassword_UnexpectedError_ShowsItInTheStepsOwnAlert()
    {
        Auth.PasswordChangeError = "server_error";
        var page = PasswordStep();
        Fill(page, NewPassword, NewPassword);

        page.Find("button.app-sign-in-change-submit").Click();

        page.WaitForAssertion(() => page.Find(".mud-alert"));
        page.Find("#sign-in-new-password").Should().NotBeNull();
    }

    [Fact]
    public void Cancel_ReturnsToThePasswordForm()
    {
        var page = PasswordStep();

        page.Find("button.app-sign-in-change-cancel").Click();

        page.Find("#sign-in-password").Should().NotBeNull();
        page.FindAll("#sign-in-new-password").Should().BeEmpty();
    }
}
