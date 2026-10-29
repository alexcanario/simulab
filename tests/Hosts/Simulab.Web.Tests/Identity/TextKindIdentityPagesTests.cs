using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Identity;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;

namespace Simulab.Web.Tests.Identity;

/// <summary>F-183 AC2, AC7, AC8, AC10 through the real sign-in page: the e-mail and the code step.</summary>
public sealed class TextKindSignInTests : IdentityPageTestContext
{
    private FakeAuthApi Auth => Services.GetRequiredService<FakeAuthApi>();

    public TextKindSignInTests()
    {
        Services.AddSingleton(_ => new FakeAuthApi { RequireTotp = true });
        Services.AddSingleton(provider => new AuthClient(provider.GetRequiredService<FakeAuthApi>().Client(), Options.Create(new OpenIddictClientOptions())));
        Services.AddSingleton<SignInTicketStore>();
    }

    private IRenderedComponent<SignIn> PasswordStep(string email = "ana@example.com")
    {
        var page = Render<SignIn>();
        page.Find("#sign-in-email").Change(email);
        page.Find("#sign-in-password").Change("Estudar#2026!");
        page.Find("button.app-sign-in-submit").Click();
        page.WaitForAssertion(() => page.Find("#sign-in-code"));
        return page;
    }

    [Fact]
    public void Email_IsTheEmailField()
    {
        var page = Render<SignIn>();

        page.FindAll(".app-email-field #sign-in-email").Should().ContainSingle();
    }

    // AC10: the page only trims and lower-cases; the account is found as before (Identity normalizes the e-mail).
    [Fact]
    public void Email_MixedCaseWithSpaces_IsKeptTrimmedAndLowerCase()
    {
        var page = Render<SignIn>();

        page.Find("#sign-in-email").Change("ANA@Example.com ");

        page.Find("#sign-in-email").GetAttribute("value").Should().Be("ana@example.com");
    }

    [Fact]
    public void CodeStep_UsesTheOneTimeCodeField()
    {
        var page = PasswordStep();

        page.FindAll(".app-otp-field #sign-in-code").Should().ContainSingle();
    }

    [Fact]
    public void RecoveryCodeMode_UsesTheCodeField()
    {
        var page = PasswordStep();

        page.Find("button.app-sign-in-code-mode").Click();

        page.FindAll(".app-code-field #sign-in-code").Should().ContainSingle();
        page.FindAll(".app-otp-field").Should().BeEmpty();
    }

    // AC2: the sixth digit sends nothing; only the button does.
    [Fact]
    public void Code_Typed_IsNotSentUntilTheButton()
    {
        var page = PasswordStep();

        page.Find(".app-otp-field #sign-in-code").Change("123456");

        Auth.TotpForms.Should().BeEmpty();
        page.Find("button.app-sign-in-code-submit").Click();
        page.WaitForAssertion(() => Auth.TotpForms.Should().ContainSingle());
    }

    [Fact]
    public void Code_PastedWithASpace_IsSentWhole()
    {
        var page = PasswordStep();
        page.Find("#sign-in-code").Change("123 456");

        page.Find("button.app-sign-in-code-submit").Click();

        page.WaitForAssertion(() => Auth.TotpForms.Single().Should().Contain("code=123456"));
    }

    // AC8 [sec]: the code goes only in the body of the existing call: never in the address, never to browser storage.
    [Fact]
    public void Code_StaysOutOfTheAddressAndTheBrowserStorage()
    {
        var page = PasswordStep();
        page.Find(".app-otp-field #sign-in-code").Change("123456");

        page.Find("button.app-sign-in-code-submit").Click();

        page.WaitForAssertion(() => Auth.TotpForms.Should().ContainSingle());
        Services.GetRequiredService<NavigationManager>().Uri.Should().NotContain("123456");
        JSInterop.Invocations
            .Where(call => call.Arguments.Any(argument => argument?.ToString()?.Contains("123456", StringComparison.Ordinal) == true))
            .Should().BeEmpty();
    }

    // AC8 [sec]: a recovery code in the code field follows the same path.
    [Fact]
    public void RecoveryCode_StaysOutOfTheAddressAndTheBrowserStorage()
    {
        var page = PasswordStep();
        page.Find("button.app-sign-in-code-mode").Click();
        page.Find(".app-code-field #sign-in-code").Change("ABCDE-FGHJK");

        page.Find("button.app-sign-in-code-submit").Click();

        page.WaitForAssertion(() => Auth.TotpForms.Should().ContainSingle());
        Services.GetRequiredService<NavigationManager>().Uri.Should().NotContain("ABCDE");
        JSInterop.Invocations
            .Where(call => call.Arguments.Any(argument => argument?.ToString()?.Contains("ABCDE", StringComparison.Ordinal) == true))
            .Should().BeEmpty();
    }
}

/// <summary>F-183 AC7 on the security page: the enrolment takes a TOTP code alone, the other two a code or a recovery code.</summary>
public sealed class TextKindSecurityPageTests : IdentityPageTestContext
{
    private const string WebSessionId = "web-1";

    public TextKindSecurityPageTests()
    {
        var store = new InMemoryWebSessionStore();
        store.SaveAsync(WebSessionId, new WebSession("jti-1", "access-1", "refresh-1", DateTimeOffset.UtcNow.AddMinutes(10), [], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30)));
        Services.AddSingleton<FakeAuthApi>();
        Services.AddSingleton(provider => new WebSessionTokenAccessor(
            store,
            new AuthClient(provider.GetRequiredService<FakeAuthApi>().Client(), Options.Create(new OpenIddictClientOptions())),
            new SessionRefreshGate(),
            TimeProvider.System));
    }

    private IRenderedComponent<Security> RenderSignedIn()
    {
        Authorization.SetAuthorized("ana@exemplo.com").SetClaims(new Claim(WebAuthClaims.WebSessionId, WebSessionId));
        return Render<Security>();
    }

    [Fact]
    public void Enrol_UsesTheOneTimeCodeField()
    {
        Api.TotpStatus = new TotpStatusResponse(false);
        var page = RenderSignedIn();

        page.Find("button.app-security-turn-on").Click();

        page.WaitForAssertion(() => page.FindAll(".app-otp-field #security-enrol-code").Should().ContainSingle());
    }

    [Fact]
    public void Regenerate_UsesTheCodeField()
    {
        Api.TotpStatus = new TotpStatusResponse(true, null, 3);
        var page = RenderSignedIn();

        page.Find("button.app-security-regenerate").Click();

        page.FindAll(".app-code-field #security-regenerate-code").Should().ContainSingle();
    }

    [Fact]
    public void Disable_UsesTheCodeField()
    {
        Api.TotpStatus = new TotpStatusResponse(true, null, 10);
        var page = RenderSignedIn();

        page.Find("button.app-security-disable").Click();

        page.FindAll(".app-code-field #security-disable-code").Should().ContainSingle();
    }
}
