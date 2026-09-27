using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MudBlazor;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Identity;
using Simulab.Web.Components.Ui;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;

namespace Simulab.Web.Tests.Identity;

/// <summary>
/// F-29 BR14 on the screen: which of the two blocks `/account/security` carries, and when the page exists at
/// all. The two switches are independent, and v1 ships the combination the page was never built for —
/// Google on, two-factor off.
/// </summary>
public sealed class SecurityGooglePageTests : IdentityPageTestContext
{
    private const string WebSessionId = "web-1";

    public SecurityGooglePageTests()
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

    // AC18: two-factor off, Google on — the page loads, carried by the Google card alone.
    [Fact]
    public void TotpOffGoogleOn_ShowsTheGoogleCardAndNoTwoFactorBlock()
    {
        Api.TotpStatus = null;
        Api.GoogleLink = new GoogleLinkResponse(false, null, HasPassword: true);

        var page = RenderSignedIn();

        page.WaitForAssertion(() => page.Find(".app-security-google-connect"));
        page.Markup.Should().NotContain("Two-factor sign-in");
        page.FindAll("button.app-security-turn-on").Should().BeEmpty();
    }

    // AC18c: Google off, two-factor on — the page loads without the Google card.
    [Fact]
    public void GoogleOffTotpOn_ShowsNoGoogleCard()
    {
        Api.TotpStatus = new TotpStatusResponse(false);
        Api.GoogleLink = null;

        var page = RenderSignedIn();

        page.WaitForAssertion(() => page.Find("button.app-security-turn-on"));
        page.FindAll(".app-security-google-connect").Should().BeEmpty();
        page.FindAll(".app-security-google-disconnect").Should().BeEmpty();
        page.Markup.Should().NotContain("Connect Google");
    }

    // AC19: both switches off and there is nothing left, so the page is not found.
    [Fact]
    public void BothOff_IsNotFound()
    {
        Api.TotpStatus = null;
        Api.GoogleLink = null;

        var page = RenderSignedIn();

        page.FindAll(".app-security-card").Should().BeEmpty();
        page.Markup.Should().NotContain("Connect Google").And.NotContain("Two-factor sign-in");
    }

    // AC1 on the screen: a linked account shows its address and offers to disconnect.
    [Fact]
    public void Linked_ShowsTheAddressAndOffersToDisconnect()
    {
        Api.TotpStatus = new TotpStatusResponse(false);
        Api.GoogleLink = new GoogleLinkResponse(true, "ana@gmail.com", HasPassword: true);

        var page = RenderSignedIn();

        page.WaitForAssertion(() => page.Find(".app-security-google-account").TextContent.Trim().Should().Be("ana@gmail.com"));
        page.Find(".app-security-google-disconnect").HasAttribute("disabled").Should().BeFalse();
        page.FindAll(".app-security-google-connect").Should().BeEmpty();
    }

    // BR12 on the screen: a row written before F-29 has no address to show, and says so.
    [Fact]
    public void LinkedWithoutAnAddress_SaysSoInsteadOfShowingNothing()
    {
        Api.TotpStatus = new TotpStatusResponse(false);
        Api.GoogleLink = new GoogleLinkResponse(true, null, HasPassword: true);

        var page = RenderSignedIn();

        page.WaitForAssertion(() =>
            page.Find(".app-security-google-account").TextContent.Should().Contain("The address was not recorded."));
    }

    // AC14 on the screen, BR9: without a password the disconnect is disabled and the reason is on the button.
    [Fact]
    public void LinkedWithoutAPassword_DisablesTheDisconnectAndSaysWhy()
    {
        Api.TotpStatus = new TotpStatusResponse(false);
        Api.GoogleLink = new GoogleLinkResponse(true, "ana@gmail.com", HasPassword: false);

        var page = RenderSignedIn();

        page.WaitForAssertion(() => page.Find(".app-security-google-disconnect").HasAttribute("disabled").Should().BeTrue());
        // The reason is in the document, not only in a tooltip a disabled button never shows.
        var reason = page.Find(".app-security-google-needs-password");
        reason.TextContent.Should().Contain("Create a password first");
        page.Find(".app-security-google-disconnect").GetAttribute("aria-describedby").Should().Be(reason.Id);
    }

    // BR2 on the screen: connecting is a form post with the antiforgery token, never a link.
    [Fact]
    public void NotLinked_OffersAFormPostWithTheIntentAndTheAntiforgeryToken()
    {
        Api.TotpStatus = new TotpStatusResponse(false);
        Api.GoogleLink = new GoogleLinkResponse(false, null, HasPassword: true);

        var page = RenderSignedIn();

        page.WaitForAssertion(() => page.Find(".app-security-google-connect"));
        var form = page.Find("form[method=post]");
        form.GetAttribute("action").Should().Be(GoogleAccountEndpoints.StartPath);
        form.QuerySelector("input[name=intent]")!.GetAttribute("value").Should().Be(GoogleAccountEndpoints.LinkIntent);
        page.Find(".app-security-google-connect").GetAttribute("type").Should().Be("submit");
    }

    // AC7b and AC8 on the screen: the callback's refusal travels in the address, and the card shows it.
    // Without this the Web host would produce the two codes and the reader would never learn anything.
    [Theory]
    [InlineData(IdentityErrorCodes.GoogleLinkSessionChanged)]
    [InlineData(IdentityErrorCodes.GoogleLinkExpired)]
    public void RefusalInTheAddress_IsShownOnTheCardAndThenCleared(string code)
    {
        Api.TotpStatus = new TotpStatusResponse(false);
        Api.GoogleLink = new GoogleLinkResponse(false, null, HasPassword: true);
        Authorization.SetAuthorized("ana@exemplo.com").SetClaims(new Claim(WebAuthClaims.WebSessionId, WebSessionId));

        // bUnit supplies a [SupplyParameterFromQuery] through the address, never as a parameter.
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"{GoogleAccountEndpoints.SecurityPath}?error={Uri.EscapeDataString(code)}");

        var page = Render<Security>();

        page.WaitForAssertion(() => page.Find(".mud-alert").TextContent.Should().Be(ErrorTextOf(code)));
        Services.GetRequiredService<NavigationManager>().Uri
            .Should().EndWith(GoogleAccountEndpoints.SecurityPath, "the outcome is read once, not replayed on a refresh");
    }

    // The success half of the same round trip: a link that worked says so instead of landing silently.
    [Fact]
    public void LinkedInTheAddress_ConfirmsAndClearsTheAddress()
    {
        Api.TotpStatus = new TotpStatusResponse(false);
        Api.GoogleLink = new GoogleLinkResponse(true, "ana@gmail.com", HasPassword: true);
        Authorization.SetAuthorized("ana@exemplo.com").SetClaims(new Claim(WebAuthClaims.WebSessionId, WebSessionId));

        Services.GetRequiredService<NavigationManager>().NavigateTo($"{GoogleAccountEndpoints.SecurityPath}?linked=1");

        var page = Render<Security>();

        page.WaitForAssertion(() => page.Find(".app-security-google-account"));
        Services.GetRequiredService<ISnackbar>().ShownSnackbars
            .Should().Contain(shown => shown.Message!.Contains("Google connected", StringComparison.Ordinal));
        Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith(GoogleAccountEndpoints.SecurityPath);
    }

    private string ErrorTextOf(string code) => Services.GetRequiredService<ErrorText>().For(code);
}
