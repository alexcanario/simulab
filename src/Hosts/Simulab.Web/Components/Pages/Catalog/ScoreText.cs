using System.Globalization;

namespace Simulab.Web.Components.Pages.Catalog;

/// <summary>
/// F-168: scores and points per question as the reader types and reads them, in the reader's culture ("60,00" in
/// pt-BR, design risk R7). The fields are bound as text, so a value that is not a number is caught here and refused
/// with the code the Api would answer, instead of never reaching the binding.
/// </summary>
public static class ScoreText
{
    /// <summary>The value as a field shows it: two decimal places; empty when there is none.</summary>
    public static string? ForField(decimal? value) => value?.ToString("0.00", CultureInfo.CurrentCulture);

    /// <summary>The value as a sentence reads it: "60" or "60.5", without trailing zeros.</summary>
    public static string Compact(decimal value) => value.ToString("#,0.##", CultureInfo.CurrentCulture);

    /// <summary>True with null for a blank text, true with the number for a number, false for anything else.</summary>
    public static bool TryParse(string? text, out decimal? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }
}
