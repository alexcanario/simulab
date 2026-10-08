using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simulab.Web.Components.Pages.Identity;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;

namespace Simulab.Web.Tests.Identity;

/// <summary>
/// F-8 AC7 and AC11 through the real sign-in page: the ticket it issues carries the name and the language
/// the Api read from the account, which the completion endpoint writes into the cookies.
/// </summary>
public sealed class SignInProfileTests : IdentityPageTestContext
{
    public SignInProfileTests()
    {
        // Created by the container, so the container disposes the fake handler.
        Services.AddSingleton(_ => new FakeAuthApi { SessionFullName = "Ana Souza", SessionPreferredLanguage = "pt-PT" });
        Services.AddSingleton(provider => new AuthClient(provider.GetRequiredService<FakeAuthApi>().Client(), Options.Create(new OpenIddictClientOptions())));
        Services.AddSingleton<SignInTicketStore>();
    }

    [Fact]
    public void SignIn_IssuesATicketWithTheAccountNameAndLanguage()
    {
        var page = Render<SignIn>();
        page.Find("#sign-in-email").Change("ana@example.com");
        page.Find("#sign-in-password").Change("Estudar#2026!");

        page.Find("button.app-sign-in-submit").Click();

        page.WaitForAssertion(() => Services.GetRequiredService<NavigationManager>().Uri.Should().Contain("/account/sign-in-complete?ticket="));
        var target = Services.GetRequiredService<NavigationManager>().Uri;
        var ticketId = target[(target.IndexOf("ticket=", StringComparison.Ordinal) + "ticket=".Length)..];
        Services.GetRequiredService<SignInTicketStore>().TryConsume(ticketId, out var ticket).Should().BeTrue();
        ticket.DisplayName.Should().Be("Ana Souza");
        ticket.PreferredLanguage.Should().Be("pt-PT");
    }
}
