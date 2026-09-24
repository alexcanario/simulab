namespace Simulab.Catalog.Contracts;

/// <summary>
/// What the list asks the server for (F-33, BR12). <paramref name="Page"/> is zero-based and
/// <paramref name="PageSize"/> is capped; <paramref name="SortBy"/> is one of
/// <see cref="OrganizerSort"/> and anything else falls back to the name. <paramref name="KindOrder"/> is the
/// caller's own order for the three kinds (B-15, BR15); null falls back to the stored name order.
/// </summary>
public sealed record OrganizerListQuery(
    int Page = 0,
    int PageSize = OrganizerListQuery.DefaultPageSize,
    string? Search = null,
    string? SortBy = null,
    bool Descending = false,
    IReadOnlyList<OrganizerKind>? KindOrder = null)
{
    public const int DefaultPageSize = 25;

    /// <summary>The cap the rule `api-contracts` sets for every paged list.</summary>
    public const int MaxPageSize = 100;

    /// <summary>The same query with its numbers brought inside the allowed range.</summary>
    public OrganizerListQuery Sanitized() => this with
    {
        Page = Math.Max(Page, 0),
        PageSize = Math.Clamp(PageSize, 1, MaxPageSize)
    };
}
