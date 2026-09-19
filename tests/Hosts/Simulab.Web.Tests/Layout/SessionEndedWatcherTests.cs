using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Web.Components.Layout;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.Layout;

/// <summary>B-3, BR5: when the circuit's revalidation ends the session, the browser goes to sign in again.</summary>
public sealed class SessionEndedWatcherTests : KitTestContext
{
    [Fact]
    public void SignedInThenAnonymous_GoesThroughSignOutWithTheSessionEndedReason()
    {
        var authorization = Authorization;
        authorization.SetAuthorized("ana@example.com");
        Render<SessionEndedWatcher>();

        authorization.SetNotAuthorized();

        Services.GetRequiredService<NavigationManager>().Uri
            .Should().EndWith($"/account/sign-out?reason={AccountEndpoints.SessionEndedReason}");
    }

    [Fact]
    public void AnonymousFromTheStart_StaysPut()
    {
        Render<SessionEndedWatcher>();
        var before = Services.GetRequiredService<NavigationManager>().Uri;

        Authorization.SetNotAuthorized();

        Services.GetRequiredService<NavigationManager>().Uri.Should().Be(before);
    }
}
