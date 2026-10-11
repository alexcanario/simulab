using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-114: the picker of a list the page holds whole looks like what it does. An arrow at the end says "a list
/// opens before you type"; the list opens on focus, narrows in memory and keeps the "all" choice where it was.
/// </summary>
public sealed class AppSelectFieldTests : KitTestContext
{
    private const string ExpandLabel = "Show the options";
    private const string CollapseLabel = "Hide the options";

    private static readonly AppSelectOption<string?>[] Areas =
    [
        new(null, "All"),
        new("math", "Matemática"),
        new("pt", "Português"),
        new("history", "História")
    ];

    private IRenderedComponent<MudPopoverProvider> _provider = default!;

    private IRenderedComponent<AppSelectField<string?>> Render(
        string? value = null,
        string? error = null,
        string? hint = null,
        bool disabled = false,
        bool loading = false,
        string? leadingIcon = null,
        List<string?>? changes = null)
    {
        _provider = Render<MudPopoverProvider>();

        return Render<AppSelectField<string?>>(parameters =>
        {
            parameters
                .Add(select => select.Id, "area")
                .Add(select => select.Label, "Area")
                .Add(select => select.Options, Areas)
                .Add(select => select.Value, value)
                .Add(select => select.Error, error)
                .Add(select => select.Hint, hint)
                .Add(select => select.Disabled, disabled)
                .Add(select => select.Loading, loading)
                .Add(select => select.LeadingIcon, leadingIcon);

            if (changes is not null)
            {
                parameters.Add(
                    select => select.ValueChanged,
                    EventCallback.Factory.Create<string?>(this, (string? picked) => changes.Add(picked)));
            }
        });
    }

    private IReadOnlyList<string> OpenOptionTexts() =>
        _provider.FindAll(".mud-popover-open [role='option']").Select(option => option.TextContent.Trim()).ToList();

    // AC1: an arrow at the end and an input that takes typing (it is not read-only).
    [Fact]
    public void Render_ShowsTheExpandArrowAndAnInputThatAcceptsTyping()
    {
        var field = Render();

        field.Find($"button[aria-label='{ExpandLabel}']").Should().NotBeNull();
        var input = field.Find("#area");
        input.HasAttribute("readonly").Should().BeFalse("the reader types to narrow the list");
        input.GetAttribute("role").Should().Be("combobox");
        input.GetAttribute("aria-expanded").Should().Be("false");
        input.GetAttribute("aria-haspopup").Should().Be("listbox");
    }

    // AC2: focusing the field opens the whole list before any typing.
    [Fact]
    public void Focus_OpensTheWholeList()
    {
        var field = Render();

        field.Find("#area").Focus();

        field.WaitForAssertion(() =>
            OpenOptionTexts().Should().Equal("All", "Matemática", "Português", "História"));
        field.Find("#area").GetAttribute("aria-expanded").Should().Be("true");
        field.Find($"button[aria-label='{CollapseLabel}']").Should().NotBeNull("the arrow says the list can be closed");
    }

    // AC3: typing narrows the list in memory, ignoring case and accents.
    [Fact]
    public void Type_NarrowsTheListIgnoringCaseAndAccents()
    {
        var field = Render();
        field.Find("#area").Focus();

        field.Find("#area").Input("MATEMATICA");
        AdvanceDebounce();

        field.WaitForAssertion(() => OpenOptionTexts().Should().Equal("Matemática"));
    }

    // AC3: a text that matches nothing says so, with the term.
    [Fact]
    public void Type_WithNoMatch_ShowsTheNoResultsMessage()
    {
        var field = Render();
        field.Find("#area").Focus();

        field.Find("#area").Input("zzz");
        AdvanceDebounce();

        field.WaitForAssertion(() => _provider.Markup.Should().Contain("No option matches \"zzz\"."));
    }

    // AC4: the field already shows "All"; focusing it lists everything, not only "All".
    [Fact]
    public void Focus_WhileShowingTheAllChoice_ListsEveryOption()
    {
        var field = Render(value: null);

        field.Find("#area").GetAttribute("value").Should().Be("All");
        field.Find("#area").Focus();

        field.WaitForAssertion(() => OpenOptionTexts().Should().HaveCount(4));
    }

    // AC5: one pick, one ValueChanged with that option's value.
    [Fact]
    public void Pick_AnOption_RaisesValueChangedOnceWithItsValue()
    {
        var changes = new List<string?>();
        var field = Render(changes: changes);
        field.Find("#area").Focus();
        field.WaitForAssertion(() => OpenOptionTexts().Should().HaveCount(4));

        _provider.FindAll(".mud-popover-open [role='option']")[2].Click();

        field.WaitForAssertion(() => changes.Should().Equal("pt"));
    }

    // AC5: picking "all" gives the null value, as the filters of the pages expect today.
    [Fact]
    public void Pick_TheAllChoice_GivesNull()
    {
        var changes = new List<string?>();
        var field = Render(value: "math", changes: changes);
        field.Find("#area").Focus();
        field.WaitForAssertion(() => OpenOptionTexts().Should().HaveCount(4));

        _provider.FindAll(".mud-popover-open [role='option']")[0].Click();

        field.WaitForAssertion(() => changes.Should().Equal([null]));
    }

    // AC6: the arrow is there with a leading icon too (both drawn, or the arrow alone: BR5).
    [Fact]
    public void Render_WithALeadingIcon_StillShowsTheArrow()
    {
        var field = Render(leadingIcon: AppIcons.Scope);

        field.Find($"button[aria-label='{ExpandLabel}']").Should().NotBeNull();
        field.Find("#area").HasAttribute("readonly").Should().BeFalse();
    }

    // AC8: disabled mutes the field and keeps the hint.
    [Fact]
    public void Render_Disabled_DisablesTheInputAndKeepsTheHint()
    {
        var field = Render(disabled: true, hint: "Groups the subject with similar ones.");

        field.Find("#area").HasAttribute("disabled").Should().BeTrue();
        field.Find($"button[aria-label='{ExpandLabel}']").HasAttribute("disabled").Should().BeTrue("the arrow is muted");
        field.Find(".app-field-description").TextContent.Should().Contain("Groups the subject with similar ones.");
    }

    // AC8: an error replaces the hint, marks the input and keeps the arrow.
    [Fact]
    public void Render_WithAnError_ShowsItInsteadOfTheHintAndKeepsTheArrow()
    {
        var field = Render(error: "Choose an area.", hint: "Groups the subject with similar ones.");

        field.Find(".app-field-error-text").TextContent.Should().Contain("Choose an area.");
        field.Find(".app-field-description").TextContent.Should().NotContain("Groups the subject");
        field.Find("#area").GetAttribute("aria-invalid").Should().Be("true");
        field.Find($"button[aria-label='{ExpandLabel}']").Should().NotBeNull();
    }

    // AC10: the keyboard alone opens, closes and picks; Escape closes the list and stops there.
    [Fact]
    public void Keyboard_ArrowDownOpens_EscapeCloses_EnterPicks()
    {
        var changes = new List<string?>();
        var field = Render(changes: changes);
        var input = field.Find("#area");

        input.Focus();
        field.WaitForAssertion(() => field.Find("#area").GetAttribute("aria-expanded").Should().Be("true"));

        input.KeyDown(new KeyboardEventArgs { Key = "Escape" });
        field.WaitForAssertion(() => field.Find("#area").GetAttribute("aria-expanded").Should().Be("false"));

        input.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        field.WaitForAssertion(() => field.Find("#area").GetAttribute("aria-expanded").Should().Be("true"));

        input.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        input.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        field.WaitForAssertion(() => changes.Should().HaveCount(1));
    }

    // AC14: while the page still fetches the options, a progress takes the arrow's place.
    [Fact]
    public void Render_Loading_ShowsAProgressInsteadOfTheArrow()
    {
        var field = Render(loading: true);

        field.FindAll(".mud-progress-circular").Should().NotBeEmpty();
        field.FindAll($"button[aria-label='{ExpandLabel}']").Should().BeEmpty();

        field.Find("#area").Focus();

        field.WaitForAssertion(() => _provider.Markup.Should().Contain("Loading the options..."));
    }

    // AC14: the default changes nothing for the 18 call sites.
    [Fact]
    public void Render_ByDefault_HasNoProgress()
    {
        var field = Render();

        field.FindAll(".mud-progress-circular").Should().BeEmpty();
        field.Find($"button[aria-label='{ExpandLabel}']").Should().NotBeNull();
    }
}
