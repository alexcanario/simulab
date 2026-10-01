using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Identity;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;

namespace Simulab.Web.Tests.Identity;

/// <summary>F-38 through the real sign-in page: the refusal of the per-address limit (AC12) and the address the page forwards (AC9, BR6).</summary>
public sealed class SignInRateLimitPageTests : IdentityPageTestContext
{
    private FakeAuthApi Auth => Services.GetRequiredService<FakeAuthApi>();

    public SignInRateLimitPageTests()
    {
        Services.AddSingleton(_ => new FakeAuthApi { RequireTotp = false });
        Services.AddSingleton(provider => new AuthClient(
            provider.GetRequiredService<FakeAuthApi>().Client(),
            Options.Create(new OpenIddictClientOptions { ClientId = "simulab-web", ClientSecret = WebSecret })));
        Services.AddSingleton<SignInTicketStore>();
    }

    private IRenderedComponent<SignIn> Submit()
    {
        var page = Render<SignIn>();
        page.Find("#sign-in-email").Change("ana@example.com");
        page.Find("#sign-in-password").Change("Estudar#2026!");
        page.Find("button.app-sign-in-submit").Click();
        return page;
    }

    // AC12: the password form shows the alert with the time left.
    [Fact]
    public void Password_RateLimited_ShowsTheAlertWithTheTimeLeft()
    {
        Auth.PasswordError = IdentityErrorCodes.SignInRateLimited;
        Auth.PasswordErrorDescription = "600";

        var page = Submit();

        page.WaitForAssertion(() => page.Markup.Should().Contain("Too many attempts from this network. Try again in 10:00."));
        page.Find("#sign-in-password").Should().NotBeNull("the page stays on the password form");
    }

    // AC12: the same alert after the code step, back on the password form.
    [Fact]
    public void Code_RateLimited_GoesBackToThePasswordFormWithTheAlert()
    {
        Auth.RequireTotp = true;
        Auth.TotpError = IdentityErrorCodes.SignInRateLimited;
        Auth.TotpErrorDescription = "600";
        var page = Submit();
        page.WaitForAssertion(() => page.Find("#sign-in-code"));
        page.Find("#sign-in-code").Change("123456");

        page.Find("button.app-sign-in-code-submit").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("Too many attempts from this network. Try again in 10:00."));
        page.Find("#sign-in-password").Should().NotBeNull();
    }

    // AC9, BR6: both steps carry the visitor's address and the Web's secret.
    [Fact]
    public void PasswordAndCodeSteps_CarryTheVisitorsAddressAndTheWebSecret()
    {
        Visitor.Address = "203.0.113.10";
        Auth.RequireTotp = true;
        var page = Submit();
        page.WaitForAssertion(() => page.Find("#sign-in-code"));
        page.Find("#sign-in-code").Change("123456");
        page.Find("button.app-sign-in-code-submit").Click();

        page.WaitForAssertion(() => Auth.TotpForms.Should().HaveCount(1));
        Auth.TokenCalls.Should().HaveCount(2);
        Auth.TokenCalls.Should().OnlyContain(call => call.Address == "203.0.113.10" && call.Secret == WebSecret);
    }

    // BR6: without a known address, neither header is sent and the Api keys on the connection.
    [Fact]
    public void PasswordStep_UnknownVisitor_SendsNeitherHeader()
    {
        Auth.PasswordError = IdentityErrorCodes.InvalidCredentials;

        var page = Submit();

        page.WaitForAssertion(() => Auth.TokenCalls.Should().ContainSingle());
        Auth.TokenCalls.Single().Address.Should().BeNull();
        Auth.TokenCalls.Single().Secret.Should().BeNull();
    }

    // BR6: the Google step and the refresh grant stay as they were.
    [Fact]
    public async Task GoogleAndRefreshSteps_SendNoVisitorHeaders()
    {
        var client = new AuthClient(Auth.Client(), Options.Create(new OpenIddictClientOptions { ClientSecret = WebSecret }));

        await client.SignInWithGoogleAsync("id-token");
        await client.RefreshAsync("refresh");

        Auth.TokenCalls.Should().HaveCount(2).And.OnlyContain(call => call.Address == null && call.Secret == null);
    }
}
