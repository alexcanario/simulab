using System.Net;
using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MudBlazor;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Identity;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;

namespace Simulab.Web.Tests.Identity;

/// <summary>F-10 AC12: the danger zone on `/account` and its confirmation dialog.</summary>
public sealed class AccountErasureTests : IdentityPageTestContext
{
    private const string WebSessionId = "web-1";

    public AccountErasureTests()
    {
        var store = new InMemoryWebSessionStore();
        store.SaveAsync(WebSessionId, new WebSession("jti-1", "access-1", "refresh-1", DateTimeOffset.UtcNow.AddMinutes(10), [], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30)));
        Services.AddSingleton<FakeAuthApi>();
        Services.AddSingleton(provider => new WebSessionTokenAccessor(
            store,
            new AuthClient(provider.GetRequiredService<FakeAuthApi>().Client(), Options.Create(new OpenIddictClientOptions())),
            new SessionRefreshGate(),
            TimeProvider.System));

        Authorization.SetAuthorized("ana@exemplo.com").SetClaims(new Claim(WebAuthClaims.WebSessionId, WebSessionId));
    }

    private IRenderedComponent<MudDialogProvider> Providers()
    {
        Render<MudPopoverProvider>();
        return Render<MudDialogProvider>();
    }

    /// <summary>Opens the page, clicks "Erase my account" and waits for the dialog's password field.</summary>
    private (IRenderedComponent<Account> Page, IRenderedComponent<MudDialogProvider> Dialogs) OpenDialog()
    {
        var dialogs = Providers();
        var page = Render<Account>();
        page.Find("button.app-erase-open").Click();
        dialogs.WaitForAssertion(() => dialogs.FindAll("#erase-account-password").Should().ContainSingle());
        return (page, dialogs);
    }

    private static void Type(IRenderedComponent<MudDialogProvider> dialogs, string password) =>
        dialogs.Find("#erase-account-password").Change(password);

    private static void Confirm(IRenderedComponent<MudDialogProvider> dialogs) =>
        dialogs.Find("button.app-erase-confirm").Click();

    [Fact]
    public void Page_ShowsTheDangerZoneWithWhatErasureDoes()
    {
        var page = Render<Account>();

        page.Markup.Should().Contain("Erase my account");
        page.Markup.Should().Contain("It cannot be undone.");
        page.Find("button.app-erase-open").Should().NotBeNull();
    }

    [Fact]
    public void Dialog_WithoutAPassword_CannotConfirm()
    {
        var (_, dialogs) = OpenDialog();

        dialogs.Find("button.app-erase-confirm").HasAttribute("disabled").Should().BeTrue();

        Type(dialogs, "Estudar#2026!");

        dialogs.WaitForAssertion(() => dialogs.Find("button.app-erase-confirm").HasAttribute("disabled").Should().BeFalse());
    }

    [Fact]
    public void Dialog_WrongPassword_StaysOpenWithTheTranslatedError()
    {
        Api.EraseFailure = (HttpStatusCode.UnprocessableEntity, IdentityErrorCodes.AccountErasureCurrentPasswordInvalid);
        var (_, dialogs) = OpenDialog();

        Type(dialogs, "not-the-password");
        Confirm(dialogs);

        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain("The current password is not correct."));
        dialogs.FindAll("#erase-account-password").Should().ContainSingle("the dialog stays open on a failure");
        Services.GetRequiredService<NavigationManager>().Uri.Should().NotContain("sign-out");
    }

    [Fact]
    public void Dialog_LastManager_ShowsWhyItWasRefused()
    {
        Api.EraseFailure = (HttpStatusCode.UnprocessableEntity, IdentityErrorCodes.AccountErasureLastManager);
        var (_, dialogs) = OpenDialog();

        Type(dialogs, "Estudar#2026!");
        Confirm(dialogs);

        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain("last user able to manage roles"));
        dialogs.FindAll("#erase-account-password").Should().ContainSingle();
    }

    // F-20 AC16.
    [Fact]
    public void Dialog_AccountWithoutPassword_ShowsHowToCreateOne()
    {
        Api.EraseFailure = (HttpStatusCode.UnprocessableEntity, IdentityErrorCodes.PasswordNotSet);
        var (_, dialogs) = OpenDialog();

        Type(dialogs, "anything");
        Confirm(dialogs);

        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain("Your account was created with Google and has no password yet."));
        dialogs.FindAll("button").Single(button => button.TextContent.Trim() == "Create a password").Click();
        dialogs.WaitForAssertion(() => Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/forgot-password"));
    }

    [Fact]
    public void Dialog_LockedOut_ShowsTheWait()
    {
        Api.EraseLockedSeconds = 90;
        var (_, dialogs) = OpenDialog();

        Type(dialogs, "Estudar#2026!");
        Confirm(dialogs);

        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain("Too many attempts. Try again in 01:30."));
    }

    [Fact]
    public void Dialog_Cancel_SendsNothing()
    {
        var (_, dialogs) = OpenDialog();

        dialogs.Find("button.app-erase-cancel").Click();

        dialogs.WaitForAssertion(() => dialogs.FindAll("#erase-account-password").Should().BeEmpty());
        Api.ErasureAttempts.Should().BeEmpty();
        Services.GetRequiredService<NavigationManager>().Uri.Should().NotContain("sign-out");
    }

    [Fact]
    public void Dialog_Success_SendsThePasswordAndSignsTheBrowserOut()
    {
        var (page, dialogs) = OpenDialog();

        Type(dialogs, "Estudar#2026!");
        Confirm(dialogs);

        // The page yields one render before it navigates (the unsaved-changes guard), so wait for it.
        page.WaitForAssertion(() => Services.GetRequiredService<NavigationManager>().Uri
            .Should().EndWith($"/account/sign-out?reason={AccountEndpoints.AccountErasedReason}"));
        Api.ErasureAttempts.Should().Equal("Estudar#2026!");
    }
}
