using System.Net;
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

/// <summary>F-11 `/account/security` (AC16, and the page side of AC13).</summary>
public sealed class SecurityPageTests : IdentityPageTestContext
{
    private const string WebSessionId = "web-1";

    public SecurityPageTests()
    {
        var store = new InMemoryWebSessionStore();
        store.SaveAsync(WebSessionId, new WebSession("jti-1", "access-1", "refresh-1", DateTimeOffset.UtcNow.AddMinutes(10), [], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30)));
        Services.AddSingleton<FakeAuthApi>();
        Services.AddSingleton(provider => new WebSessionTokenAccessor(
            store,
            new AuthClient(provider.GetRequiredService<FakeAuthApi>().Client(), Options.Create(new OpenIddictClientOptions())),
            new SessionRefreshGate(),
            TimeProvider.System));
        Api.TotpStatus = new TotpStatusResponse(false);
    }

    private IRenderedComponent<Security> RenderSignedIn()
    {
        Authorization.SetAuthorized("ana@exemplo.com").SetClaims(new Claim(WebAuthClaims.WebSessionId, WebSessionId));
        return Render<Security>();
    }

    private static IRenderedComponent<Security> Enrolling(IRenderedComponent<Security> page)
    {
        page.Find("button.app-security-turn-on").Click();
        page.WaitForAssertion(() => page.Find("img.app-security-qr"));
        return page;
    }

    [Fact]
    public void Anonymous_IsSentToSignIn()
    {
        Render<Security>();

        Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/sign-in");
    }

    // AC13: the page does not exist while the feature is off.
    [Fact]
    public void FeatureOff_ShowsNothingOfTheFeature()
    {
        Api.TotpStatus = null;

        var page = RenderSignedIn();

        page.FindAll(".app-security-card").Should().BeEmpty();
        page.Markup.Should().NotContain("Two-factor sign-in");
    }

    [Fact]
    public void Off_ExplainsAndOffersToTurnItOn()
    {
        var page = RenderSignedIn();

        page.Markup.Should().Contain("Someone who learns your password still cannot sign in without your phone.");
        page.Find("button.app-security-turn-on").TextContent.Should().Contain("Turn on two-factor sign-in");
    }

    // AC16: the QR code and the manual secret.
    [Fact]
    public void TurnOn_ShowsTheQrCodeAndTheSecretInGroupsOfFour()
    {
        var page = Enrolling(RenderSignedIn());

        page.Find("img.app-security-qr").GetAttribute("src").Should().Be(Api.TotpEnrolment.QrCodeDataUri);
        page.Find("img.app-security-qr").GetAttribute("alt").Should().NotBeNullOrWhiteSpace();
        page.Find("code.app-security-secret").TextContent.Should().Be("JBSW Y3DP EHPK 3PXP JBSW Y3DP EHPK 3PXP");
        page.Find("#security-enrol-code").GetAttribute("autocomplete").Should().Be("one-time-code");
    }

    [Fact]
    public void Confirm_WithoutACode_ShowsItOnTheFieldAndCallsNothing()
    {
        var page = Enrolling(RenderSignedIn());

        page.Find("button.app-security-confirm").Click();

        page.Markup.Should().Contain("Enter the code from the app.");
        Api.CountOf("/totp/enrolments/confirmations").Should().Be(0);
    }

    [Fact]
    public void Confirm_WrongCode_ShowsItOnTheField()
    {
        Api.TotpFailure = (HttpStatusCode.UnprocessableEntity, IdentityErrorCodes.TotpCodeInvalid);
        var page = Enrolling(RenderSignedIn());
        page.Find("#security-enrol-code").Change("000000");

        page.Find("button.app-security-confirm").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("The code is not correct or was already used."));
        page.Find("#security-enrol-code").GetAttribute("aria-invalid").Should().Be("true");
    }

    // AC16: the recovery codes, Copy, Download, and Done only after the checkbox.
    [Fact]
    public void Confirm_RightCode_ShowsTheTenCodesAndDoneWaitsForTheCheckbox()
    {
        var page = Enrolling(RenderSignedIn());
        page.Find("#security-enrol-code").Change("123456");

        page.Find("button.app-security-confirm").Click();

        page.WaitForAssertion(() => page.FindAll(".app-security-codes li").Should().HaveCount(10));
        page.Markup.Should().Contain("AAAA0-BBBB0").And.Contain("Save these codes now. You will not see them again.");
        page.Find("a.app-security-download").GetAttribute("download").Should().Be("simulab-recovery-codes.txt");
        page.Find("a.app-security-download").GetAttribute("href").Should().StartWith("data:text/plain").And.Contain("AAAA9-BBBB9");
        page.Find("button.app-security-done").HasAttribute("disabled").Should().BeTrue();
        Api.TotpBodies.Single().Should().Contain("123456");

        page.Find("#security-codes-saved").Change(true);

        page.Find("button.app-security-done").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Copy_WritesTheCodesToTheClipboard()
    {
        var page = Enrolling(RenderSignedIn());
        page.Find("#security-enrol-code").Change("123456");
        page.Find("button.app-security-confirm").Click();
        page.WaitForAssertion(() => page.Find("button.app-security-copy"));

        page.Find("button.app-security-copy").Click();

        JSInterop.VerifyInvoke("simulabShell.copyText").Arguments[0].Should().Be(string.Join("\n", Api.RecoveryCodes));
    }

    [Fact]
    public void Done_ReloadsAndShowsItOn()
    {
        var page = Enrolling(RenderSignedIn());
        page.Find("#security-enrol-code").Change("123456");
        page.Find("button.app-security-confirm").Click();
        page.WaitForAssertion(() => page.Find("#security-codes-saved"));
        page.Find("#security-codes-saved").Change(true);
        Api.TotpStatus = new TotpStatusResponse(true, new DateTimeOffset(2026, 9, 21, 10, 0, 0, TimeSpan.Zero), 10);

        page.Find("button.app-security-done").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("You have 10 recovery codes left."));
        page.FindAll(".app-security-codes li").Should().BeEmpty("the codes are shown once");
    }

    [Fact]
    public void On_ShowsTheDateTheCodesLeftAndTheDangerZone()
    {
        Api.TotpStatus = new TotpStatusResponse(true, new DateTimeOffset(2026, 9, 21, 10, 0, 0, TimeSpan.Zero), 1);

        var page = RenderSignedIn();

        page.Markup.Should().Contain("Two-factor sign-in is on since").And.Contain("You have 1 recovery code left.");
        page.Find(".app-danger-zone button.app-security-disable").ClassList.Should().Contain("mud-button-outlined-error");

        // Measured on screen: primary text on the dark card is 3.5:1, below AA; the default colour is 13:1.
        page.Find("button.app-security-regenerate").ClassList.Should().NotContain("mud-button-outlined-primary");
    }

    [Fact]
    public void Regenerate_ValidCode_ShowsTheNewCodes()
    {
        Api.TotpStatus = new TotpStatusResponse(true, null, 3);
        var page = RenderSignedIn();
        page.Find("button.app-security-regenerate").Click();
        page.Find("#security-regenerate-code").Change("ABCDE-FGHJK");

        page.Find("button.app-security-regenerate-confirm").Click();

        page.WaitForAssertion(() => page.FindAll(".app-security-codes li").Should().HaveCount(10));
        Api.TotpBodies.Single().Should().Contain("ABCDE-FGHJK");
    }

    [Fact]
    public void Disable_PasswordAndCode_SendsBothAndReloads()
    {
        Api.TotpStatus = new TotpStatusResponse(true, null, 10);
        var page = RenderSignedIn();
        page.Find("button.app-security-disable").Click();
        page.Find("#security-disable-password").Change("Estudar#2026!");
        page.Find("#security-disable-code").Change("123456");
        Api.TotpStatus = new TotpStatusResponse(false);

        page.Find("button.app-security-disable-confirm").Click();

        page.WaitForAssertion(() => page.Find("button.app-security-turn-on"));
        Api.TotpBodies.Single().Should().Contain("Estudar#2026!").And.Contain("123456");
    }

    [Fact]
    public void Disable_WrongPassword_ShowsItOnThePasswordField()
    {
        Api.TotpStatus = new TotpStatusResponse(true, null, 10);
        Api.TotpFailure = (HttpStatusCode.UnprocessableEntity, IdentityErrorCodes.TotpCurrentPasswordInvalid);
        var page = RenderSignedIn();
        page.Find("button.app-security-disable").Click();
        page.Find("#security-disable-password").Change("wrong");
        page.Find("#security-disable-code").Change("123456");

        page.Find("button.app-security-disable-confirm").Click();

        page.WaitForAssertion(() => page.Find("#security-disable-password").GetAttribute("aria-invalid").Should().Be("true"));
        page.Markup.Should().Contain("The current password is not correct.");
    }

    // F-20 AC16: turning two-factor off asks for the password too.
    [Fact]
    public void Disable_AccountWithoutPassword_ShowsHowToCreateOne()
    {
        Api.TotpStatus = new TotpStatusResponse(true, null, 10);
        Api.TotpFailure = (HttpStatusCode.UnprocessableEntity, IdentityErrorCodes.PasswordNotSet);
        var page = RenderSignedIn();
        page.Find("button.app-security-disable").Click();
        page.Find("#security-disable-password").Change("anything");
        page.Find("#security-disable-code").Change("123456");

        page.Find("button.app-security-disable-confirm").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("Your account was created with Google and has no password yet."));
        page.FindAll("button").Single(button => button.TextContent.Trim() == "Create a password").Click();
        Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/forgot-password");
    }

    [Fact]
    public void Locked_ShowsTheTimeLeftAndDisablesTheAction()
    {
        Api.TotpStatus = new TotpStatusResponse(true, null, 10);
        Api.TotpLockedSeconds = 600;
        var page = RenderSignedIn();
        page.Find("button.app-security-regenerate").Click();
        page.Find("#security-regenerate-code").Change("000000");

        page.Find("button.app-security-regenerate-confirm").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("Too many attempts. Try again in 10:00."));
        page.Find("button.app-security-regenerate-confirm").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Load_ApiFails_ShowsTheErrorStateWithTryAgain()
    {
        Api.TotpStatusFails = true;

        var page = RenderSignedIn();

        page.Markup.Should().Contain("We could not load your security settings.");
        Api.TotpStatusFails = false;
        page.Find("button.app-retry").Click();
        page.WaitForAssertion(() => page.Find("button.app-security-turn-on"));
    }
}
