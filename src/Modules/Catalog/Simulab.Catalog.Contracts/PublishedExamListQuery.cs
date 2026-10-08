namespace Simulab.Catalog.Contracts;

/// <summary>
/// What the student catalog asks the server for (F-36, BR2, BR3, BR5). <paramref name="Page"/> is zero-based
/// and <paramref name="PageSize"/> is capped; the four filters and the text combine with AND, and the board
/// and the year match on the same published edition. <paramref name="State"/> is the acronym of one of the
/// 27 states (F-57 BR1, BR2): it lists only the <c>State</c> exams of that state.
/// </summary>
public sealed record PublishedExamListQuery(
    int Page = 0,
    int PageSize = PublishedExamListQuery.DefaultPageSize,
    string? Search = null,
    AssessmentType? AssessmentType = null,
    ExamScope? Scope = null,
    Guid? OrganizerId = null,
    int? NoticeYear = null,
    string? State = null)
{
    public const int DefaultPageSize = 25;

    /// <summary>The cap the rule `api-contracts` sets for every paged list.</summary>
    public const int MaxPageSize = 100;

    /// <summary>The same query with its numbers brought inside the allowed range.</summary>
    public PublishedExamListQuery Sanitized() => this with
    {
        Page = Math.Max(Page, 0),
        PageSize = Math.Clamp(PageSize, 1, MaxPageSize)
    };
}
