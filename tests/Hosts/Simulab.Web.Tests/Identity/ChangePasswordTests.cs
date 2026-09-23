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

/// <summary>F-7 `/account/password`: the signed-in user's own password.</summary>
public sealed class ChangePasswordTests : IdentityPageTestContext
{
    private const string WebSessionId = "web-1";

    public ChangePasswordTests()
    {
        var store = new InMemoryWebSessionStore();
        store.SaveAsync(WebSessionId, new WebSession("jti-1", "access-1", "refresh-1", DateTimeOffset.UtcNow.AddMinutes(10), [], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30)));
        // Created by the container, so the container disposes the fake handler.
        Services.AddSingleton<FakeAuthApi>();
        Services.AddSingleton(provider => new WebSessionTokenAccessor(
            store,
            new AuthClient(provider.GetRequiredService<FakeAuthApi>().Client(), Options.Create(new OpenIddictClientOptions())),
            new SessionRefreshGate(),
            TimeProvider.System));
    }

    private IRenderedComponent<ChangePassword> RenderSignedIn()
    {
        Authorization.SetAuthorized("ana@exemplo.com").SetClaims(new Claim(WebAuthClaims.WebSessionId, WebSessionId));
        return Render<ChangePassword>();
    }

    private static void Fill(IRenderedComponent<ChangePassword> page, string current = "Estudar#2026!", string next = "Revisar#2026!x", string? confirm = null)
    {
        page.Find("#change-password-current").Change(current);
        page.Find("#change-password-new").Change(next);
        page.Find("#change-password-confirm").Change(confirm ?? next);
    }

    private static void Save(IRenderedComponent<ChangePassword> page) => page.Find("button.app-form-save").Click();

    [Fact]
    public void Anonymous_IsSentToSignIn()
    {
        Render<ChangePassword>();

        Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/sign-in");
    }

    [Fact]
    public void Save_Success_ShowsTheMessageAndClearsTheFields()
    {
        var page = RenderSignedIn();
        Fill(page);

        Save(page);

        page.Markup.Should().Contain("Password changed. Other devices were signed out.");
        page.Find("#change-password-current").GetAttribute("value").Should().BeNullOrEmpty();
        Api.CountOf("/password-changes").Should().Be(1);
    }

    [Fact]
    public void Save_EmptyForm_ShowsOneMessagePerField()
    {
        var page = RenderSignedIn();

        Save(page);

        page.Markup.Should().Contain("Enter your current password.").And.Contain("Enter a password.").And.Contain("Repeat the password.");
        Api.CountOf("/password-changes").Should().Be(0);
    }

    // F-20 AC16: an account created with Google has no password to change; the page says how to create one.
    [Fact]
    public void Save_AccountWithoutPassword_ShowsHowToCreateOne()
    {
        Api.ChangeFailure = (HttpStatusCode.UnprocessableEntity, IdentityErrorCodes.PasswordNotSet);
        var page = RenderSignedIn();
        Fill(page);

        Save(page);

        page.WaitForAssertion(() => page.Markup.Should().Contain("Your account was created with Google and has no password yet."));
        page.FindAll("button").Single(button => button.TextContent.Trim() == "Create a password").Click();
        Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/forgot-password");
    }

    [Fact]
    public void Save_WrongCurrentPassword_ShowsItOnTheCurrentField()
    {
        Api.ChangeFailure = (HttpStatusCode.UnprocessableEntity, IdentityErrorCodes.PasswordChangeCurrentInvalid);
        var page = RenderSignedIn();
        Fill(page);

        Save(page);

        page.Markup.Should().Contain("The current password is not correct.");
        page.Find("#change-password-current").GetAttribute("aria-invalid").Should().Be("true");
    }

    [Fact]
    public void Save_Locked_ShowsTheTimeLeftAndDisablesSave()
    {
        Api.ChangeLockedSeconds = 600;
        var page = RenderSignedIn();
        Fill(page);

        Save(page);

        page.Markup.Should().Contain("Too many attempts. Try again in 10:00.");
        page.Find("button.app-form-save").HasAttribute("disabled").Should().BeTrue();
    }
}
