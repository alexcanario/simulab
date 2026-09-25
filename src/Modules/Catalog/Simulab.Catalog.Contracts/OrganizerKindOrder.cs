namespace Simulab.Catalog.Contracts;

/// <summary>
/// Parses the `kindOrder` query parameter (B-15): the <see cref="OrganizerKind"/> names, comma-separated,
/// in the order the caller wants the kind column sorted by. Sorting by kind is a secondary column (F-33 BR12), so
/// a bad value never fails the request — it just means "no order given", and the caller falls back on its own.
/// The parsing moved to <see cref="EnumOrder"/> in F-34, when the exam list needed the same for two more columns;
/// this type stays as the name the organizer endpoint reads.
/// </summary>
public static class OrganizerKindOrder
{
    /// <summary>
    /// The parsed order, or null when <paramref name="value"/> is blank, has anything other than the known
    /// kinds, repeats one, or does not list them all.
    /// </summary>
    public static IReadOnlyList<OrganizerKind>? Parse(string? value) => EnumOrder.Parse<OrganizerKind>(value);
}
