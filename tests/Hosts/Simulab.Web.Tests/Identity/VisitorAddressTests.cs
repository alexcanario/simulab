using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Simulab.Identity.Contracts;
using Simulab.Web.Components;
using Simulab.Web.Components.Layout;
using Simulab.Web.Components.Pages.Identity;

namespace Simulab.Web.Tests.Identity;

/// <summary>B-4 AC4: a page's calls to the Api carry the visitor's address, proven by the Web's secret.</summary>
public sealed class VisitorAddressTests : IdentityPageTestContext
{
    [Fact]
    public void PageCall_CarriesTheVisitorsAddressAndTheWebSecret()
    {
        Visitor.Address = "203.0.113.10";
        var page = Render<ForgotPassword>();
        page.Find("#forgot-password-email").Change("ana@exemplo.com");

        page.Find("button.app-forgot-password-submit").Click();

        var call = Api.Requests.Single(request => request.RequestUri!.AbsolutePath.EndsWith("/password-reset-requests", StringComparison.Ordinal));
        call.Headers.GetValues(ClientAddressHeaders.Address).Should().Equal("203.0.113.10");
        call.Headers.GetValues(ClientAddressHeaders.Secret).Should().Equal(WebSecret);
    }

    [Fact]
    public void UnknownVisitor_SendsNeitherHeader()
    {
        var page = Render<ForgotPassword>();
        page.Find("#forgot-password-email").Change("ana@exemplo.com");

        page.Find("button.app-forgot-password-submit").Click();

        var call = Api.Requests.Single(request => request.RequestUri!.AbsolutePath.EndsWith("/password-reset-requests", StringComparison.Ordinal));
        call.Headers.Contains(ClientAddressHeaders.Address).Should().BeFalse();
        call.Headers.Contains(ClientAddressHeaders.Secret).Should().BeFalse();
    }

    /// <summary>The circuit's root receives the address App read at the first request and hands it to the client.</summary>
    [Fact]
    public void Routes_StoresTheVisitorAddressItReceives()
    {
        Services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment("Production"));

        Render<Routes>(parameters => parameters
            .Add(routes => routes.Preferences, ShellPreferences.Default)
            .Add(routes => routes.VisitorAddress, "198.51.100.20"));

        Visitor.Address.Should().Be("198.51.100.20");
    }
}
