namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// What the question list asks the server for (F-95 BR15, F-100). <paramref name="Page"/> is zero-based and
/// <paramref name="PageSize"/> is capped. Between filters it is AND, among the values of one filter it is OR
/// (F-100 BR1); a list with no value is no filter. A list is a read, so a value that is not understood is
/// ignored, not an error (F-100 BR6).
/// </summary>
/// <param name="Page">The zero-based page.</param>
/// <param name="PageSize">The rows per page, capped at <see cref="QuestionListQuery.MaxPageSize"/>.</param>
/// <param name="Search">A word of the statement, case and accents ignored.</param>
/// <param name="Statuses">The statuses to keep.</param>
/// <param name="Types">The question types to keep.</param>
/// <param name="Difficulties">The difficulties to keep, each 1 to 5.</param>
/// <param name="DifficultyNotSet">Also keep the questions that have no difficulty.</param>
/// <param name="SubjectIds">Every question of any topic of these subjects.</param>
/// <param name="TopicIds">Questions of these topics (a union with the subjects' topics).</param>
/// <param name="OrganizerIds">Questions that came from an edition of these organizers.</param>
/// <param name="ExamEditionId">Questions that came from this edition.</param>
/// <param name="BaseTextId">Questions that link this base text, most recently changed first (F-102, BR11).</param>
public sealed record QuestionListQuery(
    int Page = 0,
    int PageSize = QuestionListQuery.DefaultPageSize,
    string? Search = null,
    IReadOnlyList<QuestionStatus>? Statuses = null,
    IReadOnlyList<QuestionType>? Types = null,
    IReadOnlyList<int>? Difficulties = null,
    bool DifficultyNotSet = false,
    IReadOnlyList<Guid>? SubjectIds = null,
    IReadOnlyList<Guid>? TopicIds = null,
    IReadOnlyList<Guid>? OrganizerIds = null,
    Guid? ExamEditionId = null,
    Guid? BaseTextId = null)
{
    public const int DefaultPageSize = 25;

    /// <summary>The cap the rule `api-contracts` sets for every paged list.</summary>
    public const int MaxPageSize = 100;

    /// <summary>The most values one filter list keeps (F-100 BR6); the rest is ignored.</summary>
    public const int MaxFilterValues = 100;

    /// <summary>
    /// The same query with its numbers brought inside the allowed range and every list distinct and cut at
    /// <see cref="MaxFilterValues"/>: the one place that holds the cap (F-100 BR6). A difficulty outside 1 to 5
    /// is dropped.
    /// </summary>
    public QuestionListQuery Sanitized() => this with
    {
        Page = Math.Max(Page, 0),
        PageSize = Math.Clamp(PageSize, 1, MaxPageSize),
        Statuses = Capped(Statuses),
        Types = Capped(Types),
        Difficulties = Capped(Difficulties?.Where(difficulty => difficulty is >= 1 and <= 5)),
        SubjectIds = Capped(SubjectIds),
        TopicIds = Capped(TopicIds),
        OrganizerIds = Capped(OrganizerIds)
    };

    private static List<T>? Capped<T>(IEnumerable<T>? values) =>
        values is null ? null : [.. values.Distinct().Take(MaxFilterValues)];
}
