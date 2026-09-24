namespace Simulab.Catalog.Contracts;

/// <summary>
/// Parses the `kindOrder` query parameter (B-15): the three <see cref="OrganizerKind"/> names, comma-separated,
/// in the order the caller wants the kind column sorted by. Sorting by kind is a secondary column (F-33 BR12), so
/// a bad value never fails the request — it just means "no order given", and the caller falls back on its own.
/// </summary>
public static class OrganizerKindOrder
{
    /// <summary>
    /// The parsed order, or null when <paramref name="value"/> is blank, has anything other than the three
    /// known kinds, or repeats one.
    /// </summary>
    public static IReadOnlyList<OrganizerKind>? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var names = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var kinds = new List<OrganizerKind>(names.Length);
        foreach (var name in names)
        {
            if (!Enum.TryParse<OrganizerKind>(name, ignoreCase: true, out var kind) || !Enum.IsDefined(kind) || kinds.Contains(kind))
            {
                return null;
            }

            kinds.Add(kind);
        }

        return kinds.Count == Enum.GetValues<OrganizerKind>().Length ? kinds : null;
    }
}
