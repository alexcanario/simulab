using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-169: the kit's answer area. A radio group (one, or true or false) and a group of checkboxes; marking is a click or
/// Space or Enter, the arrow keys only move focus, and a busy group ignores clicks. It is controlled: the page hands the
/// selection in.
/// </summary>
public sealed class AppAnswerOptionsTests : KitTestContext
{
    private static IReadOnlyList<AppAnswerOption> Options(int count) =>
        [.. Enumerable.Range(0, count).Select(position => new AppAnswerOption(position, ((char)('A' + position)).ToString(), Text: $"Option {position + 1}"))];

    private readonly List<IReadOnlyList<int>> _changes = [];

    private IRenderedComponent<AppAnswerOptions> RenderOptions(
        AppAnswerMode mode,
        IReadOnlyList<AppAnswerOption> options,
        IReadOnlyList<int>? selected = null,
        bool busy = false) =>
        Render<AppAnswerOptions>(parameters => parameters
            .Add(group => group.Id, "answer")
            .Add(group => group.Mode, mode)
            .Add(group => group.Options, options)
            .Add(group => group.Selected, selected ?? Array.Empty<int>())
            .Add(group => group.LabelledBy, "answer-title")
            .Add(group => group.DescribedBy, "answer-hint")
            .Add(group => group.Busy, busy)
            .Add(group => group.OnChange, (IReadOnlyList<int> next) => _changes.Add(next)));

    [Fact]
    public void Single_IsARadioGroupNamedByItsHeading_WithOneRadioPerOption()
    {
        var group = RenderOptions(AppAnswerMode.SingleChoice, Options(4), [1]);

        var root = group.Find("#answer");
        root.GetAttribute("role").Should().Be("radiogroup");
        root.GetAttribute("aria-labelledby").Should().Be("answer-title");
        root.GetAttribute("aria-describedby").Should().Be("answer-hint");
        var rows = group.FindAll("[role=radio]");
        rows.Select(row => row.GetAttribute("aria-checked")).Should().Equal("false", "true", "false", "false");
        rows[1].ClassList.Should().Contain("app-answer-option-selected");
        rows.Select(row => row.QuerySelector(".app-answer-letter")!.TextContent).Should().Equal("A", "B", "C", "D");
        rows[0].Id.Should().Be("answer-option-0");
    }

    // One tab stop: the marked row, else the first.
    [Fact]
    public void Single_HasOneTabStop_TheMarkedRowElseTheFirst()
    {
        var blank = RenderOptions(AppAnswerMode.SingleChoice, Options(3));
        blank.FindAll("[role=radio]").Select(row => row.GetAttribute("tabindex")).Should().Equal("0", "-1", "-1");

        var marked = RenderOptions(AppAnswerMode.SingleChoice, Options(3), [2]);
        marked.FindAll("[role=radio]").Select(row => row.GetAttribute("tabindex")).Should().Equal("-1", "-1", "0");
    }

    [Fact]
    public void Single_AClickOnAnUnmarkedRowSelectsIt_AndAClickOnTheMarkedOneClearsIt()
    {
        var group = RenderOptions(AppAnswerMode.SingleChoice, Options(3), [1]);

        group.Find("#answer-option-2").Click();
        group.Find("#answer-option-1").Click();

        _changes.Should().HaveCount(2);
        _changes[0].Should().Equal(2);
        _changes[1].Should().BeEmpty();
    }

    [Fact]
    public void TrueFalse_IsAlsoARadioGroup_WithTheTextsItIsGiven()
    {
        var rows = new[]
        {
            new AppAnswerOption(0, null, Text: "True"),
            new AppAnswerOption(1, null, Text: "False")
        };

        var group = RenderOptions(AppAnswerMode.TrueFalse, rows, [0]);

        group.Find("#answer").GetAttribute("role").Should().Be("radiogroup");
        group.FindAll("[role=radio]").Select(row => row.TextContent.Trim()).Should().Equal("True", "False");
        group.FindAll(".app-answer-letter").Should().BeEmpty("a true or false row has no letter");
    }

    // AC7: a checkbox group; each click toggles one option and hands back the whole set.
    [Fact]
    public void Multiple_IsAGroupOfCheckboxes_AndEachClickHandsBackTheWholeSet()
    {
        var group = RenderOptions(AppAnswerMode.Multiple, Options(4), [0, 2]);

        group.Find("#answer").GetAttribute("role").Should().Be("group");
        group.FindAll("[role=checkbox]").Select(row => row.GetAttribute("aria-checked")).Should().Equal("true", "false", "true", "false");
        group.FindAll("[role=checkbox]").Select(row => row.GetAttribute("tabindex")).Should().OnlyContain(value => value == "0", "every checkbox is a tab stop");
        group.FindAll(".app-answer-indicator-box .mud-icon-root").Should().HaveCount(2, "a ticked box shows its check");

        group.Find("#answer-option-1").Click();
        group.Find("#answer-option-0").Click();

        _changes[0].Should().Equal(0, 1, 2);
        _changes[1].Should().Equal(2);
    }

    // The group is busy while a save is in flight: it says so and ignores clicks.
    [Fact]
    public void Busy_MarksTheGroup_AndIgnoresClicks()
    {
        var group = RenderOptions(AppAnswerMode.SingleChoice, Options(3), busy: true);

        group.Find("#answer").GetAttribute("aria-busy").Should().Be("true");
        group.Find("#answer-option-1").Click();

        _changes.Should().BeEmpty();
        RenderOptions(AppAnswerMode.SingleChoice, Options(3)).FindAll("[aria-busy]").Should().BeEmpty();
    }

    // The arrow keys move focus between the rows, wrapping at the ends, and mark nothing.
    [Theory]
    [InlineData(0, "ArrowDown", 1)]
    [InlineData(0, "ArrowRight", 1)]
    [InlineData(2, "ArrowDown", 0)]
    [InlineData(0, "ArrowUp", 2)]
    [InlineData(1, "ArrowLeft", 0)]
    public void ArrowKeys_MoveTheFocus_WrappingAtTheEnds_AndMarkNothing(int from, string key, int to)
    {
        var group = RenderOptions(AppAnswerMode.SingleChoice, Options(3));

        group.Find($"#answer-option-{from}").KeyDown(new KeyboardEventArgs { Key = key });

        JSInterop.Invocations["simulabShell.focusOption"].Should().ContainSingle()
            .Which.Arguments.Should().Equal($"answer-option-{to}");
        _changes.Should().BeEmpty();
    }

    [Fact]
    public void OtherKeys_DoNothing()
    {
        var group = RenderOptions(AppAnswerMode.SingleChoice, Options(3));

        group.Find("#answer-option-0").KeyDown(new KeyboardEventArgs { Key = "a" });

        _changes.Should().BeEmpty();
    }

    // The question's own option text is HTML in the content language; the app's words are plain text.
    [Fact]
    public void HtmlOptions_AreRenderedAsMarkup_InTheContentLanguage()
    {
        var group = Render<AppAnswerOptions>(parameters => parameters
            .Add(g => g.Id, "answer")
            .Add(g => g.Mode, AppAnswerMode.SingleChoice)
            .Add(g => g.Options, new[] { new AppAnswerOption(0, "A", Html: "<p>Texto <strong>forte</strong></p>") })
            .Add(g => g.Selected, Array.Empty<int>())
            .Add(g => g.LabelledBy, "answer-title")
            .Add(g => g.Lang, "pt-BR"));

        var text = group.Find(".app-answer-text");
        text.GetAttribute("lang").Should().Be("pt-BR");
        text.QuerySelector("strong")!.TextContent.Should().Be("forte");
    }

    // Long text: an option wider than its row wraps inside the row's text element, which has no fixed width.
    [Fact]
    public void LongText_SitsInTheElementThatWraps()
    {
        var longText = string.Join(' ', Enumerable.Repeat("pneumonoultramicroscopicsilicovolcanoconiosis", 20));

        var group = RenderOptions(AppAnswerMode.SingleChoice, [new AppAnswerOption(0, "A", Text: longText)]);

        var text = group.Find(".app-answer-text");
        text.TextContent.Trim().Should().Be(longText);
        text.HasAttribute("style").Should().BeFalse();
        text.ClassList.Should().Contain("app-question-content", "the content class carries overflow-wrap: anywhere");
    }
}
