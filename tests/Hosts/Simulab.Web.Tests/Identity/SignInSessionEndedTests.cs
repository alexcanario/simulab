using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simulab.Web.Components.Pages.Identity;
using Simulab.Web.Services.Auth;

namespace Simulab.Web.Tests.Identity;

/// <summary>B-3 BR5 and F-7 UC1-UC2: the alerts sign-in shows on arrival, and its "forgot password" link.</summary>
public class SignInSessionEndedTests : IdentityPageTestContext
{
    public SignInSessionEndedTests()
    {
        Services.AddSingleton(new AuthClient(new HttpClient(Api) { BaseAddress = new Uri("https://api.test") }, Options.Create(new OpenIddictClientOptions())));
        Services.AddSingleton<SignInTicketStore>();
    }

    [Fact]
    public void SessionEndedQuery_ShowsTheSessionEndedAlert()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/sign-in?session=ended");

        var page = Render<SignIn>();

        page.Markup.Should().Contain("Your session ended. Sign in again.");
    }

    [Fact]
    public void NoQuery_ShowsNoAlert()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/sign-in");

        var page = Render<SignIn>();

        page.Markup.Should().NotContain("Your session ended");
    }

    [Fact]
    public void PasswordChangedQuery_ShowsTheSuccessAlert()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(ResetPassword.SignInAfterResetPath);

        var page = Render<SignIn>();

        page.Markup.Should().Contain("Your password was changed. Sign in with the new one.");
    }

    [Fact]
    public void Page_LinksToForgotPassword()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/sign-in");

        var page = Render<SignIn>();

        page.Find("a[href='/forgot-password']").TextContent.Should().Be("Forgot your password?");
    }
}
