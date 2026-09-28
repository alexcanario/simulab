using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.ExamEditions;

/// <summary>
/// What the edition handlers need from the database (F-35). The port exists because the rules that need
/// data — the parents that must exist (BR3), the duplicate (BR10) and the two "has editions" guards
/// (BR12) — live in a handler, and this project never references EF Core.
/// </summary>
public interface IExamEditionStore
{
    /// <summary>
    /// The edition with this id under this exam, or null when it does not exist, was deleted or belongs to
    /// another exam (the route's exam and the row's exam must agree).
    /// </summary>
    Task<ExamEdition?> FindAsync(Guid examId, Guid id, CancellationToken cancellationToken);

    /// <summary>True when an exam with this id exists and was not deleted (BR3).</summary>
    Task<bool> ExamExistsAsync(Guid examId, CancellationToken cancellationToken);

    /// <summary>True when an organizer with this id exists and was not deleted (BR3).</summary>
    Task<bool> OrganizerExistsAsync(Guid organizerId, CancellationToken cancellationToken);

    /// <summary>
    /// True when another edition of the exam already holds this year, normalized position and board (BR10).
    /// Deleted rows count: they stay in the unique index.
    /// </summary>
    Task<bool> IsDuplicateAsync(
        Guid examId,
        int noticeYear,
        string normalizedPosition,
        Guid organizerId,
        Guid? exceptId,
        CancellationToken cancellationToken);

    /// <summary>True when the exam has at least one edition that was not deleted (BR12).</summary>
    Task<bool> ExamHasEditionsAsync(Guid examId, CancellationToken cancellationToken);

    /// <summary>True when at least one edition that was not deleted names this organizer (BR12).</summary>
    Task<bool> OrganizerHasEditionsAsync(Guid organizerId, CancellationToken cancellationToken);

    void Add(ExamEdition edition);

    /// <summary>Soft-deletes the edition (BR1): the interceptor turns the removal into a flag.</summary>
    void Remove(ExamEdition edition);

    /// <summary>
    /// Saves, and answers with the 409 for a duplicate when the unique index refuses the row after the
    /// "is duplicate" check passed: another writer committed in between (the B-14 lesson). Null when saved.
    /// </summary>
    Task<Error?> TrySaveChangesAsync(CancellationToken cancellationToken);
}
