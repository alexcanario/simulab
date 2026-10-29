using System.Globalization;

namespace Simulab.Web.Components.Ui;

/// <summary>Skeleton: the tests of F-185 fail against it first.</summary>
public sealed record AppDateRange(DateOnly? From = null, DateOnly? To = null, int? Days = null)
{
    public static AppDateRange Empty { get; } = new();

    public bool IsEmpty => throw new NotImplementedException();

    public static AppDateRange OfDays(int? days) => throw new NotImplementedException();

    public string Format(CultureInfo? culture = null) => throw new NotImplementedException();
}
