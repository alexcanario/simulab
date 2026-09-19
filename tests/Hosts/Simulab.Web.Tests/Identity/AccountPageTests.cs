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

/// <summary>F-8 `/account`: the signed-in user's own profile (AC11-AC13).</summary>
public sealed class AccountPageTests : IdentityPageTestContext
{
    private const string WebSessionId = "web-1";

    public AccountPageTests()
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

    private IRenderedComponent<Account> RenderSignedIn(string? query = null)
    {
        Authorization.SetAuthorized("ana@exemplo.com").SetClaims(new Claim(WebAuthClaims.WebSessionId, WebSessionId));
        if (query is not null)
        {
            Services.GetRequiredService<NavigationManager>().NavigateTo($"/account{query}");
        }

        return Render<Account>();
    }

    private static void Save(IRenderedComponent<Account> page) => page.Find("button.app-form-save").Click();

    [Fact]
    public void Anonymous_IsSentToSignIn()
    {
        Render<Account>();

        Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/sign-in");
    }

    [Fact]
    public void Load_ShowsTheEmailReadOnly_TheProfile_AndTheChangePasswordLink()
    {
        var page = RenderSignedIn();

        page.Find("#account-email").GetAttribute("value").Should().Be("ana@exemplo.com");
        page.Find("#account-email").HasAttribute("readonly").Should().BeTrue();
        page.Find("#account-full-name").GetAttribute("value").Should().Be("Ana");
        page.Markup.Should().Contain("English");
        page.Find("a.app-account-password").GetAttribute("href").Should().Be("/account/password");
    }

    [Fact]
    public void Load_ApiFails_ShowsTheErrorStateWithTryAgain()
    {
        Api.Profile = null;

        var page = RenderSignedIn();

        page.Markup.Should().Contain("We could not load your profile.");
        Api.Profile = new ProfileResponse("ana@exemplo.com", "Ana", "en");
        page.Find("button.app-retry").Click();
        page.Find("#account-full-name").GetAttribute("value").Should().Be("Ana");
    }

    [Fact]
    public void NameOver120Characters_ShowsTheErrorAndDisablesSave()
    {
        var page = RenderSignedIn();

        page.Find("#account-full-name").Change(new string('a', 121));

        page.Markup.Should().Contain("Use at most 120 characters.");
        page.Find("button.app-form-save").HasAttribute("disabled").Should().BeTrue();
        page.Find("#account-full-name").Change(new string('a', 120));
        page.Find("button.app-form-save").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Save_Success_SendsTheProfileAndReloadsThroughTheWeb()
    {
        var page = RenderSignedIn();
        page.Find("#account-full-name").Change("Ana Souza");

        Save(page);

        Api.ProfileUpdates.Should().Equal(new UpdateProfileRequest("Ana Souza", "en"));
        Services.GetRequiredService<NavigationManager>().Uri
            .Should().EndWith("/account/profile-applied?redirectUri=%2Faccount%3Fsaved%3Dtrue");
    }

    [Fact]
    public void Save_ApiError_ShowsTheTranslatedAlert()
    {
        Api.UpdateProfileFailure = (HttpStatusCode.BadRequest, IdentityErrorCodes.ProfileLanguageNotSupported);
        var page = RenderSignedIn();

        Save(page);

        page.Markup.Should().Contain("Choose one of the languages in the list.");
        Services.GetRequiredService<NavigationManager>().Uri.Should().NotContain("profile-applied");
    }

    [Fact]
    public void AfterTheReload_ShowsTheSavedAlert()
    {
        var page = RenderSignedIn("?saved=true");

        page.Markup.Should().Contain("Your profile was saved.");
    }

    [Fact]
    public void Cancel_RestoresTheLoadedValues()
    {
        var page = RenderSignedIn();
        page.Find("#account-full-name").Change("Someone else");

        page.Find("button.app-form-cancel").Click();

        page.Find("#account-full-name").GetAttribute("value").Should().Be("Ana");
    }
}
