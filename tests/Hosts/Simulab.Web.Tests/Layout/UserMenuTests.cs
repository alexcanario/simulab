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

    [Fact]
    public void SignedIn_OffersChangePasswordBeforeSignOut()
    {
        Authorization.SetAuthorized("ana@exemplo.com");

        // The menu items render in MudBlazor's popover host, as in the app bar.
        var popovers = Render<MudBlazor.MudPopoverProvider>();
        var cut = Render<UserMenu>();
        cut.Find(".app-user-menu-button button").Click();

        var item = popovers.Find(".app-user-menu-password");
        item.TextContent.Trim().Should().Be("Change password");
        item.GetAttribute("href").Should().Be("/account/password");
        popovers.Markup.IndexOf("app-user-menu-password", StringComparison.Ordinal)
            .Should().BeLessThan(popovers.Markup.IndexOf("app-user-menu-signout", StringComparison.Ordinal));
    }
}
