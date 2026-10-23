using Bunit;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-126 BR1 and BR2: the numbered section card. The number is a decorative tag drawn in place of the icon; the
/// heading keeps its text and its id, so a screen reader hears the section's name as before.
/// </summary>
public sealed class AppSectionCardNumberTests : KitTestContext
{
    private IRenderedComponent<AppSectionCard> RenderCard(int? number) =>
        Render<AppSectionCard>(parameters => parameters
            .Add(card => card.Id, "classification")
            .Add(card => card.Title, "Classification")
            .Add(card => card.Icon, AppIcons.Exams)
            .Add(card => card.Number, number)
            .Add(card => card.ChildContent, builder => builder.AddMarkupContent(0, "<p>a field</p>")));

    [Fact]
    public void WithANumber_TheHeadShowsTheNumberTagInPlaceOfTheIcon()
    {
        var card = RenderCard(3);

        var tag = card.Find(".app-section-card-number");
        tag.TextContent.Trim().Should().Be("3");
        tag.GetAttribute("aria-hidden").Should().Be("true", "the number is decorative");
        card.FindAll(".app-section-card-icon").Should().BeEmpty("the number takes the icon's place");
    }

    [Fact]
    public void WithANumber_TheHeadingKeepsItsTextAndItsId()
    {
        var card = RenderCard(3);

        var heading = card.Find("h2.app-section-card-title");
        heading.Id.Should().Be("classification-title");
        heading.TextContent.Trim().Should().Be("Classification", "the number is not part of the name");
        card.Find("section.app-section-card").GetAttribute("aria-labelledby").Should().Be("classification-title");
    }

    [Fact]
    public void WithoutANumber_TheCardIsUnchanged()
    {
        var card = RenderCard(null);

        card.FindAll(".app-section-card-number").Should().BeEmpty();
        card.Find(".app-section-card-icon").GetAttribute("aria-hidden").Should().Be("true");
    }
}
