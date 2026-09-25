namespace Simulab.Catalog.Contracts;

/// <summary>
/// Parses a caller's own order for an enum column: the names, comma-separated, first to last (B-15,
/// generalized in F-34 for the assessment type and the scope). A column sorted by a translated label
/// is sorted in the reader's culture, which only the caller knows; the server ranks by the list it is
/// given. A bad value is never an error — sorting by such a column is secondary, so it just means "no
/// order given" and the query falls back on the stored name.
/// </summary>
public static class EnumOrder
{
    /// <summary>
    /// The parsed order, or null when <paramref name="value"/> is blank, holds a name that is not one of
    /// <typeparamref name="TEnum"/>'s, repeats one, or does not list every value exactly once.
    /// </summary>
    public static IReadOnlyList<TEnum>? Parse<TEnum>(string? value)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var names = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var parsed = new List<TEnum>(names.Length);
        foreach (var name in names)
        {
            if (!Enum.TryParse<TEnum>(name, ignoreCase: true, out var item) || !Enum.IsDefined(item) || parsed.Contains(item))
            {
                return null;
            }

            parsed.Add(item);
        }

        return parsed.Count == Enum.GetValues<TEnum>().Length ? parsed : null;
    }
}
