using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.Exams;

/// <summary>
/// What the exam handlers need from the database (F-34). The port exists because the rules that need
/// data — the name already taken inside the issuing authority (BR10) and the parent that must exist
/// (BR11) — live in a handler, and this project never references EF Core.
/// </summary>
public interface IExamStore
{
    /// <summary>The exam with this id, or null when it does not exist or was deleted.</summary>
    Task<Exam?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>True when an issuing authority with this id exists and was not deleted (BR11, v2).</summary>
    Task<bool> IssuingAuthorityExistsAsync(Guid issuingAuthorityId, CancellationToken cancellationToken);

    /// <summary>
    /// True when another exam of the same issuing authority already holds this normalized name (BR10).
    /// Deleted rows count: their names stay taken, exactly as the unique index sees them.
    /// </summary>
    Task<bool> NameIsTakenAsync(Guid issuingAuthorityId, string normalizedName, Guid? exceptId, CancellationToken cancellationToken);

    /// <summary>True when the issuing authority has at least one exam that was not deleted (BR12, v2).</summary>
    Task<bool> IssuingAuthorityHasExamsAsync(Guid issuingAuthorityId, CancellationToken cancellationToken);

    void Add(Exam exam);

    /// <summary>Soft-deletes the exam (BR1): the interceptor turns the removal into a flag.</summary>
    void Remove(Exam exam);

    /// <summary>
    /// Saves, and answers with the 409 for a duplicate name when the unique index refuses the row after
    /// the "is taken" check passed: another writer committed in between (the B-14 lesson). Null when saved.
    /// </summary>
    Task<Error?> TrySaveChangesAsync(CancellationToken cancellationToken);
}
