using System.Globalization;
using System.Text;

namespace Simulab.Catalog.Domain;

/// <summary>
/// How the catalog compares the text a person typed (F-33, BR9 and BR12). A normalized value is
/// trimmed, uppercased with the invariant culture and stripped of accents, so "Fundação" and
/// "FUNDACAO" are the same name. It is stored in its own column: the unique index and the search
/// both read it, which is what keeps them agreeing without a PostgreSQL extension.
/// </summary>
public static class CatalogText
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
