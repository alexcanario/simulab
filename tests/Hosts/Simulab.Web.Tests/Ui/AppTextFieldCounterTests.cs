using Bunit;
using Microsoft.AspNetCore.Components;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-105 AC18, BR18: the optional counter of <see cref="AppTextField{TValue}"/>. "{n}/{max}" follows the typing,
/// sits in the description the input points at beside the hint or the error text, is text and never colour, and is
/// announced politely. Without the parameter the field renders as it always did.
/// </summary>
public sealed class AppTextFieldCounterTests : KitTestContext
{
    private IRenderedComponent<AppTextField<string>> Render(
        string? value,
        bool counter,
        string? error = null,
        string? hint = null,
        EventCallback<string?> changed = default) =>
        Render<AppTextField<string>>(parameters => parameters
            .Add(field => field.Id, "reason")
            .Add(field => field.Label, "Reason")
            .Add(field => field.MaxLength, 500)
            .Add(field => field.Counter, counter)
            .Add(field => field.Value, value)
            .Add(field => field.Error, error)
            .Add(field => field.Hint, hint)
            .Add(field => field.ValueChanged, changed));

    [Fact]
    public void Counter_Off_RendersNoCounterAndNoLiveRegion()
    {
        var field = Render("abc", counter: false, hint: "Plain text.");

        field.FindAll(".app-field-counter").Should().BeEmpty();
        field.FindAll("[role='status']").Should().BeEmpty();
        field.Find("#reason-description").ClassList.Should().NotContain("app-field-description-counter");
    }

    // AC18: "0/500" empty, "12/500" with twelve characters; the count is text.
    [Theory]
    [InlineData("", "0/500")]
    [InlineData("abcdefghijkl", "12/500")]
    public void Counter_On_ShowsTheLengthOverTheMaximum_AsText(string value, string expected)
    {
        var field = Render(value, counter: true, hint: "Required. Plain text, up to 500 characters.");

        field.Find(".app-field-counter").TextContent.Trim().Should().Be(expected);
    }

    // AC18: the count is part of the description the input points at, so it is read with the label on focus.
    [Fact]
    public void Counter_On_IsInsideTheDescriptionTheInputPointsAt()
    {
        var field = Render("abc", counter: true, hint: "Required.");

        var description = field.Find("#reason-description");
        description.QuerySelector(".app-field-counter").Should().NotBeNull();
        description.QuerySelector(".app-field-hint")!.TextContent.Should().Be("Required.");
        field.Find("#reason").GetAttribute("aria-describedby").Should().Be("reason-description");
    }

    // AC18: the counter stays visible beside an error text and never replaces it.
    [Fact]
    public void Counter_On_StaysBesideTheErrorText()
    {
        var field = Render(new string('a', 512), counter: true, error: "The reason or note has more than 500 characters.");

        var description = field.Find("#reason-description");
        description.QuerySelector(".app-field-error-text")!.TextContent.Should().Contain("more than 500");
        description.QuerySelector(".app-field-counter")!.TextContent.Trim().Should().Be("512/500");
    }

    // AC18: a polite live region says it in words, so a screen reader is never interrupted while typing.
    [Fact]
    public void Counter_On_HasAPoliteLiveRegionWithTheSpokenCount()
    {
        var field = Render("abcdefghijkl", counter: true);

        var region = field.Find("[role='status']");
        region.GetAttribute("aria-live").Should().Be("polite");
        region.TextContent.Trim().Should().Be("12 of 500 characters");
    }

    // AC18: the count follows the typing, not the blur: the value reaches the page on input.
    [Fact]
    public void Counter_On_FollowsTheTyping_WithoutWaitingForTheBlur()
    {
        string? received = null;
        var field = Render(string.Empty, counter: true, changed: EventCallback.Factory.Create<string?>(this, value => received = value));

        field.Find("input,textarea").Input("abc");
        // The host's clock is fake, which freezes the field's debounce (profile: test strategy): move it past.
        AdvanceDebounce(500);

        field.WaitForAssertion(() =>
        {
            received.Should().Be("abc");
            field.Find(".app-field-counter").TextContent.Trim().Should().Be("3/500");
        });
    }
}
