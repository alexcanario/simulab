using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Components.Pages.QuestionBank;

/// <summary>
/// What the address of <c>/admin/questions</c> holds (F-100, BR8): the search, the filters and the page. A null or
/// empty list is no filter. <see cref="Page"/> is zero-based; the address writes it one-based.
/// </summary>
public sealed record QuestionListState(
    string? Search = null,
    IReadOnlyList<QuestionStatus>? Statuses = null,
    IReadOnlyList<QuestionType>? Types = null,
    IReadOnlyList<int>? Difficulties = null,
    bool DifficultyNotSet = false,
    IReadOnlyList<Guid>? SubjectIds = null,
    IReadOnlyList<Guid>? TopicIds = null,
    IReadOnlyList<Guid>? OrganizerIds = null,
    Guid? ExamEditionId = null,
    int Page = 0,
    int PageSize = AppDataTable<object>.DefaultPageSize)
{
    /// <summary>The ids of subject, topic and organizer picks together: the page limit of F-100 BR11 counts them.</summary>
    public const int PickedIdLimit = QuestionListQuery.MaxFilterValues;

    /// <summary>True while the search or any filter holds a value (the page's "Clear all" shows then).</summary>
    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Search)
        || Statuses is { Count: > 0 }
        || Types is { Count: > 0 }
        || Difficulties is { Count: > 0 }
        || DifficultyNotSet
        || PickedIdCount > 0
        || ExamEditionId is not null;

    /// <summary>The ids of subject, topic and organizer picks together.</summary>
    public int PickedIdCount => (SubjectIds?.Count ?? 0) + (TopicIds?.Count ?? 0) + (OrganizerIds?.Count ?? 0);

    /// <summary>The query the list sends for this state, with the page and search the table itself asks for.</summary>
    public QuestionListQuery ToQuery(int page, int pageSize, string? search) =>
        new(
            page,
            pageSize,
            search,
            Statuses,
            Types,
            Difficulties,
            DifficultyNotSet,
            SubjectIds,
            TopicIds,
            OrganizerIds,
            ExamEditionId);
}
