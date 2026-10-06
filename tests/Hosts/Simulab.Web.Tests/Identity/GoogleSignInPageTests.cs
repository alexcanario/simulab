using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simulab.Web.Components.Pages.Identity;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;

namespace Simulab.Web.Tests.Identity;

/// <summary>F-20 on the sign-in and sign-up pages: the Google button behind the switch, and the code step a Google sign-in reaches.</summary>
public sealed class GoogleSignInPageTests : IdentityPageTestContext
{
    private FakeAuthApi Auth => Services.GetRequiredService<FakeAuthApi>();

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    public GoogleSignInPageTests()
    {
        Services.AddSingleton(_ => new FakeAuthApi());
        Services.AddSingleton(provider => new AuthClient(provider.GetRequiredService<FakeAuthApi>().Client(), Options.Create(new OpenIddictClientOptions())));
        Services.AddSingleton<SignInTicketStore>();
    }

    private void TurnGoogleOn() =>
        Services.AddSingleton(new GoogleSignInSettings { Enabled = true, ClientId = "client", ClientSecret = "secret" });

    // AC1.
    [Fact]
    public void SignInAndSignUp_SwitchedOff_ShowNoGoogleButton()
    {
        var signIn = Render<SignIn>();
        var signUp = Render<SignUp>();

        signIn.FindAll("button.app-google-button").Should().BeEmpty();
        signIn.FindAll(".app-divider").Should().BeEmpty();
        signUp.FindAll("button.app-google-button").Should().BeEmpty();
    }

    // Screens and API, AC14: the button under an "or" divider starts the round trip with a full page load.
    [Fact]
    public void SignIn_SwitchedOn_ShowsTheButtonThatStartsTheRoundTrip()
    {
        TurnGoogleOn();
        var page = Render<SignIn>();

        page.Find(".app-divider").TextContent.Trim().Should().Be("or");
        var button = page.Find("button.app-google-button");
        button.TextContent.Trim().Should().Be("Continue with Google");
        button.QuerySelector("svg.app-google-mark")!.GetAttribute("aria-hidden").Should().Be("true");

        button.Click();

        page.WaitForAssertion(() => Navigation.Uri.Should().EndWith(GoogleAccountEndpoints.StartPath));
    }

    [Fact]
    public void SignUp_SwitchedOn_ShowsTheButtonThatStartsTheRoundTrip()
    {
        TurnGoogleOn();
        var page = Render<SignUp>();

        page.Find("button.app-google-button").Click();

        page.WaitForAssertion(() => Navigation.Uri.Should().EndWith(GoogleAccountEndpoints.StartPath));
    }

    // BR6, AC10 on screen: a Google sign-in that needs the code opens the code step; the code goes with its challenge.
    [Fact]
    public void SignIn_WithACodeStepTicket_OpensTheCodeStepAndSendsTheChallenge()
    {
        TurnGoogleOn();
        Auth.RequireTotp = true;
        var ticket = Services.GetRequiredService<CodeStepTickets>().Issue(new CodeStepTicket("challenge-g"));
        Navigation.NavigateTo($"/sign-in?{GoogleAccountEndpoints.CodeStepQuery}={ticket}");

        var page = Render<SignIn>();
        page.FindAll("#sign-in-password").Should().BeEmpty();
        page.Find("#sign-in-code").Change("123456");
        page.Find("button.app-sign-in-code-submit").Click();

        page.WaitForAssertion(() => Navigation.Uri.Should().Contain("/account/sign-in-complete?ticket="));
        Auth.TotpForms.Single().Should().Contain("challenge=challenge-g").And.Contain("code=123456");
    }

    // An unknown or expired code-step ticket leaves the password form.
    [Fact]
    public void SignIn_WithAnUnknownCodeStepTicket_ShowsThePasswordForm()
    {
        TurnGoogleOn();
        Navigation.NavigateTo($"/sign-in?{GoogleAccountEndpoints.CodeStepQuery}=unknown");

        var page = Render<SignIn>();

        page.Find("#sign-in-password").Should().NotBeNull();
        page.FindAll("#sign-in-code").Should().BeEmpty();
    }
}
