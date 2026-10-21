using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Identity;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;

namespace Simulab.Web.Tests.Identity;

/// <summary>F-85 `/accept-terms`: the page a user who owes an acceptance lands on (AC14, AC18, AC19, AC23).</summary>
public sealed class AcceptTermsPageTests : IdentityPageTestContext
{
    private const string WebSessionId = "web-1";

    public AcceptTermsPageTests()
    {
        var store = new InMemoryWebSessionStore();
        store.SaveAsync(
            WebSessionId,
            new WebSession("jti-1", "access-1", "refresh-1", DateTimeOffset.UtcNow.AddMinutes(10), [], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1)))
            .GetAwaiter().GetResult();
        Services.AddSingleton<FakeAuthApi>();
        Services.AddSingleton(provider => new WebSessionTokenAccessor(
            store,
            new AuthClient(provider.GetRequiredService<FakeAuthApi>().Client(), Options.Create(new OpenIddictClientOptions())),
            new SessionRefreshGate(),
            TimeProvider.System));
    }

    private IRenderedComponent<AcceptTerms> RenderSignedIn(string? returnUrl = null)
    {
        Authorization.SetAuthorized("ana@exemplo.com").SetClaims(new System.Security.Claims.Claim(WebAuthClaims.WebSessionId, WebSessionId));
        var query = returnUrl is null ? string.Empty : $"?returnUrl={Uri.EscapeDataString(returnUrl)}";
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/accept-terms{query}");
        return Render<AcceptTerms>();
    }

    private string CurrentPath => new Uri(Services.GetRequiredService<NavigationManager>().Uri).PathAndQuery;

    // UC1: only the documents that changed are listed, with a way to read each one.
    [Fact]
    public void Owing_ListsOnlyTheChangedDocumentsWithTheirVersionAndAReadLink()
    {
        Api.LegalStatus = new LegalAcceptanceStatusResponse(
            true,
            [
                new LegalAcceptanceDocumentResponse(LegalTopic.Terms, "2026-v1", new DateOnly(2026, 9, 17), "Terms of use", false, false),
                new LegalAcceptanceDocumentResponse(LegalTopic.Privacy, "2026-v3", new DateOnly(2026, 10, 9), "Privacy policy", true, true),
            ]);

        var page = RenderSignedIn();

        page.Find("h1").TextContent.Should().Be("Our documents changed");
        page.FindAll("li.app-accept-document").Should().ContainSingle();
        var row = page.Find("li.app-accept-document[data-topic=Privacy]");
        row.TextContent.Should().Contain("Privacy policy").And.Contain("Version 2026-v3");
        row.TextContent.Should().Contain("Draft text", "a placeholder document keeps its draft notice");
        row.QuerySelector("a.app-link")!.GetAttribute("href").Should().Be("/privacy");
        page.Find("button.app-accept-submit").TextContent.Should().Contain("Accept and continue");
        page.Find("button.app-accept-signout").TextContent.Should().Contain("Sign out");
        page.Find(".app-accept-data a").GetAttribute("href").Should().Be("/account");
    }

    // AC18: the acceptance sends the versions the page showed and lands on the page that was asked for.
    [Fact]
    public void Accept_SendsTheShownVersionsAndGoesToTheLocalReturnUrl()
    {
        var page = RenderSignedIn("/exams?tab=2");

        page.Find("button.app-accept-submit").Click();

        page.WaitForAssertion(() => CurrentPath.Should().Be("/exams?tab=2"));
        Api.Acceptances.Should().ContainSingle().Which.Should().Be(new AcceptLegalDocumentsRequest("2026-v1", "2026-v3", "en"));
    }

    [Fact]
    public void Accept_WithoutReturnUrl_GoesHome()
    {
        var page = RenderSignedIn();

        page.Find("button.app-accept-submit").Click();

        page.WaitForAssertion(() => CurrentPath.Should().Be("/"));
    }

    // AC14 [sec]: only a local path is followed.
    [Theory]
    [InlineData("https://evil.example/steal")]
    [InlineData("//evil.example/steal")]
    [InlineData("/\\evil.example")]
    [InlineData("/exams\\..\\evil")]
    [InlineData("/ex\tams")]
    [InlineData("/accept-terms?returnUrl=/exams")]
    [InlineData("exams")]
    public void Accept_AReturnUrlThatIsNotALocalPath_GoesHome(string returnUrl)
    {
        var page = RenderSignedIn(returnUrl);

        page.Find("button.app-accept-submit").Click();

        page.WaitForAssertion(() => CurrentPath.Should().Be("/"));
    }

    // AC18: a newer version published meanwhile reloads the documents and says so.
    [Fact]
    public void Accept_ANewerVersionWasPublished_ReloadsTheDocumentsAndAsksAgain()
    {
        Api.AcceptFailure = (HttpStatusCode.Conflict, IdentityErrorCodes.LegalAcceptanceVersionOutdated);
        Api.OnAcceptFailure = () =>
        {
            Api.LegalStatus = IdentityPageTestContext.StubApiHandler.OwingBoth(privacy: "2026-v4");
            Api.AcceptFailure = null;
        };
        var page = RenderSignedIn();

        page.Find("button.app-accept-submit").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("changed while you were reading"));
        page.Markup.Should().Contain("Version 2026-v4");
        page.Find("button.app-accept-submit").HasAttribute("disabled").Should().BeFalse();
        CurrentPath.Should().StartWith("/accept-terms");

        page.Find("button.app-accept-submit").Click();

        page.WaitForAssertion(() => CurrentPath.Should().Be("/"));
        Api.Acceptances.Last().PrivacyVersion.Should().Be("2026-v4");
    }

    [Fact]
    public void Accept_AnotherFailure_ShowsTheErrorWithTryAgain()
    {
        Api.AcceptFailure = (HttpStatusCode.InternalServerError, "identity.unexpected");
        var page = RenderSignedIn();

        page.Find("button.app-accept-submit").Click();

        page.WaitForAssertion(() => page.FindAll("[role=alert]").Should().NotBeEmpty());
        CurrentPath.Should().StartWith("/accept-terms");
    }

    // The status call fails: the page says so and offers a retry, nothing is accepted.
    [Fact]
    public void Load_TheStatusFails_ShowsTheErrorStateWithTryAgain()
    {
        Api.LegalStatusFailure = (HttpStatusCode.InternalServerError, "identity.unexpected");

        var page = RenderSignedIn();

        page.Markup.Should().Contain("Try again");
        page.FindAll("button.app-accept-submit").Should().BeEmpty();
        Api.Acceptances.Should().BeEmpty();
    }

    // A link kept from before, or a second tab: nothing is owed, so the page is not shown.
    [Fact]
    public void Load_NothingIsOwed_LeavesForTheReturnUrl()
    {
        Api.LegalStatus = new LegalAcceptanceStatusResponse(false, []);

        RenderSignedIn("/exams");

        CurrentPath.Should().Be("/exams");
    }

    // AC19: no decline button; signing out is the way out.
    [Fact]
    public void SignOut_GoesToTheSignOutEndpoint_AndThereIsNoDeclineButton()
    {
        var page = RenderSignedIn();

        page.Markup.Should().NotContainEquivalentOf("decline");
        page.Find("button.app-accept-signout").Click();

        CurrentPath.Should().Be("/account/sign-out");
    }

    // AC23: the page in the three UI languages.
    [Theory]
    [InlineData("pt-BR", "Nossos documentos mudaram", "Aceitar e continuar")]
    [InlineData("pt-PT", "Os nossos documentos mudaram", "Aceitar e continuar")]
    [InlineData("en", "Our documents changed", "Accept and continue")]
    public void Page_IsTranslated(string culture, string title, string accept)
    {
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo(culture);

        var page = RenderSignedIn();

        page.Find("h1").TextContent.Should().Be(title);
        page.Find("button.app-accept-submit").TextContent.Should().Contain(accept);
        Api.Requests.Should().Contain(request => request.Headers.AcceptLanguage.ToString().StartsWith(culture, StringComparison.Ordinal));
    }
}
