using Simulab.SharedKernel.Text;

namespace Simulab.Catalog.Domain;

/// <summary>
/// How the catalog compares the text a person typed (F-33, BR9 and BR12). A normalized value is
/// trimmed, uppercased with the invariant culture and stripped of accents, so "Fundação" and
/// "FUNDACAO" are the same name. It is stored in its own column: the unique index and the search
/// both read it, which is what keeps them agreeing without a PostgreSQL extension. The recipe is
/// <see cref="ComparableText"/>'s since F-42, so the contracts that filter a list fold text the same way.
/// </summary>
public static class CatalogText
{
    /// <summary>The comparable form of <paramref name="value"/>; an empty string when there is nothing to compare.</summary>
    public static string Normalize(string? value) => ComparableText.Normalize(value);

    /// <summary>
    /// The words of a search box (F-36 BR3, F-80 BR1): normalized, split on whitespace, none empty. A list
    /// keeps a row only when every word is found in one of its fields; no words is no filter.
    /// </summary>
    public static string[] SearchWords(string? search) =>
        Normalize(search).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
}
