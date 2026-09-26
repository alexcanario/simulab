using Bunit;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-43 BR1 and UC2: the choice that changes the rest of the form, as cards that explain themselves. The
/// keyboard is the part a page cannot get right on its own: one tab stop for the group, arrows between cards.
/// </summary>
public sealed class AppRadioCardsTests : KitTestContext
{
    private static readonly IReadOnlyList<AppRadioCardOption<string>> Scopes =
    [
        new("National", "National", "The whole country"),
        new("State", "State", "One state"),
        new("Municipal", "Municipal", "One municipality")
    ];

    private string? _chosen;

    private IRenderedComponent<AppRadioCards<string>> Render(string? value = null, string? error = null) =>
        Render<AppRadioCards<string>>(parameters => parameters
            .Add(cards => cards.Id, "exam-scope")
            .Add(cards => cards.Label, "Scope")
            .Add(cards => cards.Options, Scopes)
            .Add(cards => cards.Value, value)
            .Add(cards => cards.Required, true)
            .Add(cards => cards.Error, error)
            .Add(cards => cards.ValueChanged, (string? picked) => _chosen = picked));

    [Fact]
    public void Render_TheGroupIsARadiogroupNamedByItsLabel()
    {
        var cards = Render();

        var group = cards.Find("[role=\"radiogroup\"]");
        group.GetAttribute("aria-labelledby").Should().Be("exam-scope-label");
        group.GetAttribute("aria-required").Should().Be("true");
        cards.FindAll("[role=\"radio\"]").Should().HaveCount(3);
    }

    [Fact]
    public void Render_EachCardCarriesItsOwnExplanation()
    {
        var cards = Render();

        cards.FindAll(".app-radio-card-description")
            .Select(card => card.TextContent)
            .Should().Equal("The whole country", "One state", "One municipality");
    }

    [Fact]
    public void Render_NothingChosen_OnlyTheFirstCardIsATabStop()
    {
        var cards = Render();

        cards.FindAll("[role=\"radio\"]").Select(card => card.GetAttribute("tabindex"))
            .Should().Equal("0", "-1", "-1");
    }

    [Fact]
    public void Render_Chosen_TheChosenCardIsTheTabStopAndTheOnlyCheckedOne()
    {
        var cards = Render("State");

        cards.FindAll("[role=\"radio\"]").Select(card => card.GetAttribute("tabindex"))
            .Should().Equal("-1", "0", "-1");
        cards.FindAll("[role=\"radio\"]").Select(card => card.GetAttribute("aria-checked"))
            .Should().Equal("false", "true", "false");
        cards.FindAll(".app-radio-card-selected").Should().ContainSingle();
    }

    [Fact]
    public void Click_ACard_ChoosesIt()
    {
        var cards = Render();

        cards.FindAll("[role=\"radio\"]")[2].Click();

        _chosen.Should().Be("Municipal");
    }

    [Theory]
    [InlineData("ArrowRight", "Municipal")]
    [InlineData("ArrowDown", "Municipal")]
    [InlineData("ArrowLeft", "National")]
    [InlineData("ArrowUp", "National")]
    public void KeyDown_AnArrow_MovesToTheNeighbour(string key, string expected)
    {
        var cards = Render("State");

        cards.FindAll("[role=\"radio\"]")[1].KeyDown(key);

        _chosen.Should().Be(expected);
    }

    [Fact]
    public void KeyDown_AtTheEnd_WrapsAround()
    {
        var cards = Render("Municipal");

        cards.FindAll("[role=\"radio\"]")[2].KeyDown("ArrowRight");

        _chosen.Should().Be("National");
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("Enter")]
    public void KeyDown_SpaceOrEnter_ChoosesTheFocusedCard(string key)
    {
        var cards = Render();

        cards.FindAll("[role=\"radio\"]")[1].KeyDown(key);

        _chosen.Should().Be("State");
    }

    [Fact]
    public void Render_WithAnError_TheGroupSaysSoAndShowsTheMessage()
    {
        var cards = Render(error: "Choose a scope.");

        cards.Find("[role=\"radiogroup\"]").GetAttribute("aria-invalid").Should().Be("true");
        cards.Find(".app-field-error-text").TextContent.Should().Be("Choose a scope.");
    }
}
