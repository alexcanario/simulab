using Bunit;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-43 BR1: the titled block a form is read in. It is a landmark with its own name, which is what a page
/// drawing a bare div around its fields never gives (UC1).
/// </summary>
public sealed class AppSectionCardTests : KitTestContext
{
    private IRenderedComponent<AppSectionCard> Render(string? subtitle = null) =>
        Render<AppSectionCard>(parameters => parameters
            .Add(card => card.Id, "identification")
            .Add(card => card.Title, "Identification")
            .Add(card => card.Icon, AppIcons.Exams)
            .Add(card => card.Subtitle, subtitle)
            .Add(card => card.ChildContent, builder => builder.AddMarkupContent(0, "<p>a field</p>")));

    [Fact]
    public void Render_TheSectionIsNamedByItsOwnHeading()
    {
        var card = Render();

        var section = card.Find("section.app-section-card");
        var heading = card.Find("h2.app-section-card-title");
        heading.Id.Should().Be("identification-title");
        section.GetAttribute("aria-labelledby").Should().Be(heading.Id);
        heading.TextContent.Should().Be("Identification");
    }

    [Fact]
    public void Render_TheIconIsDecorative()
    {
        var card = Render();

        card.Find(".app-section-card-icon").GetAttribute("aria-hidden").Should().Be("true");
    }

    [Fact]
    public void Render_WithoutASubtitle_NoEmptyLineIsLeftBehind()
    {
        var card = Render();

        card.FindAll(".app-section-card-subtitle").Should().BeEmpty();
    }

    [Fact]
    public void Render_WithASubtitle_ItSitsUnderTheTitle()
    {
        var card = Render("Who publishes the notice");

        card.Find(".app-section-card-subtitle").TextContent.Should().Be("Who publishes the notice");
    }

    [Fact]
    public void Render_TheChildrenAreInsideTheBody()
    {
        var card = Render();

        card.Find(".app-section-card-body").InnerHtml.Should().Contain("a field");
    }
}
