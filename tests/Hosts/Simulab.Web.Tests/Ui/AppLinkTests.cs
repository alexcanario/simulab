using Bunit;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>B-6 AC1: the kit link is a real link in the kit's colour, never the library's primary blue.</summary>
public sealed class AppLinkTests : KitTestContext
{
    [Fact]
    public void Renders_ARealLinkWithTheKitClass()
    {
        var link = Render<AppLink>(parameters => parameters
            .Add(appLink => appLink.Href, "/terms")
            .Add(appLink => appLink.Class, "mt-2")
            .AddChildContent("Terms of use"));

        var anchor = link.Find("a");
        anchor.GetAttribute("href").Should().Be("/terms");
        anchor.ClassList.Should().Contain(["app-link", "mt-2"]).And.NotContain("mud-primary-text");
        anchor.TextContent.Should().Be("Terms of use");
    }
}
