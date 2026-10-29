using Bunit;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>F-181 AC2, AC6: the whole-number field.</summary>
public sealed class AppIntegerFieldTests : KitTestContext
{
    private int? _changed;
    private int _changes;
    private bool? _invalid;

    private IRenderedComponent<AppIntegerField> RenderField(int? value = null, string? error = null, bool stepper = false) =>
        Render<AppIntegerField>(parameters => parameters
            .Add(field => field.Id, "count")
            .Add(field => field.Label, "Questions")
            .Add(field => field.Value, value)
            .Add(field => field.Unit, "questions")
            .Add(field => field.Stepper, stepper)
            .Add(field => field.Error, error)
            .Add(field => field.ValueChanged, (int? changed) => { _changed = changed; _changes++; })
            .Add(field => field.InvalidChanged, (bool invalid) => _invalid = invalid));

    [Fact]
    public void Input_IsTextWithTheNumericKeyboard_AndShowsTheUnit()
    {
        var field = RenderField();

        var input = field.Find("#count");
        input.GetAttribute("type").Should().Be("text");
        input.GetAttribute("inputmode").Should().Be("numeric");
        field.Find(".app-integer-field").TextContent.Should().Contain("questions");
    }

    [Fact]
    public void Leave_AWholeNumber_IsTheValue()
    {
        var field = RenderField();

        field.Find("#count").Change("20");

        _changed.Should().Be(20);
        _invalid.Should().NotBe(true);
    }

    [Fact]
    public void Leave_Blank_IsNoValue()
    {
        var field = RenderField(value: 12);

        field.Find("#count").Change("");

        _changed.Should().BeNull();
        _changes.Should().Be(1);
    }

    [Theory]
    [InlineData("20,5")]
    [InlineData("abc")]
    public void Leave_NotAWholeNumber_KeepsTheTextShowsTheErrorAndDoesNotChangeTheValue(string typed)
    {
        var field = RenderField(value: 12);

        field.Find("#count").Change(typed);

        _changes.Should().Be(0, "a value that does not parse never reaches the page");
        _invalid.Should().BeTrue();
        field.Find("#count").GetAttribute("value").Should().Be(typed);
        field.Find("#count").GetAttribute("aria-invalid").Should().Be("true");
        field.Find("#count-description").TextContent.Should().Contain("whole number");
    }

    [Fact]
    public void Error_ThePagesOwnMessage_WinsOverTheFields()
    {
        var field = RenderField(error: "Between 1 and 500.");

        field.Find("#count").Change("abc");

        field.Find("#count-description").TextContent.Should().Contain("Between 1 and 500.").And.NotContain("whole number");
    }

    [Fact]
    public void Leave_AfterAnError_ValidTextClearsIt()
    {
        var field = RenderField();
        field.Find("#count").Change("abc");

        field.Find("#count").Change("7");

        _invalid.Should().BeFalse();
        field.Find("#count").GetAttribute("aria-invalid").Should().BeNull();
        _changed.Should().Be(7);
    }

    [Fact]
    public void Stepper_PlusAndMinus_MoveTheValue()
    {
        var field = RenderField(value: 5, stepper: true);

        field.Find("#count-increase").Click();
        _changed.Should().Be(6);

        field.Find("#count-decrease").Click();
        field.Find("#count-decrease").Click();
        _changed.Should().Be(4);
    }

    [Fact]
    public void Description_IsWhatTheInputPointsAt()
    {
        var field = RenderField();

        field.Find("#count").GetAttribute("aria-describedby").Should().Be("count-description");
        field.Find("label").GetAttribute("for").Should().Be("count");
    }
}

/// <summary>F-181 AC3, AC6: the decimal field rounds when the user leaves it, never while typing.</summary>
public sealed class AppDecimalFieldTests : KitTestContext
{
    private decimal? _changed;
    private int _changes;
    private bool? _invalid;

    private IRenderedComponent<AppDecimalField> RenderField(decimal? value = null) =>
        Render<AppDecimalField>(parameters => parameters
            .Add(field => field.Id, "weight")
            .Add(field => field.Label, "Weight")
            .Add(field => field.Decimals, 2)
            .Add(field => field.Value, value)
            .Add(field => field.ValueChanged, (decimal? changed) => { _changed = changed; _changes++; })
            .Add(field => field.InvalidChanged, (bool invalid) => _invalid = invalid));

    [Fact]
    public void Input_IsTextWithTheDecimalKeyboard()
    {
        var input = RenderField().Find("#weight");

        input.GetAttribute("type").Should().Be("text");
        input.GetAttribute("inputmode").Should().Be("decimal");
    }

    [Fact]
    public void Leave_TheCultureSeparator_IsRoundedAndShownWithTheDecimals()
    {
        var field = RenderField();

        field.Find("#weight").Change("1.5");

        _changed.Should().Be(1.5m);
        field.WaitForAssertion(() => field.Find("#weight").GetAttribute("value").Should().Be("1.50"));
    }

    [Fact]
    public void Leave_MoreDecimalsThanAsked_AreRounded()
    {
        var field = RenderField();

        field.Find("#weight").Change("2.345");

        _changed.Should().Be(2.35m);
        field.WaitForAssertion(() => field.Find("#weight").GetAttribute("value").Should().Be("2.35"));
    }

    [Fact]
    public void Leave_TheOtherSeparator_IsAParseErrorAndKeepsTheText()
    {
        var field = RenderField(value: 3m);

        field.Find("#weight").Change("1,5");

        _changes.Should().Be(0);
        _invalid.Should().BeTrue();
        field.Find("#weight").GetAttribute("value").Should().Be("1,5");
        field.Find("#weight-description").TextContent.Should().Contain("number");
    }

    [Fact]
    public void Value_ShownWithTheDecimalsOfTheField()
    {
        RenderField(value: 60m).Find("#weight").GetAttribute("value").Should().Be("60.00");
    }

    [Fact]
    public void Value_ChangedByThePage_IsShownAgain()
    {
        var field = RenderField(value: 1m);

        field.Render(parameters => parameters.Add(f => f.Value, 7.5m));

        field.Find("#weight").GetAttribute("value").Should().Be("7.50");
    }
}

/// <summary>F-181 AC4, BR4: the score field is the decimal field with the unit "points".</summary>
public sealed class AppScoreFieldTests : KitTestContext
{
    [Fact]
    public void Score_BehavesAsTheDecimalFieldAndShowsThePointsUnit()
    {
        decimal? changed = null;
        var field = Render<AppScoreField>(parameters => parameters
            .Add(f => f.Id, "points")
            .Add(f => f.Label, "Points per question")
            .Add(f => f.Decimals, 2)
            .Add(f => f.ValueChanged, (decimal? value) => changed = value));

        field.Find("#points").GetAttribute("inputmode").Should().Be("decimal");
        field.Find(".app-score-field").TextContent.Should().Contain("pts");

        field.Find("#points").Change("1.5");

        changed.Should().Be(1.5m);
    }
}

/// <summary>F-181 AC5, BR5: the year field is plain digits inside a range.</summary>
public sealed class AppYearFieldTests : KitTestContext
{
    private int? _changed;
    private int _changes;

    private IRenderedComponent<AppYearField> RenderField(int? value = null) =>
        Render<AppYearField>(parameters => parameters
            .Add(field => field.Id, "year")
            .Add(field => field.Label, "Year")
            .Add(field => field.Value, value)
            .Add(field => field.ValueChanged, (int? changed) => { _changed = changed; _changes++; }));

    [Fact]
    public void Value_IsShownPlain_WithFourDigitsAtMost()
    {
        var input = RenderField(2026).Find("#year");

        input.GetAttribute("value").Should().Be("2026");
        input.GetAttribute("inputmode").Should().Be("numeric");
        input.GetAttribute("maxlength").Should().Be("4");
        input.GetAttribute("type").Should().Be("text");
    }

    [Fact]
    public void Leave_AYearInTheRange_IsTheValue()
    {
        var field = RenderField();

        field.Find("#year").Change("2026");

        _changed.Should().Be(2026);
    }

    [Theory]
    [InlineData("2.026")]
    [InlineData("26")]
    [InlineData("1949")]
    [InlineData("2029")]
    public void Leave_NotAYearOrOutOfTheRange_IsAnError(string typed)
    {
        var field = RenderField();

        field.Find("#year").Change(typed);

        _changes.Should().Be(0);
        field.Find("#year").GetAttribute("aria-invalid").Should().Be("true");
        field.Find("#year").GetAttribute("value").Should().Be(typed);
    }

    [Fact]
    public void DefaultRange_IsFrom1950ToTheCurrentYearPlusTwo()
    {
        var field = RenderField();

        field.Find("#year").Change("2028");
        _changed.Should().Be(2028, "the clock of the test is in 2026");
        field.Find("#year").Change("1950");
        _changed.Should().Be(1950);
    }
}
