namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// What the question list asks the server for (F-95, BR15). <paramref name="Page"/> is zero-based and
/// <paramref name="PageSize"/> is capped. A list is a read, so a status that is not one of the two values
/// is ignored, not an error.
/// </summary>
public sealed record QuestionListQuery(
    int Page = 0,
    int PageSize = QuestionListQuery.DefaultPageSize,
    string? Search = null,
    QuestionStatus? Status = null)
{
    public const int DefaultPageSize = 25;

    /// <summary>The cap the rule `api-contracts` sets for every paged list.</summary>
    public const int MaxPageSize = 100;

    /// <summary>The same query with its numbers brought inside the allowed range.</summary>
    public QuestionListQuery Sanitized() => this with
    {
        Page = Math.Max(Page, 0),
        PageSize = Math.Clamp(PageSize, 1, MaxPageSize)
    };
}
