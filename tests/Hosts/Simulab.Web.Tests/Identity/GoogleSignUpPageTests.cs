using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Identity;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;

namespace Simulab.Web.Tests.Identity;

/// <summary>F-20 `/sign-up/google`: the confirmation of a first Google sign-in (UC1, BR7-BR9, AC15).</summary>
public sealed class GoogleSignUpPageTests : IdentityPageTestContext
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 9, 23, 12, 0, 0, TimeSpan.Zero));

    private FakeAuthApi Auth => Services.GetRequiredService<FakeAuthApi>();

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    private GoogleSignUpTickets Tickets => Services.GetRequiredService<GoogleSignUpTickets>();

    public GoogleSignUpPageTests()
    {
        Services.AddSingleton(_ => new FakeAuthApi());
        Services.AddSingleton(provider => new AuthClient(provider.GetRequiredService<FakeAuthApi>().Client(), Options.Create(new OpenIddictClientOptions())));
        Services.AddSingleton<SignInTicketStore>();
        Services.AddSingleton(new GoogleSignUpTickets(_clock));
        Services.AddSingleton(new GoogleSignInSettings { Enabled = true, ClientId = "client", ClientSecret = "secret" });
    }

    /// <summary>The page as the Web host's Google step leaves the visitor on it.</summary>
    private (IRenderedComponent<GoogleSignUp> Page, string Ticket) Open(string? name = "Ana Google")
    {
        var ticket = Tickets.Issue(new GoogleSignUpTicket("google-id-token", "ana@gmail.com", name));
        Navigation.NavigateTo($"{GoogleAccountEndpoints.SignUpPath}?ticket={ticket}");
        var page = Render<GoogleSignUp>();
        page.WaitForAssertion(() => page.Find("button.app-google-sign-up-submit").HasAttribute("disabled").Should().BeFalse());
        return (page, ticket);
    }

    private static void AcceptAll(IRenderedComponent<GoogleSignUp> page)
    {
        page.Find("#google-sign-up-adult").Change(true);
        page.Find("#google-sign-up-terms").Change(true);
        page.Find("#google-sign-up-privacy").Change(true);
    }

    // UC1: the Google address is shown, the name comes prefilled, and the three acceptances are asked.
    [Fact]
    public void Open_ShowsTheGoogleAddressThePrefilledNameAndTheThreeAcceptances()
    {
        var (page, _) = Open();

        page.Markup.Should().Contain("You are signing up with the Google account ana@gmail.com.");
        page.Find("#google-sign-up-full-name").GetAttribute("value").Should().Be("Ana Google");
        page.FindAll("#google-sign-up-adult, #google-sign-up-terms, #google-sign-up-privacy").Should().HaveCount(3);
        page.Markup.Should().NotContain("google-id-token", "the ID token never reaches the page (BR8)");
    }

    // AC15.
    [Fact]
    public void Open_TicketOlderThanTenMinutes_GoesBackToSignInAsExpired()
    {
        var ticket = Tickets.Issue(new GoogleSignUpTicket("google-id-token", "ana@gmail.com", null));
        _clock.Advance(GoogleSignUpTickets.Lifetime + TimeSpan.FromSeconds(1));
        Navigation.NavigateTo($"{GoogleAccountEndpoints.SignUpPath}?ticket={ticket}");

        Render<GoogleSignUp>();

        Navigation.Uri.Should().EndWith($"/sign-in?error={IdentityErrorCodes.GoogleSignInExpired}");
    }

    // AC15.
    [Fact]
    public void Open_SpentTicket_GoesBackToSignInAsExpired()
    {
        var ticket = Tickets.Issue(new GoogleSignUpTicket("google-id-token", "ana@gmail.com", null));
        Tickets.TryConsume(ticket, out _);
        Navigation.NavigateTo($"{GoogleAccountEndpoints.SignUpPath}?ticket={ticket}");

        Render<GoogleSignUp>();

        Navigation.Uri.Should().EndWith($"/sign-in?error={IdentityErrorCodes.GoogleSignInExpired}");
    }

    // AC1: off, the page does not exist.
    [Fact]
    public void Open_SwitchedOff_IsNotFound()
    {
        Services.AddSingleton(new GoogleSignInSettings());

        Render<GoogleSignUp>();

        Navigation.Uri.Should().EndWith("/not-found");
    }

    // AC5 on screen: comfort only, the Api is the authority; nothing is sent.
    [Fact]
    public void Submit_WithoutTheAcceptances_ShowsOneMessagePerRuleAndSendsNothing()
    {
        var (page, _) = Open();

        page.Find("button.app-google-sign-up-submit").Click();

        page.Markup.Should().Contain("You must be 18 or older to create an account.");
        page.Markup.Should().Contain("Accept the terms of use and the privacy policy.");
        Api.GoogleRegistrations.Should().BeEmpty();
    }

    // UC1, AC4: the confirmation sends the ID token and the versions shown, then the same token signs the new account in.
    [Fact]
    public void Submit_Valid_CreatesTheAccountAndSignsItIn()
    {
        var (page, ticket) = Open();
        page.Find("#google-sign-up-full-name").Change("  Ana Souza  ");
        AcceptAll(page);

        page.Find("button.app-google-sign-up-submit").Click();

        page.WaitForAssertion(() => Navigation.Uri.Should().Contain("/account/sign-in-complete?ticket="));
        var sent = Api.GoogleRegistrations.Single();
        sent.Should().Be(new GoogleRegistrationRequest("google-id-token", true, true, true, "2026-v1", "2026-v1", "Ana Souza"));
        Auth.GoogleForms.Single().Should().Contain("id_token=google-id-token");
        Tickets.TryPeek(ticket, out _).Should().BeFalse("the ticket is spent once the account exists");
    }

    // BR9 on screen: the account appeared meanwhile; the page offers the Google sign-in that reaches it.
    [Fact]
    public void Submit_AccountAppearedMeanwhile_OffersToContinueWithGoogle()
    {
        Api.GoogleRegisterFailure = (HttpStatusCode.Conflict, IdentityErrorCodes.GoogleAccountExists);
        var (page, ticket) = Open();
        AcceptAll(page);

        page.Find("button.app-google-sign-up-submit").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("An account with this email address already exists."));
        Tickets.TryPeek(ticket, out _).Should().BeFalse();
        page.FindAll("button").Single(button => button.TextContent.Trim() == "Continue with Google").Click();
        Navigation.Uri.Should().EndWith(GoogleAccountEndpoints.StartPath);
    }

    // A refusal the visitor can fix keeps the ticket, so the page can be sent again.
    [Fact]
    public void Submit_TermsChangedMeanwhile_KeepsTheTicketAndAsksAgain()
    {
        Api.GoogleRegisterFailure = (HttpStatusCode.Conflict, IdentityErrorCodes.TermsVersionOutdated);
        var (page, ticket) = Open();
        AcceptAll(page);

        page.Find("button.app-google-sign-up-submit").Click();

        page.WaitForAssertion(() => page.Find("#google-sign-up-terms").HasAttribute("checked").Should().BeFalse());
        Tickets.TryPeek(ticket, out _).Should().BeTrue();
    }

    [Fact]
    public void Cancel_SpendsTheTicketAndGoesToSignIn()
    {
        var (page, ticket) = Open();

        page.Find("button.app-google-sign-up-cancel").Click();

        Navigation.Uri.Should().EndWith("/sign-in");
        Tickets.TryPeek(ticket, out _).Should().BeFalse();
        Api.GoogleRegistrations.Should().BeEmpty();
    }
}
