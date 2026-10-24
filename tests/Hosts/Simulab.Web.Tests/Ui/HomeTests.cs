using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Web.Components.Pages;

namespace Simulab.Web.Tests.Ui;

/// <summary>B-9 AC2: the home status notice is the kit's alert, the only one that is readable in both themes.</summary>
public sealed class HomeTests : KitTestContext
{
    [Fact]
    public void StatusNotice_UsesTheFilledKitAlert()
    {
        Services.AddSingleton<Microsoft.Extensions.Hosting.IHostEnvironment>(new FakeHostEnvironment("Production"));
        var home = Render<Home>();

        var alert = home.Find(".mud-alert");
        alert.ClassList.Should().Contain("mud-alert-filled-info");
        alert.QuerySelector(".app-alert-content").Should().NotBeNull("the notice is rendered by AppAlert");
    }
}
