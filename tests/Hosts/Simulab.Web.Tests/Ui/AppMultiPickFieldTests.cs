using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-75 Screen 4: the multi-pick field of the kit - a search over a short grouped list, several picks, chips with remove.
/// The page owns the picks, so these tests say what the field draws from what it is given and what it hands back.
/// </summary>
public sealed class AppMultiPickFieldTests : KitTestContext
{
    private readonly List<string> _toggled = [];
    private readonly List<string> _removed = [];
    private int _cleared;
    private int _retried;

    private static AppMultiPickOption Whole(string key, string name, bool picked = false, bool disabled = false, string? secondary = "whole subject") =>
        new(key, name, picked, disabled, secondary, IsGroupOption: true);

    private static AppMultiPickOption Member(string key, string name, bool picked = false, bool disabled = false, string? secondary = null) =>
        new(key, name, picked, disabled, secondary);

    private static IReadOnlyList<AppMultiPickGroup> Taxonomy() =>
    [
        new AppMultiPickGroup("g-mat", "Matemática", [Whole("s-mat", "Matemática"), Member("t-fra", "Frações"), Member("t-equ", "Equações")]),
        new AppMultiPickGroup("g-rl", "Raciocínio Lógico", [Whole("s-rl", "Raciocínio Lógico"), Member("t-pro", "Proposições")]),
        new AppMultiPickGroup("g-inf", "Informática", [Whole("s-inf", "Informática")])
    ];

    private static AppMultiPickItem PickedProposition() => new(
        "t-pro",
        "Raciocínio Lógico › Proposições",
        "Proposições, topic of Raciocínio Lógico",
        AppIcons.Topics,
        SpokenText: "Proposições, topic of Raciocínio Lógico");

    private IRenderedComponent<AppMultiPickField> RenderField(
        IReadOnlyList<AppMultiPickGroup>? groups = null,
        IReadOnlyList<AppMultiPickItem>? picked = null,
        Action<ComponentParameterCollectionBuilder<AppMultiPickField>>? more = null) =>
        Render<AppMultiPickField>(parameters =>
        {
            parameters
                .Add(p => p.Id, "pick")
                .Add(p => p.Label, "Covers")
                .Add(p => p.Placeholder, "Search a subject or a topic")
                .Add(p => p.OptionsLabel, "Taxonomy: subjects and topics")
                .Add(p => p.PickedLabel, "Picked items")
                .Add(p => p.CountText, $"Picked: {(picked?.Count ?? 0)} of 50")
                .Add(p => p.ClearText, "Clear the selection")
                .Add(p => p.NoneText, "Nothing picked: the subject stays not mapped.")
                .Add(p => p.RemoveLabelFor, item => $"Remove {item.Name}")
                .Add(p => p.Groups, groups ?? Taxonomy())
                .Add(p => p.Picked, picked ?? [])
                .Add(p => p.OnToggle, EventCallback.Factory.Create<string>(this, (Action<string>)(key => _toggled.Add(key))))
                .Add(p => p.OnRemove, EventCallback.Factory.Create<string>(this, (Action<string>)(key => _removed.Add(key))))
                .Add(p => p.OnClear, EventCallback.Factory.Create(this, () => _cleared++))
                .Add(p => p.OnRetry, EventCallback.Factory.Create(this, () => _retried++))
                .Add(p => p.Hint, "Optional. At most 50 items.");
            more?.Invoke(parameters);
        });

    private static void Open(IRenderedComponent<AppMultiPickField> field) => field.Find("#pick").Click();

    private static IReadOnlyList<string> OptionNames(IRenderedComponent<AppMultiPickField> field) =>
        [.. field.FindAll("[role='option'] .app-mp-option-name").Select(name => name.TextContent.Trim())];

    private List<string> FocusTargets() =>
        [.. JSInterop.Invocations
            .Where(invocation => invocation.Identifier == "simulabShell.focusOption")
            .Select(invocation => (string)invocation.Arguments[0]!)];

    private static void Press(AngleSharp.Dom.IElement element, string key) =>
        element.KeyDown(new KeyboardEventArgs { Key = key });

    [Fact]
    public void Render_Closed_ShowsTheLabelTheComboboxAndTheHintAndNoList()
    {
        var field = RenderField();

        field.Find("label.app-field-label").GetAttribute("for").Should().Be("pick");
        var input = field.Find("input#pick");
        input.GetAttribute("role").Should().Be("combobox");
        input.GetAttribute("aria-autocomplete").Should().Be("list");
        input.GetAttribute("aria-expanded").Should().Be("false");
        input.GetAttribute("aria-controls").Should().Be("pick-list");
        input.GetAttribute("aria-describedby").Should().Be("pick-description pick-count");
        input.GetAttribute("placeholder").Should().Be("Search a subject or a topic");
        input.GetAttribute("aria-invalid").Should().BeNull();
        field.Find("#pick-description").TextContent.Should().Contain("Optional. At most 50 items.");
        field.Find("#pick-count").TextContent.Should().Be("Picked: 0 of 50");
        field.FindAll("[role='listbox']").Should().BeEmpty("the list opens on a click, on typing or on Arrow down - not on focus");
    }

    [Fact]
    public void Open_ByClick_ShowsAMultiSelectableListboxWithLabelledGroupsAndTheWholeOptionFirst()
    {
        var field = RenderField();

        Open(field);

        field.Find("input#pick").GetAttribute("aria-expanded").Should().Be("true");
        var list = field.Find("[role='listbox']");
        list.Id.Should().Be("pick-list");
        list.GetAttribute("aria-multiselectable").Should().Be("true");
        list.GetAttribute("aria-label").Should().Be("Taxonomy: subjects and topics");
        field.FindAll("[role='group']").Select(group => group.GetAttribute("aria-label")).Should()
            .Equal("Matemática", "Raciocínio Lógico", "Informática");
        OptionNames(field).Should().Equal("Matemática", "Frações", "Equações", "Raciocínio Lógico", "Proposições", "Informática");
        field.FindAll("[role='group']")[0].QuerySelector("[role='option']")!.ClassList.Should().Contain("is-group");
        field.FindAll("[role='option']").Should().OnlyContain(option => option.GetAttribute("tabindex") == "-1");
    }

    [Fact]
    public void Open_ByTyping_ShowsTheList()
    {
        var field = RenderField();

        field.Find("input#pick").Input("mat");

        field.FindAll("[role='listbox']").Should().ContainSingle();
    }

    [Fact]
    public void Search_IgnoresCaseAndAccents_AndAGroupWhoseNameMatchesShowsAllItsOptions()
    {
        var field = RenderField();

        field.Find("input#pick").Input("MATEMATICA");

        OptionNames(field).Should().Equal("Matemática", "Frações", "Equações");
    }

    [Fact]
    public void Search_OtherwiseShowsOnlyTheMatchingTopicsWithTheGroupsOwnOptionForContext()
    {
        var field = RenderField();

        field.Find("input#pick").Input("proposicoes");

        OptionNames(field).Should().Equal("Raciocínio Lógico", "Proposições");
        field.FindAll("[role='group']").Should().ContainSingle();
    }

    [Fact]
    public void Search_NoMatch_SaysSoWithTheTermAndAnnouncesIt()
    {
        var field = RenderField();

        field.Find("input#pick").Input("xadrez");

        field.Find(".app-mp-empty").TextContent.Should().Be("Nothing found for \"xadrez\".");
        field.FindAll("[aria-live='polite']").Select(live => live.TextContent).Should().Contain("Nothing found for \"xadrez\".");
    }

    [Fact]
    public void Search_AnnouncesTheNumberOfResults()
    {
        var field = RenderField();

        field.Find("input#pick").Input("proposicoes");

        field.FindAll("[aria-live='polite']").Select(live => live.TextContent).Should().Contain("Results: 2");
    }

    [Fact]
    public void Options_ShowTheirStateAndTheDisabledReasonIsTheirAccessibleDescription()
    {
        var groups = new List<AppMultiPickGroup>
        {
            new("g-mat", "Matemática",
            [
                Whole("s-mat", "Matemática", picked: true),
                Member("t-fra", "Frações", disabled: true, secondary: "Already covered by the whole subject"),
                Member("t-equ", "Equações")
            ])
        };
        var field = RenderField(groups);

        Open(field);

        var picked = field.Find("#pick-option-s-mat");
        picked.GetAttribute("aria-selected").Should().Be("true");
        picked.GetAttribute("aria-disabled").Should().BeNull();
        var disabled = field.Find("#pick-option-t-fra");
        disabled.GetAttribute("aria-selected").Should().Be("false");
        disabled.GetAttribute("aria-disabled").Should().Be("true");
        var reasonId = disabled.GetAttribute("aria-describedby");
        field.Find($"#{reasonId}").TextContent.Should().Be("Already covered by the whole subject");
        field.Find("#pick-option-t-equ").GetAttribute("aria-disabled").Should().BeNull();
    }

    [Fact]
    public void Click_OnAnOption_HandsItsKeyBackAndKeepsTheListOpen()
    {
        var field = RenderField();
        Open(field);

        field.Find("#pick-option-t-fra").Click();

        _toggled.Should().Equal("t-fra");
        field.FindAll("[role='listbox']").Should().ContainSingle("several can be picked in a row");
    }

    [Fact]
    public void Click_OnADisabledOption_DoesNothing()
    {
        var groups = new List<AppMultiPickGroup>
        {
            new("g-mat", "Matemática", [Whole("s-mat", "Matemática", picked: true), Member("t-fra", "Frações", disabled: true)])
        };
        var field = RenderField(groups);
        Open(field);

        field.Find("#pick-option-t-fra").Click();
        Press(field.Find("#pick-option-t-fra"), " ");

        _toggled.Should().BeEmpty();
    }

    [Fact]
    public void Keyboard_ArrowDownOnTheInput_OpensTheListAndEntersIt()
    {
        var field = RenderField();

        Press(field.Find("input#pick"), "ArrowDown");

        field.FindAll("[role='listbox']").Should().ContainSingle();
        field.WaitForAssertion(() => FocusTargets().Should().Contain("pick-option-s-mat"));
    }

    [Fact]
    public void Keyboard_ArrowKeysHomeAndEnd_MoveThroughEveryOptionIncludingTheDisabledOnes()
    {
        var groups = new List<AppMultiPickGroup>
        {
            new("g-mat", "Matemática",
            [
                Whole("s-mat", "Matemática", picked: true),
                Member("t-fra", "Frações", disabled: true, secondary: "Already covered by the whole subject"),
                Member("t-equ", "Equações")
            ])
        };
        var field = RenderField(groups);
        Open(field);

        Press(field.Find("#pick-option-s-mat"), "ArrowDown");
        field.WaitForAssertion(() => FocusTargets().Should().Contain("pick-option-t-fra"));

        Press(field.Find("#pick-option-t-fra"), "End");
        field.WaitForAssertion(() => FocusTargets().Should().Contain("pick-option-t-equ"));

        Press(field.Find("#pick-option-t-equ"), "Home");
        field.WaitForAssertion(() => FocusTargets().Count(target => target == "pick-option-s-mat").Should().BeGreaterThan(0));

        Press(field.Find("#pick-option-s-mat"), "ArrowUp");
        field.WaitForAssertion(() => FocusTargets().Should().Contain("pick")); // past the first option the focus returns to the input
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("Enter")]
    public void Keyboard_SpaceOrEnterOnAnOption_TogglesItAndKeepsTheListOpen(string key)
    {
        var field = RenderField();
        Open(field);

        Press(field.Find("#pick-option-t-pro"), key);

        _toggled.Should().Equal("t-pro");
        field.FindAll("[role='listbox']").Should().ContainSingle();
    }

    [Fact]
    public void Keyboard_EscapeInTheList_ClosesItAndReturnsTheFocusToTheInput()
    {
        var field = RenderField();
        Open(field);

        Press(field.Find("#pick-option-t-pro"), "Escape");

        field.FindAll("[role='listbox']").Should().BeEmpty();
        field.WaitForAssertion(() => FocusTargets().Should().Contain("pick"));
    }

    [Fact]
    public void Keyboard_TabInTheList_ClosesIt()
    {
        var field = RenderField();
        Open(field);

        Press(field.Find("#pick-option-t-pro"), "Tab");

        field.FindAll("[role='listbox']").Should().BeEmpty();
    }

    [Fact]
    public void Root_WhileTheListIsOpen_KeepsEscapeFromReachingTheDialogAroundIt()
    {
        var field = RenderField();
        Open(field);

        // The root stops the key only while the list is open, like AppSuggestField: a second Esc is the dialog's.
        field.Markup.Should().Contain("data-app-multipick");
        Press(field.Find("input#pick"), "Escape");
        field.FindAll("[role='listbox']").Should().BeEmpty();
    }

    [Fact]
    public void Picked_AreRemovableChipsWithTheirOwnLabelledList()
    {
        var field = RenderField(picked: [PickedProposition()]);

        field.Find("ul.app-mp-chips").GetAttribute("aria-label").Should().Be("Picked items");
        field.FindAll("ul.app-mp-chips > li").Should().ContainSingle();
        var remove = field.Find("#pick-remove-t-pro");
        remove.GetAttribute("aria-label").Should().Be("Remove Proposições, topic of Raciocínio Lógico");
        field.FindAll(".app-mp-none").Should().BeEmpty();

        remove.Click();

        _removed.Should().Equal("t-pro");
    }

    [Fact]
    public void Picked_None_SaysSoAndOffersNoClearButton()
    {
        var field = RenderField();

        field.Find(".app-mp-none").TextContent.Should().Be("Nothing picked: the subject stays not mapped.");
        field.FindAll("ul.app-mp-chips").Should().BeEmpty();
        field.FindAll(".app-mp-clear").Should().BeEmpty();
    }

    [Fact]
    public void Clear_IsShownOnlyWithPicksAndCallsBack()
    {
        var field = RenderField(picked: [PickedProposition()]);

        field.Find("button.app-mp-clear").TextContent.Trim().Should().Be("Clear the selection");
        field.Find("button.app-mp-clear").Click();

        _cleared.Should().Be(1);
    }

    [Fact]
    public void Disabled_TheInputTheClearAndEveryRemoveButtonAreDisabled()
    {
        var field = RenderField(picked: [PickedProposition()], more: parameters => parameters.Add(p => p.Disabled, true));

        field.Find("input#pick").HasAttribute("disabled").Should().BeTrue();
        field.Find("button.app-mp-clear").HasAttribute("disabled").Should().BeTrue();
        field.Find("#pick-remove-t-pro").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void State_Loading_DisablesTheInputAndSaysSoWithAProgressIndicator()
    {
        var field = RenderField(more: parameters => parameters
            .Add(p => p.State, AppMultiPickState.Loading)
            .Add(p => p.LoadingText, "Loading the taxonomy..."));

        field.Find("input#pick").HasAttribute("disabled").Should().BeTrue();
        field.Find("#pick-description").TextContent.Should().Contain("Loading the taxonomy...");
        field.FindAll("#pick-description [role='progressbar']").Should().ContainSingle();
        field.Find("#pick-description").TextContent.Should().NotContain("Optional. At most 50 items.");
        field.Find("input#pick").Click();
        field.FindAll("[role='listbox']").Should().BeEmpty();
    }

    [Fact]
    public void State_LoadFailed_SaysSoInErrorStyleOffersTryAgainAndKeepsThePicks()
    {
        var field = RenderField(
            picked: [PickedProposition()],
            more: parameters => parameters
                .Add(p => p.State, AppMultiPickState.LoadFailed)
                .Add(p => p.LoadFailedText, "The taxonomy could not be loaded."));

        field.Find("input#pick").HasAttribute("disabled").Should().BeTrue();
        field.Find("#pick-description .app-field-error-text").TextContent.Should().Contain("The taxonomy could not be loaded.");
        field.FindAll("#pick-remove-t-pro").Should().ContainSingle("the picked chips stay removable");
        field.Find("#pick-remove-t-pro").HasAttribute("disabled").Should().BeFalse();
        field.FindAll("[aria-live='polite']").Select(live => live.TextContent).Should().Contain("The taxonomy could not be loaded.");

        field.Find("#pick-description button").Click();

        _retried.Should().Be(1);
    }

    [Fact]
    public void State_Empty_DisablesTheInputAndSaysSo()
    {
        var field = RenderField(
            groups: [],
            more: parameters => parameters
                .Add(p => p.State, AppMultiPickState.Empty)
                .Add(p => p.EmptyText, "The taxonomy has no subject yet."));

        field.Find("input#pick").HasAttribute("disabled").Should().BeTrue();
        field.Find("#pick-description").TextContent.Should().Contain("The taxonomy has no subject yet.");
    }

    [Fact]
    public void Error_ReplacesTheHintMarksTheFieldInvalidAndStaysVisibleWithTheListClosed()
    {
        var field = RenderField(more: parameters => parameters.Add(p => p.Error, "A topic cannot be picked together with its whole subject."));

        field.Find("input#pick").GetAttribute("aria-invalid").Should().Be("true");
        field.Find("#pick-description .app-field-error-text").TextContent.Should().Contain("A topic cannot be picked");
        field.Find("#pick-description").TextContent.Should().NotContain("Optional. At most 50 items.");
        field.Find(".app-field").ClassList.Should().Contain("app-field-error");
        field.FindAll("[role='listbox']").Should().BeEmpty("focus alone never opens the list, so the error under the input stays visible");
    }

    [Fact]
    public void Announcement_IsTheTextThePageGave()
    {
        var field = RenderField(more: parameters => parameters.Add(p => p.Announcement, "Picked: Proposições."));

        field.FindAll("[aria-live='polite']").Select(live => live.TextContent).Should().Contain("Picked: Proposições.");
    }
}
