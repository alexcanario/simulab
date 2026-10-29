using System.Globalization;

namespace Simulab.Web.Components.Ui;

/// <summary>Skeleton: the tests of F-181 fail against it first.</summary>
public static class AppNumberFormat
{
    public static bool TryParseDecimal(string? text, CultureInfo culture, out decimal? value) => throw new NotImplementedException();

    public static bool TryParseInteger(string? text, CultureInfo culture, out int? value) => throw new NotImplementedException();

    public static string Format(decimal? value, int decimals, CultureInfo? culture = null) => throw new NotImplementedException();

    public static string FormatForInput(decimal value, int decimals, CultureInfo? culture = null) => throw new NotImplementedException();

    public static string FormatForInput(int value, CultureInfo? culture = null) => throw new NotImplementedException();

    public static decimal Round(decimal value, int decimals) => throw new NotImplementedException();
}
