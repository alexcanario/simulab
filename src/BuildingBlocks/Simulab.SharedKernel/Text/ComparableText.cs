using System.Globalization;
using System.Text;

namespace Simulab.SharedKernel.Text;

/// <summary>
/// The one recipe for comparing text a person typed: trimmed, upper-cased with the invariant culture and
/// stripped of accents, so "Fundação" and "FUNDACAO" are the same. The catalog stores it in its own columns and
/// the search and the pickers read it, which only works while every reader folds text the same way (F-33, F-42).
/// </summary>
public static class ComparableText
{
    /// <summary>The comparable form of <paramref name="value"/>; an empty string when there is nothing to compare.</summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
