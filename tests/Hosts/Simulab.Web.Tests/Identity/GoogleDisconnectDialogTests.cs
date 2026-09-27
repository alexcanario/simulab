using System.Net;
using System.Security.Claims;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MudBlazor;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Identity;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;

namespace Simulab.Web.Tests.Identity;

/// <summary>
/// F-29 AC12 to AC14 on the screen the owner validates: the disconnect dialog opened from the Security page,
/// and what each refusal of the Api looks like there. The Api side is covered by `GoogleLinkTests`.
/// </summary>
public sealed class GoogleDisconnectDialogTests : IdentityPageTestContext
{
    private const string WebSessionId = "web-1";

    public GoogleDisconnectDialogTests()
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
        Api.GoogleLink = new GoogleLinkResponse(true, "ana@gmail.com", HasPassword: true);
        Authorization.SetAuthorized("ana@exemplo.com").SetClaims(new Claim(WebAuthClaims.WebSessionId, WebSessionId));
    }

    /// <summary>Opens `/account/security`, presses Disconnect and waits for the dialog's password field.</summary>
    private (IRenderedComponent<Security> Page, IRenderedComponent<MudDialogProvider> Dialogs) OpenDialog()
    {
        Render<MudPopoverProvider>();
        var dialogs = Render<MudDialogProvider>();
        var page = Render<Security>();
        page.WaitForAssertion(() => page.Find(".app-security-google-disconnect"));
        page.Find(".app-security-google-disconnect").Click();
        dialogs.WaitForAssertion(() => dialogs.FindAll("#google-disconnect-password").Should().ContainSingle());
        return (page, dialogs);
    }

    private static void Confirm(IRenderedComponent<MudDialogProvider> dialogs, string password)
    {
        dialogs.Find("#google-disconnect-password").Change(password);
        dialogs.Find("button.app-google-disconnect-confirm").Click();
    }

    // AC12 on the screen: the right password closes the dialog and the card goes back to "connect".
    [Fact]
    public void RightPassword_ClosesTheDialogAndTheCardOffersToConnectAgain()
    {
        var (page, dialogs) = OpenDialog();
        Api.GoogleLink = new GoogleLinkResponse(false, null, HasPassword: true);

        Confirm(dialogs, "Correct#Password1");

        dialogs.WaitForAssertion(() => dialogs.FindAll("#google-disconnect-password").Should().BeEmpty());
        page.WaitForAssertion(() => page.Find(".app-security-google-connect"));
        Api.GoogleRemovalBodies.Should().ContainSingle().Which.Should().Contain("Correct#Password1");
    }

    // AC13 on the screen: a wrong password stays on the dialog, on the field, and the link is still there.
    [Fact]
    public void WrongPassword_ShowsItOnTheFieldAndKeepsTheDialogOpen()
    {
        var (page, dialogs) = OpenDialog();
        Api.GoogleRemovalFailure = (HttpStatusCode.UnprocessableEntity, IdentityErrorCodes.GoogleLinkCurrentPasswordInvalid);

        Confirm(dialogs, "Wrong#Password1");

        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain("Wrong password."));
        dialogs.FindAll("#google-disconnect-password").Should().ContainSingle("the dialog stays open to try again");
        page.Find(".app-security-google-disconnect").Should().NotBeNull("the link was not removed");
    }

    // AC13's second half on the screen: the lockout is an alert with the remaining time, not a field error.
    [Fact]
    public void Locked_ShowsTheCountdownAlert()
    {
        var (_, dialogs) = OpenDialog();
        Api.GoogleRemovalFailure = (HttpStatusCode.Locked, IdentityErrorCodes.AccountLocked);

        Confirm(dialogs, "Wrong#Password1");

        dialogs.WaitForAssertion(() => dialogs.Find("[aria-live=polite] .mud-alert").TextContent.Should().NotBeNullOrWhiteSpace());
    }

    // AC14 on the screen: no password means the link is the only way in, and the dialog says exactly that.
    [Fact]
    public void PasswordNotSet_ShowsThatAlertAndThenClearsItOnTheNextAnswer()
    {
        var (_, dialogs) = OpenDialog();
        Api.GoogleRemovalFailure = (HttpStatusCode.UnprocessableEntity, IdentityErrorCodes.PasswordNotSet);

        Confirm(dialogs, "Anything#1");

        const string NoPasswordText = "Your account was created with Google and has no password yet.";
        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain(NoPasswordText));

        // The verdict belongs to one attempt: a later different refusal must not keep showing it.
        Api.GoogleRemovalFailure = (HttpStatusCode.UnprocessableEntity, IdentityErrorCodes.GoogleLinkCurrentPasswordInvalid);
        Confirm(dialogs, "Wrong#Password1");

        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain("Wrong password."));
        dialogs.Markup.Should().NotContain(NoPasswordText, "one attempt's verdict must not outlive it");
    }

    // The dialog is a refusal that can be walked away from: cancelling changes nothing and calls nothing.
    [Fact]
    public void Cancel_ClosesTheDialogAndCallsNothing()
    {
        var (page, dialogs) = OpenDialog();

        dialogs.Find("button.app-google-disconnect-cancel").Click();

        dialogs.WaitForAssertion(() => dialogs.FindAll("#google-disconnect-password").Should().BeEmpty());
        Api.GoogleRemovalBodies.Should().BeEmpty();
        page.Find(".app-security-google-disconnect").Should().NotBeNull();
    }
}
