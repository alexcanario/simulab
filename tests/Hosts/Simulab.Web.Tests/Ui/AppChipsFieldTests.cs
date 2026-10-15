using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-77: the chips field of the kit - free text typed one name at a time and kept as removable chips. The page owns
/// the list, so these tests say what the field draws from what it is given and what it hands back.
/// </summary>
public sealed class AppChipsFieldTests : KitTestContext
{
    private readonly List<IReadOnlyList<string>> _changes = [];

    private IRenderedComponent<AppChipsField> RenderField(
        IReadOnlyList<string>? items = null,
        string? error = null,
        int minLength = 2) =>
        Render<AppChipsField>(parameters => parameters
            .Add(p => p.Id, "aliases")
            .Add(p => p.Label, "Other names")
            .Add(p => p.Placeholder, "Type a name and press Enter")
            .Add(p => p.Hint, "Names organizers use.")
            .Add(p => p.Items, items ?? [])
            .Add(p => p.MaxLength, 150)
            .Add(p => p.MinLength, minLength)
            .Add(p => p.TooShortText, "Each other name needs at least 2 characters.")
            .Add(p => p.Error, error)
            .Add(p => p.ItemsChanged, EventCallback.Factory.Create<IReadOnlyList<string>>(
                this, (Action<IReadOnlyList<string>>)(list => _changes.Add(list)))));

    private static IReadOnlyList<string> ChipTexts(IRenderedComponent<AppChipsField> field) =>
        [.. field.FindAll(".app-chip-text").Select(text => text.TextContent.Trim())];

    [Fact]
    public void Render_ShowsTheLabelTheHintAndOneChipPerName()
    {
        var field = RenderField(["Português", "Língua"]);

        field.Find("label").TextContent.Trim().Should().Be("Other names");
        field.Find("label").GetAttribute("for").Should().Be("aliases");
        field.Find("#aliases").GetAttribute("maxlength").Should().Be("150");
        field.Find(".app-field-hint").TextContent.Should().Be("Names organizers use.");
        ChipTexts(field).Should().Equal("Português", "Língua");
        field.Find("ul").GetAttribute("aria-label").Should().Be("Other names");
    }

    // AC2 (screen side): Enter turns the typed text into a chip, trimmed.
    [Fact]
    public void Enter_AddsTheTypedNameTrimmed()
    {
        var field = RenderField(["Português"]);

        field.Find("#aliases").Input("  Portugues  ");
        field.Find("#aliases").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        _changes.Should().ContainSingle().Which.Should().Equal("Português", "Portugues");
    }

    // A name typed and left in the input is kept when the input loses focus (the change event), so Save never drops it.
    [Fact]
    public void LeavingTheInput_AddsTheTypedName()
    {
        var field = RenderField();

        field.Find("#aliases").Change("Crase");

        _changes.Should().ContainSingle().Which.Should().Equal("Crase");
    }

    [Fact]
    public void SameNameAgainIgnoringCase_IsNotAddedTwice()
    {
        var field = RenderField(["Português"]);

        field.Find("#aliases").Input("português");
        field.Find("#aliases").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        _changes.Should().BeEmpty();
    }

    [Fact]
    public void ANameUnderTheMinimum_IsNotAdded_AndTheFieldSaysWhy()
    {
        var field = RenderField();

        field.Find("#aliases").Input("a");
        field.Find("#aliases").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        _changes.Should().BeEmpty();
        field.Find(".app-field-error-text").TextContent.Should().Be("Each other name needs at least 2 characters.");
        field.Find("#aliases").GetAttribute("aria-invalid").Should().Be("true");
    }

    [Fact]
    public void BlankText_IsIgnored()
    {
        var field = RenderField();

        field.Find("#aliases").Input("   ");
        field.Find("#aliases").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        _changes.Should().BeEmpty();
        field.FindAll(".app-field-error-text").Should().BeEmpty();
    }

    // The remove button of each chip says which name it removes and hands back the list without it.
    [Fact]
    public void Remove_HandsBackTheListWithoutThatName_AndTheButtonNamesIt()
    {
        var field = RenderField(["Português", "Língua"]);

        var button = field.Find("button[aria-label='Remove Língua']");
        button.Click();

        _changes.Should().ContainSingle().Which.Should().Equal("Português");
    }

    [Fact]
    public void Error_ReplacesTheHintAndMarksTheInput()
    {
        var field = RenderField(error: "Another subject already uses one of these names.");

        field.Find(".app-field-error-text").TextContent.Should().Be("Another subject already uses one of these names.");
        field.FindAll(".app-field-hint").Should().BeEmpty();
        field.Find("#aliases").GetAttribute("aria-invalid").Should().Be("true");
        field.Find("#aliases").GetAttribute("aria-describedby").Should().Be("aliases-description");
    }

    // rule ui, long text: a name longer than the field sits in the kit element that wraps, with no fixed width.
    [Fact]
    public void ALongName_SitsInTheWrappingChipText_WithNoFixedWidthOrNowrap()
    {
        var longName = string.Join(' ', Enumerable.Repeat("Interpretação", 20));
        var field = RenderField([longName]);

        var text = field.Find(".app-chip-text");

        text.TextContent.Trim().Should().Be(longName);
        field.Markup.Should().NotContain("nowrap").And.NotContain("width:");
    }
}
