using System.Globalization;

namespace Simulab.Web.Components.Ui;

/// <summary>
/// F-181 BR1: numbers as the user types and reads them, in the culture of the user. The decimal separator of the
/// culture is the only separator a number accepts (no group separator, no other language's separator), so "1.5" in
/// pt-BR is an error to show, never a silent 15. A blank text is no value.
/// </summary>
public static class AppNumberFormat
{
    /// <summary>The text shown for no value.</summary>
    public const string None = "—";

    private const NumberStyles DecimalStyles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    /// <summary>True with null for a blank text, true with the number for a number, false for anything else.</summary>
    public static bool TryParseDecimal(string? text, CultureInfo culture, out decimal? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (decimal.TryParse(text.Trim(), DecimalStyles, culture, out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    /// <summary>True with null for a blank text, true with the number for a whole number, false for anything else.</summary>
    public static bool TryParseInteger(string? text, CultureInfo culture, out int? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (int.TryParse(text.Trim(), NumberStyles.AllowLeadingSign, culture, out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    /// <summary>A number to read: group separators and the asked decimals ("1.234,5"); a dash for no value.</summary>
    public static string Format(decimal? value, int decimals, CultureInfo? culture = null) =>
        value is null ? None : value.Value.ToString("N" + decimals.ToString(CultureInfo.InvariantCulture), culture ?? CultureInfo.CurrentCulture);

    /// <summary>A number to edit: the asked decimals and no group separator, so the text parses back.</summary>
    public static string FormatForInput(decimal value, int decimals, CultureInfo? culture = null) =>
        value.ToString("F" + decimals.ToString(CultureInfo.InvariantCulture), culture ?? CultureInfo.CurrentCulture);

    /// <summary>A whole number to edit: no group separator ("2026", never "2.026").</summary>
    public static string FormatForInput(int value, CultureInfo? culture = null) =>
        value.ToString("D", culture ?? CultureInfo.CurrentCulture);

    /// <summary>Rounds to the decimals the way a person counts: half away from zero.</summary>
    public static decimal Round(decimal value, int decimals) => Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}
