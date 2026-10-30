using Bunit;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>F-184 AC1, BR1: a switch changes a setting at once.</summary>
public sealed class AppSwitchTests : KitTestContext
{
    private bool? _changed;

    private IRenderedComponent<AppSwitch> RenderSwitch(bool value = false, bool disabled = false) =>
        Render<AppSwitch>(parameters => parameters
            .Add(component => component.Id, "notify")
            .Add(component => component.Label, "Send me reminders")
            .Add(component => component.Value, value)
            .Add(component => component.Disabled, disabled)
            .Add(component => component.ValueChanged, (bool changed) => _changed = changed));

    [Fact]
    public void Switch_IsARoleSwitchWithItsState()
    {
        var control = RenderSwitch(value: true);

        var button = control.Find("#notify");
        button.GetAttribute("role").Should().Be("switch");
        button.GetAttribute("aria-checked").Should().Be("true");
        control.Find(".app-switch-field").TextContent.Should().Contain("Send me reminders");
    }

    [Fact]
    public void Click_ChangesTheValueAtOnce()
    {
        var control = RenderSwitch();

        control.Find("#notify").Click();

        _changed.Should().BeTrue();
    }

    [Fact]
    public void Disabled_DoesNotChange()
    {
        var control = RenderSwitch(disabled: true);

        control.Find("#notify").HasAttribute("disabled").Should().BeTrue();
    }
}

/// <summary>F-184 AC2, BR2: a segmented choice is one radio group with one tab stop.</summary>
public sealed class AppSegmentedFieldTests : KitTestContext
{
    private string? _changed;

    private static readonly IReadOnlyList<AppSegmentOption<string>> Options =
        [new("a", "Day"), new("b", "Week"), new("c", "Month")];

    private IRenderedComponent<AppSegmentedField<string>> RenderField(string? value = null, string? error = null) =>
        Render<AppSegmentedField<string>>(parameters => parameters
            .Add(component => component.Id, "period")
            .Add(component => component.Label, "Period")
            .Add(component => component.Options, Options)
            .Add(component => component.Value, value)
            .Add(component => component.Error, error)
            .Add(component => component.ValueChanged, (string? changed) => _changed = changed));

    [Fact]
    public void Render_IsARadioGroupOfRadios_NamedByItsLabel()
    {
        var field = RenderField(value: "b");

        field.Find("#period").GetAttribute("role").Should().Be("radiogroup");
        field.Find("#period").GetAttribute("aria-labelledby").Should().Be("period-label");
        field.FindAll("#period [role=radio]").Should().HaveCount(3);
        field.Find("#period-b").GetAttribute("aria-checked").Should().Be("true");
        field.Find("#period-a").GetAttribute("aria-checked").Should().Be("false");
    }

    [Fact]
    public void Click_SelectsTheOption()
    {
        var field = RenderField();

        field.Find("#period-c").Click();

        _changed.Should().Be("c");
    }

    [Theory]
    [InlineData("ArrowRight", "b")]
    [InlineData("ArrowDown", "b")]
    [InlineData("ArrowLeft", "c")]
    public void Arrows_MoveTheChoice(string key, string expected)
    {
        var field = RenderField(value: "a");

        field.Find("#period-a").KeyDown(key);

        _changed.Should().Be(expected);
    }

    [Fact]
    public void Render_OneTabStop_TheChosenOptionOrTheFirst()
    {
        var chosen = RenderField(value: "b");
        chosen.FindAll("#period [role=radio]").Select(option => option.GetAttribute("tabindex")).Should().Equal("-1", "0", "-1");

        var none = RenderField();
        none.FindAll("#period [role=radio]").Select(option => option.GetAttribute("tabindex")).Should().Equal("0", "-1", "-1");
    }

    [Fact]
    public void Error_IsTheDescriptionOfTheGroup()
    {
        var field = RenderField(error: "Pick a period.");

        field.Find("#period").GetAttribute("aria-invalid").Should().Be("true");
        field.Find("#period-description").TextContent.Should().Contain("Pick a period.");
    }
}

/// <summary>F-184 AC3, BR3: yes, no and, when the page wants it, not set.</summary>
public sealed class AppYesNoFieldTests : KitTestContext
{
    private bool? _changed = true;
    private int _changes;

    private IRenderedComponent<AppYesNoField> RenderField(bool? value = null, string? notSet = null) =>
        Render<AppYesNoField>(parameters => parameters
            .Add(component => component.Id, "key")
            .Add(component => component.Label, "Correct answer")
            .Add(component => component.TrueText, "True")
            .Add(component => component.FalseText, "False")
            .Add(component => component.NotSetText, notSet)
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, (bool? changed) => { _changed = changed; _changes++; }));

    [Fact]
    public void Render_ShowsThePagesWords_AndNothingChosenWhenUnset()
    {
        var field = RenderField();

        field.FindAll("#key [role=radio]").Select(option => option.TextContent.Trim()).Should().Equal("True", "False");
        field.FindAll("#key [aria-checked='true']").Should().BeEmpty();
    }

    [Fact]
    public void Click_False_IsFalse()
    {
        var field = RenderField();

        field.Find("#key-False").Click();

        _changed.Should().BeFalse();
        _changes.Should().Be(1);
    }

    [Fact]
    public void NotSetText_AddsAThirdOption_ThatSetsNull()
    {
        var field = RenderField(value: true, notSet: "All");

        field.FindAll("#key [role=radio]").Select(option => option.TextContent.Trim()).Should().Equal("All", "True", "False");
        field.Find("#key-none").Click();

        _changed.Should().BeNull();
    }

    [Fact]
    public void Render_WithoutTheirWords_UsesYesAndNo()
    {
        var field = Render<AppYesNoField>(parameters => parameters
            .Add(component => component.Id, "ok")
            .Add(component => component.Label, "Is it right?"));

        field.FindAll("#ok [role=radio]").Select(option => option.TextContent.Trim()).Should().Equal("Yes", "No");
    }
}

/// <summary>F-184 AC4, BR4: the level is dots and a name, never colour alone.</summary>
public sealed class AppLevelFieldTests : KitTestContext
{
    private static readonly IReadOnlyList<string> Names = ["Very easy", "Easy", "Medium", "Hard", "Very hard"];

    private int? _changed = -1;
    private int _changes;

    private IRenderedComponent<AppLevelField> RenderField(int? value = null, bool required = false) =>
        Render<AppLevelField>(parameters => parameters
            .Add(component => component.Id, "level")
            .Add(component => component.Label, "Difficulty")
            .Add(component => component.Names, Names)
            .Add(component => component.Value, value)
            .Add(component => component.Required, required)
            .Add(component => component.ValueChanged, (int? changed) => { _changed = changed; _changes++; }));

    [Fact]
    public void Render_OneDotPerLevel_EachNamedWithItsNumberAndName()
    {
        var field = RenderField(value: 3);

        field.Find("#level").GetAttribute("role").Should().Be("radiogroup");
        var dots = field.FindAll("#level [role=radio]");
        dots.Should().HaveCount(5);
        dots[0].GetAttribute("aria-label").Should().Be("1 — Very easy");
        dots[2].GetAttribute("aria-label").Should().Be("3 — Medium");
        dots[2].GetAttribute("aria-checked").Should().Be("true");
        dots[3].GetAttribute("aria-checked").Should().Be("false");
    }

    [Fact]
    public void Render_ShowsTheNameOfTheLevel_OrNotSet()
    {
        RenderField(value: 3).Find(".app-level-name").TextContent.Should().Be("Medium");
        RenderField().Find(".app-level-name").TextContent.Should().Be("Not set");
    }

    [Fact]
    public void Render_ChosenLevelAndThoseBelowAreFilled()
    {
        var dots = RenderField(value: 3).FindAll("#level .app-level-dot");

        dots.Select(dot => dot.ClassList.Contains("app-level-dot-on")).Should().Equal(true, true, true, false, false);
    }

    [Fact]
    public void Click_ADot_SetsThatLevel()
    {
        var field = RenderField();

        field.FindAll("#level [role=radio]")[2].Click();

        _changed.Should().Be(3);
    }

    [Theory]
    [InlineData("ArrowRight", 4)]
    [InlineData("ArrowUp", 4)]
    [InlineData("ArrowLeft", 2)]
    [InlineData("Home", 1)]
    [InlineData("End", 5)]
    public void Keys_MoveTheLevel(string key, int expected)
    {
        var field = RenderField(value: 3);

        field.FindAll("#level [role=radio]")[2].KeyDown(key);

        _changed.Should().Be(expected);
    }

    [Fact]
    public void Clear_SetsNull_AndIsAbsentWhenRequired()
    {
        var field = RenderField(value: 2);

        field.Find("#level-clear").Click();

        _changed.Should().BeNull();
        RenderField(value: 2, required: true).FindAll("#level-clear").Should().BeEmpty();
    }

    [Fact]
    public void Disabled_DotsDoNotChangeTheValue()
    {
        var field = Render<AppLevelField>(parameters => parameters
            .Add(component => component.Id, "level")
            .Add(component => component.Label, "Difficulty")
            .Add(component => component.Names, Names)
            .Add(component => component.Disabled, true)
            .Add(component => component.ValueChanged, (int? changed) => _changes++));

        field.FindAll("#level [role=radio]")[1].Click();

        _changes.Should().Be(0);
        field.Find("#level").GetAttribute("aria-disabled").Should().Be("true");
    }
}

/// <summary>F-184 AC5, BR5: the filter mode is toggle chips.</summary>
public sealed class AppLevelFilterFieldTests : KitTestContext
{
    private static readonly IReadOnlyList<string> Names = ["Very easy", "Easy", "Medium", "Hard", "Very hard"];

    private readonly List<int?> _toggled = [];

    private IRenderedComponent<AppLevelFilterField> RenderField(IReadOnlyList<int>? selected = null, bool notSet = false) =>
        Render<AppLevelFilterField>(parameters => parameters
            .Add(component => component.Id, "filter")
            .Add(component => component.Label, "Difficulty")
            .Add(component => component.Names, Names)
            .Add(component => component.NotSetText, "Not set")
            .Add(component => component.Selected, selected ?? [])
            .Add(component => component.NotSetSelected, notSet)
            .Add(component => component.OnToggle, (int? level) => _toggled.Add(level)));

    [Fact]
    public void Render_OneChipPerLevelAndNotSet_EachWithItsPressedState()
    {
        var field = RenderField(selected: [2, 4], notSet: true);

        field.Find("#filter").GetAttribute("role").Should().Be("group");
        field.FindAll("#filter button.app-level-chip").Should().HaveCount(6);
        field.Find("#filter-2").GetAttribute("aria-pressed").Should().Be("true");
        field.Find("#filter-3").GetAttribute("aria-pressed").Should().Be("false");
        field.Find("#filter-none").GetAttribute("aria-pressed").Should().Be("true");
        field.Find("#filter-4").TextContent.Should().Contain("Hard");
    }

    [Fact]
    public void Click_ReportsTheLevel_AndNullForNotSet()
    {
        var field = RenderField();

        field.Find("#filter-5").Click();
        field.Find("#filter-none").Click();

        _toggled.Should().Equal(5, null);
    }
}

/// <summary>F-184 AC6, BR6: the level, a yes/no and their column as text.</summary>
public sealed class ChoiceDisplayTests : KitTestContext
{
    private static readonly IReadOnlyList<string> Names = ["Very easy", "Easy", "Medium", "Hard", "Very hard"];

    [Fact]
    public void LevelBadge_ShowsTheDotsAndTheName()
    {
        var badge = Render<AppLevelBadge>(parameters => parameters
            .Add(component => component.Value, 3)
            .Add(component => component.Names, Names));

        badge.FindAll(".app-level-dot-on").Should().HaveCount(3);
        badge.FindAll(".app-level-dot").Should().HaveCount(5);
        badge.Find(".app-level-badge").TextContent.Should().Contain("Medium");
    }

    [Fact]
    public void LevelBadge_NoLevel_ReadsNotSet()
    {
        var badge = Render<AppLevelBadge>(parameters => parameters
            .Add(component => component.Value, (int?)null)
            .Add(component => component.Names, Names));

        badge.Find(".app-level-badge").TextContent.Should().Contain("Not set");
        badge.FindAll(".app-level-dot-on").Should().BeEmpty();
    }

    [Theory]
    [InlineData(true, "Yes")]
    [InlineData(false, "No")]
    [InlineData(null, "—")]
    public void BoolText_ReadsYesNoOrADash(bool? value, string expected)
    {
        var text = Render<AppBoolText>(parameters => parameters.Add(component => component.Value, value));

        text.Find(".app-bool-text").TextContent.Trim().Should().Be(expected);
    }

    [Fact]
    public void BoolText_UsesThePagesWords()
    {
        var text = Render<AppBoolText>(parameters => parameters
            .Add(component => component.Value, true)
            .Add(component => component.TrueText, "True"));

        text.Find(".app-bool-text").TextContent.Trim().Should().Be("True");
    }
}

/// <summary>F-184 AC7, BR7: the language of the content, in its own name.</summary>
public sealed class AppLanguageFieldTests : KitTestContext
{
    [Fact]
    public void Render_IsASelectFieldOfTheSupportedLanguages()
    {
        Render<MudBlazor.MudPopoverProvider>();
        string? changed = null;

        var field = Render<AppLanguageField>(parameters => parameters
            .Add(component => component.Id, "language")
            .Add(component => component.Label, "Language")
            .Add(component => component.Value, "pt-BR")
            .Add(component => component.ValueChanged, (string? value) => changed = value));

        var select = field.FindComponent<AppSelectField<string>>().Instance;
        select.Options.Select(option => option.Value).Should().BeEquivalentTo(
            Simulab.Web.Localization.SupportedCultures.All.Select(culture => culture.Name));
        select.Options.Single(option => option.Value == "pt-BR").Text.Should().Contain("Português");
        field.Find("#language").GetAttribute("role").Should().Be("combobox");
        changed.Should().BeNull();
    }
}
