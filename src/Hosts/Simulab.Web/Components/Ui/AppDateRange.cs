using System.Globalization;

namespace Simulab.Web.Components.Ui;

/// <summary>
/// F-185 BR1: a period for a filter. <paramref name="From"/> and <paramref name="To"/> are calendar days, either may be
/// open; <paramref name="Days"/> is set when a "last n days" preset made the range, so a page whose Api takes a number
/// of days reads it without counting days back from the dates.
/// </summary>
public sealed record AppDateRange(DateOnly? From = null, DateOnly? To = null, int? Days = null)
{
    private const string Dash = "–";

    /// <summary>No period: all time.</summary>
    public static AppDateRange Empty { get; } = new();

    public bool IsEmpty => From is null && To is null && Days is null;

    /// <summary>The range a page gives the field when its filter is a number of days (null: all time).</summary>
    public static AppDateRange OfDays(int? days) => days is null ? Empty : new AppDateRange(null, null, days);

    /// <summary>The last <paramref name="days"/> days up to <paramref name="today"/>.</summary>
    public static AppDateRange LastDays(int days, DateOnly today) => new(today.AddDays(-days), today, days);

    /// <summary>1 January of <paramref name="today"/>'s year up to <paramref name="today"/>.</summary>
    public static AppDateRange YearToDate(DateOnly today) => new(new DateOnly(today.Year, 1, 1), today, null);

    /// <summary>The range as text in the culture: "01/10/2026 – 27/10/2026"; an open end is left open; a dash for none.</summary>
    public string Format(CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        return (From, To) switch
        {
            ({ } from, { } to) => $"{from.ToString("d", culture)} {Dash} {to.ToString("d", culture)}",
            ({ } from, null) => $"{from.ToString("d", culture)} {Dash}",
            (null, { } to) => $"{Dash} {to.ToString("d", culture)}",
            _ => None
        };
    }

    private const string None = "—";
}
