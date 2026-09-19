using Bunit;
using Microsoft.AspNetCore.Components;
using Simulab.Web.Components.Layout;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.Layout;

/// <summary>B-6 AC2: the auth footer's legal links are kit links, and their navigation is named for them.</summary>
public sealed class AuthLayoutLinksTests : KitTestContext
{
    private IRenderedComponent<AuthLayout> RenderLayout() =>
        Render<AuthLayout>(parameters => parameters.Add(layout => layout.Body, (RenderFragment)(builder => builder.AddContent(0, "page"))));

    [Fact]
    public void Footer_IsNamedLegal_AndItsLinksAreKitLinks()
    {
        var layout = RenderLayout();

        var footer = layout.Find("nav.app-auth-footer");
        footer.GetAttribute("aria-label").Should().Be("Legal");
        var links = footer.QuerySelectorAll("a");
        links.Select(link => link.GetAttribute("href")).Should().Equal("/terms", "/privacy");
        links.Should().AllSatisfy(link => link.ClassList.Should().Contain("app-link").And.NotContain("mud-primary-text"));
    }
}
