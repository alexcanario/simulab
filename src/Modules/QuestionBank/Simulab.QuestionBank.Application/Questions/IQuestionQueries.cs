using Simulab.QuestionBank.Contracts;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>The read side of the question list and the question form (F-95).</summary>
public interface IQuestionQueries
{
    /// <summary>One page, newest first, deleted questions excluded, filtered by status and statement text (BR15).</summary>
    Task<QuestionPageResponse> ListAsync(QuestionListQuery query, CancellationToken cancellationToken);

    /// <summary>The question with its options and key, or null when it does not exist or was deleted.</summary>
    Task<QuestionResponse?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Editions of any status the origin lookup offers, by exam name then notice year descending, at most
    /// <paramref name="take"/> (F-96, BR9).
    /// </summary>
    Task<IReadOnlyList<ExamEditionLookupResponse>> SearchEditionsAsync(
        string? search,
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// Live subjects whose name contains <paramref name="search"/> (case and accents ignored), by name, at most
    /// <paramref name="take"/> (B-28, BR1).
    /// </summary>
    Task<IReadOnlyList<SubjectLookupResponse>> SearchSubjectsAsync(
        string? search,
        int take,
        CancellationToken cancellationToken);

    /// <summary>The live topics of a subject by name, or null when the subject is unknown or deleted (B-28, BR2).</summary>
    Task<IReadOnlyList<TopicLookupResponse>?> ListSubjectTopicsAsync(Guid subjectId, CancellationToken cancellationToken);

    /// <summary>One edition for prefilling the form, or null when it does not exist or was deleted (F-96, BR9).</summary>
    Task<ExamEditionLookupResponse?> FindEditionAsync(Guid examEditionId, CancellationToken cancellationToken);

    /// <summary>The notice subjects of an edition in display order, or null when the edition is unknown (F-96, BR9).</summary>
    Task<IReadOnlyList<NoticeSubjectLookupResponse>?> ListNoticeSubjectsAsync(
        Guid examEditionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// One page of the edition's live questions in paper order, drafts and published, or null when the edition
    /// is unknown (F-96, BR10). <paramref name="page"/> is zero-based and the size is capped as the list's.
    /// </summary>
    Task<EditionQuestionPageResponse?> ListEditionQuestionsAsync(
        Guid examEditionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
