namespace Simulab.Catalog.Contracts;

/// <summary>
/// What the exam list asks the server for (F-34, BR14). <paramref name="Page"/> is zero-based and
/// <paramref name="PageSize"/> is capped; <paramref name="SortBy"/> is one of <see cref="ExamSort"/> and
/// anything else falls back to the name. The three filters combine with AND, and the two order lists are
/// the caller's own order for the columns whose label is translated (B-15).
/// </summary>
public sealed record ExamListQuery(
    int Page = 0,
    int PageSize = ExamListQuery.DefaultPageSize,
    string? Search = null,
    Guid? IssuingAuthorityId = null,
    AssessmentType? AssessmentType = null,
    ExamScope? Scope = null,
    string? SortBy = null,
    bool Descending = false,
    IReadOnlyList<AssessmentType>? AssessmentTypeOrder = null,
    IReadOnlyList<ExamScope>? ScopeOrder = null)
{
    public const int DefaultPageSize = 25;

    /// <summary>The cap the rule `api-contracts` sets for every paged list.</summary>
    public const int MaxPageSize = 100;

    /// <summary>The same query with its numbers brought inside the allowed range.</summary>
    public ExamListQuery Sanitized() => this with
    {
        Page = Math.Max(Page, 0),
        PageSize = Math.Clamp(PageSize, 1, MaxPageSize)
    };
}
