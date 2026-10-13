using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Components.Pages.QuestionBank;

/// <summary>
/// What the address of <c>/admin/questions</c> holds (F-100, BR8): the search, the filters and the page. An empty
/// list is no filter. <see cref="Page"/> is zero-based; the address writes it one-based.
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
    /// <summary>True while any filter or the search holds a value (the page's "Clear all" shows then).</summary>
    public bool HasFilters => throw new NotImplementedException();

    /// <summary>The ids of subject, topic and organizer picks together: the page limit of F-100 BR11 counts them.</summary>
    public int PickedIdCount => throw new NotImplementedException();

    /// <summary>The query the list sends for this state and the page the table asks for.</summary>
    public QuestionListQuery ToQuery(int page, int pageSize, string? search) => throw new NotImplementedException();
}
