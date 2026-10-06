namespace Simulab.Catalog.Contracts;

/// <summary>
/// What the subject list asks the server for (F-79, BR12). <paramref name="Page"/> is zero-based and
/// <paramref name="PageSize"/> is capped. <paramref name="WithoutArea"/> wins over <paramref name="AreaId"/>:
/// a list is a read, so two filters that contradict each other are not an error.
/// </summary>
public sealed record SubjectListQuery(
    int Page = 0,
    int PageSize = SubjectListQuery.DefaultPageSize,
    string? Search = null,
    Guid? AreaId = null,
    bool WithoutArea = false)
{
    public const int DefaultPageSize = 25;

    /// <summary>The cap the rule `api-contracts` sets for every paged list.</summary>
    public const int MaxPageSize = 100;

    /// <summary>The same query with its numbers brought inside the allowed range.</summary>
    public SubjectListQuery Sanitized() => this with
    {
        Page = Math.Max(Page, 0),
        PageSize = Math.Clamp(PageSize, 1, MaxPageSize)
    };
}
