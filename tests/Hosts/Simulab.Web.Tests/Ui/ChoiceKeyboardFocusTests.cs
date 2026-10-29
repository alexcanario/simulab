using Bunit;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-184 AC2, AC4 (review finding): an arrow key moves the focus with the choice, so the next key starts from the new
/// option (a radio group) and a screen reader announces it. Added after the lock, in a new file.
/// </summary>
public sealed class ChoiceKeyboardFocusTests : KitTestContext
{
    private static readonly IReadOnlyList<string> Names = ["Very easy", "Easy", "Medium", "Hard", "Very hard"];

    private string? LastFocusedId() =>
        JSInterop.Invocations["simulabShell.focusOption"].Last().Arguments[0] as string;

    [Fact]
    public void LevelField_AnArrowKey_MovesTheFocusToTheNewDot()
    {
        var field = Render<AppLevelField>(parameters => parameters
            .Add(component => component.Id, "level")
            .Add(component => component.Label, "Difficulty")
            .Add(component => component.Names, Names)
            .Add(component => component.Value, 3));

        field.FindAll("#level [role=radio]")[2].KeyDown("ArrowRight");

        LastFocusedId().Should().Be("level-4");
    }

    [Fact]
    public void LevelField_TwoArrowKeys_ReachTheSecondNeighbour()
    {
        int? value = 3;
        var field = Render<AppLevelField>(parameters => parameters
            .Add(component => component.Id, "level")
            .Add(component => component.Label, "Difficulty")
            .Add(component => component.Names, Names)
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, (int? changed) => value = changed));

        field.FindAll("#level [role=radio]")[2].KeyDown("ArrowRight");
        field.Render(parameters => parameters.Add(component => component.Value, value));
        field.Find("#level-4").KeyDown("ArrowRight");

        value.Should().Be(5);
        LastFocusedId().Should().Be("level-5");
    }

    [Fact]
    public void SegmentedField_AnArrowKey_MovesTheFocusToTheNewOption()
    {
        IReadOnlyList<AppSegmentOption<string>> options = [new("a", "Day"), new("b", "Week"), new("c", "Month")];
        var field = Render<AppSegmentedField<string>>(parameters => parameters
            .Add(component => component.Id, "period")
            .Add(component => component.Label, "Period")
            .Add(component => component.Options, options)
            .Add(component => component.Value, "a"));

        field.Find("#period-a").KeyDown("ArrowRight");

        LastFocusedId().Should().Be("period-b");
    }

    [Fact]
    public void LevelBadgeAndField_TakeThePagesWordForNoLevel()
    {
        var badge = Render<AppLevelBadge>(parameters => parameters
            .Add(component => component.Value, (int?)null)
            .Add(component => component.Names, Names)
            .Add(component => component.NotSetText, "Não definida"));
        var field = Render<AppLevelField>(parameters => parameters
            .Add(component => component.Id, "level")
            .Add(component => component.Label, "Dificuldade")
            .Add(component => component.Names, Names)
            .Add(component => component.NotSetText, "Não definida"));

        badge.Find(".app-level-badge").TextContent.Should().Contain("Não definida");
        field.Find(".app-level-name").TextContent.Should().Be("Não definida");
    }
}
