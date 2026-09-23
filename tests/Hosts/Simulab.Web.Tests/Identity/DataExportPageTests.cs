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

/// <summary>F-16 AC9: the "Your data" card on `/account` and its confirmation dialog.</summary>
public sealed class DataExportPageTests : IdentityPageTestContext
{
    private const string WebSessionId = "web-1";

    public DataExportPageTests()
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
        JSInterop.SetupVoid(Account.DownloadFunction, _ => true).SetVoidResult();
    }

    private IRenderedComponent<MudDialogProvider> Providers()
    {
        Render<MudPopoverProvider>();
        Render<MudSnackbarProvider>();
        return Render<MudDialogProvider>();
    }

    /// <summary>Opens the page, clicks "Download my data" and waits for the dialog's password field.</summary>
    private (IRenderedComponent<Account> Page, IRenderedComponent<MudDialogProvider> Dialogs) OpenDialog()
    {
        var dialogs = Providers();
        var page = Render<Account>();
        page.WaitForAssertion(() => page.FindAll("button.app-download-open").Should().ContainSingle());
        page.Find("button.app-download-open").Click();
        dialogs.WaitForAssertion(() => dialogs.FindAll("#download-data-password").Should().ContainSingle());
        return (page, dialogs);
    }

    private static void Type(IRenderedComponent<MudDialogProvider> dialogs, string password) =>
        dialogs.Find("#download-data-password").Change(password);

    private static void Confirm(IRenderedComponent<MudDialogProvider> dialogs) =>
        dialogs.Find("button.app-download-confirm").Click();

    [Fact]
    public void Page_ShowsTheYourDataCardBeforeTheDangerZone()
    {
        var page = Render<Account>();

        page.WaitForAssertion(() => page.Markup.Should().Contain("Your data"));
        page.Markup.Should().Contain("the IP address they were accepted from");
        page.Markup.IndexOf("app-download-open", StringComparison.Ordinal)
            .Should().BeLessThan(page.Markup.IndexOf("app-erase-open", StringComparison.Ordinal));
    }

    [Fact]
    public void Dialog_WithoutAPassword_CannotConfirm()
    {
        var (_, dialogs) = OpenDialog();

        dialogs.Find("button.app-download-confirm").HasAttribute("disabled").Should().BeTrue();

        Type(dialogs, "Estudar#2026!");

        dialogs.Find("button.app-download-confirm").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Dialog_Success_SendsThePasswordHandsTheFileToTheBrowserAndSaysSo()
    {
        var (_, dialogs) = OpenDialog();

        Type(dialogs, "Estudar#2026!");
        Confirm(dialogs);

        dialogs.WaitForAssertion(() => JSInterop.VerifyInvoke(Account.DownloadFunction));
        var invocation = JSInterop.VerifyInvoke(Account.DownloadFunction);
        invocation.Arguments[0].Should().Be("simulab-my-data-2026-09-21.json");
        invocation.Arguments[1].Should().Be("application/json");
        Api.ExportAttempts.Should().Equal("Estudar#2026!");
        dialogs.WaitForAssertion(() => dialogs.FindAll("#download-data-password").Should().BeEmpty());
        var snackbar = Services.GetRequiredService<ISnackbar>();
        dialogs.WaitForAssertion(() => snackbar.ShownSnackbars.Should().ContainSingle(s => s.Message == "Your download has started. A confirmation email is on its way."));
    }

    [Fact]
    public void Dialog_WrongPassword_StaysOpenWithTheTranslatedError()
    {
        Api.ExportFailure = (HttpStatusCode.UnprocessableEntity, IdentityErrorCodes.DataExportCurrentPasswordInvalid);
        var (_, dialogs) = OpenDialog();

        Type(dialogs, "not-the-password");
        Confirm(dialogs);

        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain("The current password is not correct."));
        dialogs.FindAll("#download-data-password").Should().ContainSingle("the dialog stays open on a failure");
        JSInterop.Invocations.Should().NotContain(i => i.Identifier == Account.DownloadFunction);
    }

    // F-20 AC16.
    [Fact]
    public void Dialog_AccountWithoutPassword_ShowsHowToCreateOne()
    {
        Api.ExportFailure = (HttpStatusCode.UnprocessableEntity, IdentityErrorCodes.PasswordNotSet);
        var (_, dialogs) = OpenDialog();

        Type(dialogs, "anything");
        Confirm(dialogs);

        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain("Your account was created with Google and has no password yet."));
        dialogs.FindAll("button").Single(button => button.TextContent.Trim() == "Create a password").Click();
        Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/forgot-password");
    }

    [Fact]
    public void Dialog_LockedOut_ShowsTheWait()
    {
        Api.ExportLockedSeconds = 90;
        var (_, dialogs) = OpenDialog();

        Type(dialogs, "Estudar#2026!");
        Confirm(dialogs);

        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain("Too many attempts. Try again in 01:30."));
        JSInterop.Invocations.Should().NotContain(i => i.Identifier == Account.DownloadFunction);
    }

    [Fact]
    public void Dialog_Cancel_SendsNothing()
    {
        var (_, dialogs) = OpenDialog();

        dialogs.Find("button.app-download-cancel").Click();

        dialogs.WaitForAssertion(() => dialogs.FindAll("#download-data-password").Should().BeEmpty());
        Api.ExportAttempts.Should().BeEmpty();
    }
}
