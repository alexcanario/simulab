using System.Globalization;
using System.Resources;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MudBlazor;
using Simulab.Web.Components.Pages.Dev;
using Simulab.Web.Components.Ui;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.Ui;

/// <summary>F-185 AC1 to AC4, BR1 to BR4: a period is the last n days, this year or two dates.</summary>
public sealed class AppDateRangeFieldTests : KitTestContext
{
    // The context's clock stands on 2026-09-28 (UTC).
    private static readonly DateOnly Today = new(2026, 9, 28);

    private AppDateRange? _changed;
    private int _changes;

    private IRenderedComponent<AppDateRangeField> RenderField(
        AppDateRange? value = null, bool thisYear = true, bool custom = true, string? all = "All time") =>
        Render<AppDateRangeField>(parameters => parameters
            .Add(field => field.Id, "period")
            .Add(field => field.Label, "Period")
            .Add(field => field.Value, value ?? AppDateRange.Empty)
            .Add(field => field.ThisYear, thisYear)
            .Add(field => field.AllowCustom, custom)
            .Add(field => field.AllText, all)
            .Add(field => field.ValueChanged, (AppDateRange changed) => { _changed = changed; _changes++; }));

    [Fact]
    public void Render_OffersAllTheDayPresetsThisYearAndCustom_InThatOrder()
    {
        var field = RenderField();

        field.Find("#period").GetAttribute("role").Should().Be("group");
        field.FindAll("#period button.app-range-preset").Select(button => button.TextContent.Trim()).Should().Equal(
            "All time", "Last 7 days", "Last 30 days", "Last 90 days", "This year", "Custom range");
    }

    [Fact]
    public void Click_LastThirtyDays_IsTheLastThirtyDaysUpToToday()
    {
        var field = RenderField();

        field.Find("#period-30").Click();

        _changed.Should().Be(new AppDateRange(Today.AddDays(-30), Today, 30));
    }

    [Fact]
    public void Render_TheChosenPreset_IsPressed_AndTheOthersAreNot()
    {
        var field = RenderField(AppDateRange.OfDays(30));

        field.Find("#period-30").GetAttribute("aria-pressed").Should().Be("true");
        field.Find("#period-7").GetAttribute("aria-pressed").Should().Be("false");
        field.Find("#period-all").GetAttribute("aria-pressed").Should().Be("false");
        RenderField().Find("#period-all").GetAttribute("aria-pressed").Should().Be("true");
    }

    [Fact]
    public void Click_All_IsTheEmptyRange()
    {
        var field = RenderField(AppDateRange.OfDays(7));

        field.Find("#period-all").Click();

        _changed.Should().Be(AppDateRange.Empty);
    }

    [Fact]
    public void Click_ThisYear_IsTheFirstOfJanuaryUpToToday_WithNoDays()
    {
        var field = RenderField();

        field.Find("#period-year").Click();

        _changed.Should().Be(new AppDateRange(new DateOnly(2026, 1, 1), Today, null));
    }

    [Fact]
    public void PresetsOnly_NoThisYearAndNoCustom_OffersJustTheDaysAndAll()
    {
        var field = RenderField(thisYear: false, custom: false);

        field.FindAll("#period button.app-range-preset").Select(button => button.TextContent.Trim()).Should().Equal(
            "All time", "Last 7 days", "Last 30 days", "Last 90 days");
    }

    [Fact]
    public void Click_Custom_ShowsTwoDateFields()
    {
        var field = RenderField();

        field.Find("#period-custom").Click();

        field.FindAll("#period-from").Should().ContainSingle();
        field.FindAll("#period-to").Should().ContainSingle();
        field.Find("#period-custom").GetAttribute("aria-pressed").Should().Be("true");
    }

    [Fact]
    public void Custom_AStartAfterTheEnd_ShowsTheErrorAndDoesNotChangeTheValue()
    {
        var field = RenderField(new AppDateRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 10), null));
        field.Find("#period-custom").Click();
        _changes = 0;

        field.FindComponents<AppDateField>()[0].Instance.ValueChanged.InvokeAsync(new DateOnly(2026, 9, 20));

        field.WaitForAssertion(() => field.Find("#period-range-error").TextContent.Should().Contain("start date cannot be after the end date"));
        _changes.Should().Be(0);
    }

    [Fact]
    public void Custom_AValidRange_IsReported_AndAnEndMayBeLeftEmpty()
    {
        var field = RenderField(new AppDateRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 10), null));
        field.Find("#period-custom").Click();

        field.FindComponents<AppDateField>()[1].Instance.ValueChanged.InvokeAsync(null);

        field.WaitForAssertion(() => _changed.Should().Be(new AppDateRange(new DateOnly(2026, 9, 1), null, null)));
    }
}

/// <summary>F-185 AC5, BR5: a moment in the past reads the way a person says it.</summary>
public sealed class AppRelativeTimeTests : KitTestContext
{
    private IRenderedComponent<AppRelativeTime> RenderAgo(TimeSpan ago) =>
        Render<AppRelativeTime>(parameters => parameters.Add(component => component.Value, Clock.GetUtcNow() - ago));

    [Theory]
    [InlineData(0, 0, 30, "Just now")]
    [InlineData(0, 0, 300, "5 minutes ago")]
    [InlineData(0, 1, 0, "1 hour ago")]
    [InlineData(1, 0, 0, "1 day ago")]
    [InlineData(3, 0, 0, "3 days ago")]
    [InlineData(60, 0, 0, "2 months ago")]
    [InlineData(730, 0, 0, "2 years ago")]
    public void Render_ReadsTheSpanInTheBiggestUnit(int days, int hours, int seconds, string expected)
    {
        var time = RenderAgo(new TimeSpan(days, hours, 0, seconds));

        time.Find("time").TextContent.Trim().Should().Be(expected);
    }

    [Fact]
    public void Render_AMomentInTheFuture_ReadsJustNow()
    {
        RenderAgo(TimeSpan.FromHours(-2)).Find("time").TextContent.Trim().Should().Be("Just now");
    }

    [Fact]
    public void Render_IsATimeElement_WithTheExactMomentInTheTooltip()
    {
        var value = new DateTimeOffset(2026, 9, 25, 15, 4, 0, TimeSpan.Zero);

        var time = Render<AppRelativeTime>(parameters => parameters.Add(component => component.Value, value)).Find("time");

        time.GetAttribute("datetime").Should().Be(value.ToString("o", CultureInfo.InvariantCulture));
        time.GetAttribute("title").Should().Be(value.ToString("g", CultureInfo.CurrentCulture));
    }
}

/// <summary>F-185 AC6, BR6: a date and a range as text.</summary>
public sealed class DateTextTests : KitTestContext
{
    [Fact]
    public void DateText_ShowsTheDateInTheCulture_OrADash()
    {
        CultureInfo.CurrentCulture = new CultureInfo("pt-BR");

        Render<AppDateText>(parameters => parameters.Add(component => component.Value, new DateOnly(2026, 10, 27)))
            .Find(".app-date-text").TextContent.Should().Be("27/10/2026");
        Render<AppDateText>(parameters => parameters.Add(component => component.Value, (DateOnly?)null))
            .Find(".app-date-text").TextContent.Should().Be("—");
    }

    [Fact]
    public void Range_IsWrittenWithAnEnDash_AndOpenEndsAreSaid()
    {
        var culture = new CultureInfo("pt-BR");

        new AppDateRange(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 27), null).Format(culture).Should().Be("01/10/2026 – 27/10/2026");
        new AppDateRange(new DateOnly(2026, 10, 1), null, null).Format(culture).Should().Be("01/10/2026 –");
        new AppDateRange(null, new DateOnly(2026, 10, 27), null).Format(culture).Should().Be("– 27/10/2026");
        AppDateRange.Empty.Format(culture).Should().Be("—");
    }

    [Fact]
    public void Range_OfDays_IsKnownByItsDays_AndEmptyIsEmpty()
    {
        AppDateRange.OfDays(30).Days.Should().Be(30);
        AppDateRange.OfDays(null).Should().Be(AppDateRange.Empty);
        AppDateRange.Empty.IsEmpty.Should().BeTrue();
        AppDateRange.OfDays(7).IsEmpty.Should().BeFalse();
    }
}

/// <summary>F-185 AC7: the gallery shows the date controls.</summary>
public sealed class UiGalleryDatesTests : KitTestContext
{
    private IRenderedComponent<UiGallery> RenderGallery()
    {
        Services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(Environments.Development));
        Render<MudPopoverProvider>();

        return Render<UiGallery>(parameters => parameters.Add(page => page.SourceDelay, TimeSpan.Zero));
    }

    [Theory]
    [InlineData("Date range (AppDateRangeField)")]
    [InlineData("Relative time (AppRelativeTime)")]
    [InlineData("Date text (AppDateText)")]
    [InlineData("Empty")]
    [InlineData("Filled")]
    [InlineData("Error")]
    [InlineData("Disabled")]
    public void Render_Development_DatesSectionNamesEachKindAndState(string text)
    {
        RenderGallery().Find("#gallery-dates").TextContent.Should().Contain(text);
    }

    [Fact]
    public void Render_Development_DatesSectionUsesTheKitControls()
    {
        var section = RenderGallery().Find("#gallery-dates");

        section.QuerySelectorAll(".app-date-range-field").Should().NotBeEmpty();
        section.QuerySelectorAll("time.app-relative-time").Should().NotBeEmpty();
        section.QuerySelectorAll(".app-date-text").Should().NotBeEmpty();
    }
}

/// <summary>F-185 AC10: every text of the date controls exists in pt-BR, pt-PT and en (rule: i18n).</summary>
public sealed class DateResourcesTests
{
    private static readonly ResourceManager Manager =
        new(typeof(SharedResources).FullName!, typeof(SharedResources).Assembly);

    public static TheoryData<string, string> KeysByCulture()
    {
        string[] keys =
        [
            "DateRange.LastDays", "DateRange.ThisYear", "DateRange.Custom", "DateRange.From", "DateRange.To", "DateRange.Invalid",
            "Relative.Now", "Relative.Minute", "Relative.Minutes", "Relative.Hour", "Relative.Hours", "Relative.Day",
            "Relative.Days", "Relative.Month", "Relative.Months", "Relative.Year", "Relative.Years",
            "Gallery.Section.Dates", "Gallery.Dates.Hint", "Gallery.Dates.Range", "Gallery.Dates.Relative", "Gallery.Dates.Text",
            "Gallery.Dates.State.Empty", "Gallery.Dates.State.Filled", "Gallery.Dates.State.Error", "Gallery.Dates.State.Disabled",
            "Gallery.Dates.Label.Period", "Gallery.Dates.All"
        ];
        var data = new TheoryData<string, string>();
        foreach (var culture in new[] { "en", "pt-BR", "pt-PT" })
        {
            foreach (var key in keys)
            {
                data.Add(culture, key);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(KeysByCulture))]
    public void Key_HasATextInTheLanguage(string culture, string key)
    {
        var text = Manager.GetString(key, new CultureInfo(culture));

        text.Should().NotBeNullOrWhiteSpace($"'{key}' must exist in {culture}");
    }
}
