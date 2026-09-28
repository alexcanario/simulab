using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-35 BR16 and AC17: the kit's only date input. The states a page cannot show on its own - unreadable text,
/// the reader's format in three languages, the value that reaches the page - live here.
/// </summary>
public sealed class AppDateFieldTests : KitTestContext
{
    private static readonly DateOnly Day = new(2026, 6, 14);

    private DateOnly? _reported;
    private int _reports;

    private IRenderedComponent<AppDateField> RenderField(
        DateOnly? value = null,
        string? error = null,
        string? hint = null,
        bool required = false,
        bool disabled = false,
        DateOnly? min = null)
    {
        Render<MudPopoverProvider>();

        return Render<AppDateField>(parameters => parameters
            .Add(field => field.Id, "date")
            .Add(field => field.Label, "Application date")
            .Add(field => field.Value, value)
            .Add(field => field.ValueChanged, EventCallback.Factory.Create<DateOnly?>(this, reported =>
            {
                _reported = reported;
                _reports++;
            }))
            .Add(field => field.Error, error)
            .Add(field => field.Hint, hint)
            .Add(field => field.Required, required)
            .Add(field => field.Disabled, disabled)
            .Add(field => field.Min, min));
    }

    /// <summary>The day as the reader of the current culture types it: the short date pattern the field itself uses.</summary>
    private static string Typed(DateOnly day) =>
        day.ToDateTime(TimeOnly.MinValue).ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern, CultureInfo.CurrentCulture);

    private static void UseCulture(string name)
    {
        CultureInfo.CurrentCulture = new CultureInfo(name);
        CultureInfo.CurrentUICulture = new CultureInfo(name);
    }

    // Anatomy of AppTextField: the label points at the input, and the description says how to type.
    [Fact]
    public void Render_Empty_TheLabelPointsAtTheInputAndTheDescriptionSaysTheFormat()
    {
        var field = RenderField();

        field.Find("label").GetAttribute("for").Should().Be("date");
        var input = field.Find("#date");
        input.GetAttribute("aria-describedby").Should().Be("date-description");
        input.GetAttribute("inputmode").Should().Be("numeric");
        input.GetAttribute("value").Should().BeNullOrEmpty();
        field.Find("#date-description").TextContent.Should().Contain("Format: mm/dd/yyyy");
        field.Markup.Should().Contain("Open the calendar");
    }

    [Fact]
    public void Render_WithAHint_TheHintReplacesTheFormat()
    {
        var field = RenderField(hint: "The day the paper was applied.");

        field.Find("#date-description").TextContent.Should().Contain("The day the paper was applied.")
            .And.NotContain("Format:");
    }

    [Fact]
    public void Render_Required_MarksTheLabelAndTheInputAndOffersNoClearButton()
    {
        var field = RenderField(value: Day, required: true);

        field.Find(".app-field-required").TextContent.Should().Be("*");
        field.Find("#date").GetAttribute("aria-required").Should().Be("true");
        field.FindComponent<MudDatePicker>().Instance.Clearable.Should().BeFalse("a required date is never cleared");
    }

    [Fact]
    public void Render_NotRequired_CanBeCleared()
    {
        RenderField().FindComponent<MudDatePicker>().Instance.Clearable.Should().BeTrue();
    }

    // AC17: the reader types the date, so the picker is editable, in the reader's culture and short date pattern.
    [Fact]
    public void Render_TheKitWiresTheCultureTheFormatAndTheTypingPath()
    {
        var picker = RenderField(min: new DateOnly(2026, 1, 1)).FindComponent<MudDatePicker>().Instance;

        picker.Editable.Should().BeTrue();
        picker.AutoClose.Should().BeTrue();
        picker.MinDate.Should().Be(new DateTime(2026, 1, 1));
    }

    // AC5: a calendar day in, the same calendar day shown - no time, no zone.
    [Fact]
    public void Render_WithAValue_ShowsThatDayInTheReadersFormat()
    {
        var field = RenderField(value: Day);

        field.Find("#date").GetAttribute("value").Should().Be(Typed(Day));
    }

    // AC17: the three languages read and write the date in their own format.
    [Theory]
    [InlineData("pt-BR", "14/06/2026", "Formato: dd/mm/aaaa")]
    [InlineData("pt-PT", null, "Formato: dd/mm/aaaa")]
    [InlineData("en", null, "Format: mm/dd/yyyy")]
    public void Render_InEachLanguage_ShowsTheDayAndTheFormatHint(string culture, string? expectedDay, string expectedHint)
    {
        UseCulture(culture);

        var field = RenderField(value: Day);

        field.Find("#date").GetAttribute("value").Should().Be(expectedDay ?? Typed(Day));
        field.Find("#date-description").TextContent.Should().Contain(expectedHint);
    }

    // AC17: the keyboard is the full path - typing the date and leaving the input is what the page receives.
    [Fact]
    public void Type_ADateInTheReadersFormat_ReportsThatDay()
    {
        var field = RenderField();

        field.Find("#date").Change(Typed(Day));

        field.WaitForAssertion(() => _reported.Should().Be(Day));
        field.FindAll(".app-field-error-text").Should().BeEmpty();
    }

    [Fact]
    public void Type_ADateInPortuguese_ReportsThatDay()
    {
        UseCulture("pt-BR");
        var field = RenderField();

        field.Find("#date").Change("14/06/2026");

        field.WaitForAssertion(() => _reported.Should().Be(Day));
    }

    [Fact]
    public void Type_TextThatIsNotADate_SaysHowToTypeItAndReportsNoDay()
    {
        var field = RenderField();

        field.Find("#date").Change("not a date");

        field.WaitForAssertion(() =>
            field.Find(".app-field-error-text").TextContent.Should().Contain("This is not a date. Type it as mm/dd/yyyy."));
        field.Find("#date").GetAttribute("aria-invalid").Should().Be("true");
        _reported.Should().BeNull();
    }

    [Fact]
    public void Type_NotADateOverAFilledField_TakesTheValueBackToNull()
    {
        var field = RenderField(value: Day);

        field.Find("#date").Change("31/31/2026x");

        field.WaitForAssertion(() => field.Find(".app-field-error-text").TextContent.Should().Contain("This is not a date."));
        _reported.Should().BeNull();
        _reports.Should().BeGreaterThan(0, "the page has to hear that the day is gone");
    }

    [Fact]
    public void Type_Blank_IsNullAndNoError()
    {
        var field = RenderField(value: Day);

        field.Find("#date").Change(string.Empty);

        field.WaitForAssertion(() => _reports.Should().BeGreaterThan(0));
        _reported.Should().BeNull();
        field.FindAll(".app-field-error-text").Should().BeEmpty();
    }

    [Fact]
    public void Render_WithAnError_ShowsItInsteadOfTheHintAndMarksTheInput()
    {
        var field = RenderField(error: "The application date cannot be before 1 January of the notice year.");

        field.Find(".app-field-error-text").TextContent.Should().Contain("cannot be before 1 January");
        field.Find("#date").GetAttribute("aria-invalid").Should().Be("true");
        field.Find("#date-description").TextContent.Should().NotContain("Format:");
    }

    [Fact]
    public void Render_Disabled_TheInputCannotBeFocused()
    {
        var field = RenderField(value: Day, disabled: true);

        field.Find("#date").HasAttribute("disabled").Should().BeTrue();
        field.FindComponent<MudDatePicker>().Instance.Clearable.Should().BeFalse();
    }
}
