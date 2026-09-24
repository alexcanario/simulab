namespace Simulab.Catalog.Contracts;

/// <summary>
/// What the issuing-authority list asks the server for (F-34 BR18, v2). <paramref name="Page"/> is
/// zero-based and <paramref name="PageSize"/> is capped; <paramref name="SortBy"/> is one of
/// <see cref="IssuingAuthoritySort"/> and anything else falls back to the name.
/// </summary>
public sealed record IssuingAuthorityListQuery(
    int Page = 0,
    int PageSize = IssuingAuthorityListQuery.DefaultPageSize,
    string? Search = null,
    string? SortBy = null,
    bool Descending = false)
{
    public const int DefaultPageSize = 25;

    /// <summary>The cap the rule `api-contracts` sets for every paged list.</summary>
    public const int MaxPageSize = 100;

    /// <summary>The same query with its numbers brought inside the allowed range.</summary>
    public IssuingAuthorityListQuery Sanitized() => this with
    {
        Page = Math.Max(Page, 0),
        PageSize = Math.Clamp(PageSize, 1, MaxPageSize)
    };
}
