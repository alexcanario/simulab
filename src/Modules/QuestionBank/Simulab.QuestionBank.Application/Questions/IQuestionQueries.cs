using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>The read side of the question list and the question form (F-95).</summary>
public interface IQuestionQueries
{
    /// <summary>One page, newest first, deleted questions excluded, filtered by status and statement text (BR15).</summary>
    Task<QuestionPageResponse> ListAsync(QuestionListQuery query, CancellationToken cancellationToken);

    /// <summary>The question with its options and key, or null when it does not exist or was deleted.</summary>
    Task<QuestionResponse?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// The versions of a question, newest first, with the highest number marked current (F-98, UC5), or null when
    /// the question does not exist or was deleted. A question that was never published has an empty list.
    /// </summary>
    Task<IReadOnlyList<QuestionVersionListItemResponse>?> ListVersionsAsync(
        Guid questionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// One version of a question (F-98, UC5). Fails with <c>question.not_found</c> when the question does not
    /// exist or was deleted, and with <c>question.version_not_found</c> when it has no version with that number.
    /// </summary>
    Task<Result<QuestionVersionResponse>> FindVersionAsync(
        Guid questionId,
        int number,
        CancellationToken cancellationToken);

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

    /// <summary>The live taxonomy for the list's subject and topic filter: subjects by name, each with its topics (F-100).</summary>
    Task<IReadOnlyList<QuestionTaxonomySubjectResponse>> GetTaxonomyAsync(CancellationToken cancellationToken);

    /// <summary>Every live organizer by acronym, for the list's organizer filter (F-100).</summary>
    Task<IReadOnlyList<OrganizerLookupResponse>> ListOrganizersAsync(CancellationToken cancellationToken);

    /// <summary>One edition for prefilling the form, or null when it does not exist or was deleted (F-96, BR9).</summary>
    Task<ExamEditionLookupResponse?> FindEditionAsync(Guid examEditionId, CancellationToken cancellationToken);

    /// <summary>The notice subjects of an edition in display order, or null when the edition is unknown (F-96, BR9).</summary>
    Task<IReadOnlyList<NoticeSubjectLookupResponse>?> ListNoticeSubjectsAsync(
        Guid examEditionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// The number transcription mode suggests next: the highest paper number among the edition's live questions
    /// plus one, or 1 when it has none; null when the edition is unknown (F-99, BR4).
    /// </summary>
    Task<int?> GetNextPaperNumberAsync(Guid examEditionId, CancellationToken cancellationToken);

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
