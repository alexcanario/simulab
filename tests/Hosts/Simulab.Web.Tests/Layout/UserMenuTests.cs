using Bunit;
using Simulab.Web.Components.Layout;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.Layout;

/// <summary>
/// F-5, BR10 and AC10: the app bar's user menu slot. Opening the menu and clicking "Sign out" is a
/// MudBlazor popover interaction, checked on screen (validation script) rather than through bUnit.
/// </summary>
public sealed class UserMenuTests : KitTestContext
{
    [Fact]
    public void Anonymous_ShowsASignInLink()
    {
        var cut = Render<UserMenu>();

        cut.Find(".app-sign-in-link").TextContent.Trim().Should().Be("Sign in");
        cut.Find(".app-sign-in-link").GetAttribute("href").Should().Be("/sign-in");
    }

    [Fact]
    public void SignedIn_ShowsTheAccountButtonNamedForTheUser()
    {
        Authorization.SetAuthorized("ana@exemplo.com");

        var cut = Render<UserMenu>();

        cut.Find(".app-user-menu-button button").GetAttribute("aria-label").Should().Be("Account menu for ana@exemplo.com");
        cut.FindAll(".app-sign-in-link").Should().BeEmpty();
    }
}
