using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>
/// What the question handlers need from the database (F-95). The port exists because a rule needs data — the
/// topic exists — and this project never references EF Core.
/// </summary>
public interface IQuestionStore
{
    /// <summary>The question with its options, tracked; null when it does not exist or was deleted.</summary>
    Task<Question?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>True when a live topic with this id exists in the catalog (BR16).</summary>
    Task<bool> TopicExistsAsync(Guid topicId, CancellationToken cancellationToken);

    /// <summary>True when an edition of any status with this id exists in the catalog and was not deleted (F-96, BR2).</summary>
    Task<bool> ExamEditionExistsAsync(Guid examEditionId, CancellationToken cancellationToken);

    /// <summary>True when a live notice subject with this id exists and belongs to that edition (F-96, BR2).</summary>
    Task<bool> NoticeSubjectBelongsToAsync(
        Guid noticeSubjectId,
        Guid examEditionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// True when a live question other than <paramref name="exceptQuestionId"/> already has this number in the
    /// edition (F-96, BR3). The unique index is the last line; this is the answer the form gets.
    /// </summary>
    Task<bool> IsPaperNumberTakenAsync(
        Guid examEditionId,
        int paperNumber,
        Guid? exceptQuestionId,
        CancellationToken cancellationToken);

    void Add(Question question);

    /// <summary>Soft-deletes the question and its options: the interceptor turns the removal into a flag (BR11).</summary>
    void Remove(Question question);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Saves, and answers the 409 of a number two saves raced for (F-96, BR3) instead of throwing; null when
    /// the save went through. Any other database error propagates.
    /// </summary>
    Task<Error?> TrySaveChangesAsync(CancellationToken cancellationToken);
}
