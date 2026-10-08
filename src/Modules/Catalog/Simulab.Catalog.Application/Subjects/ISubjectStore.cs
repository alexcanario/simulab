using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.Subjects;

/// <summary>
/// What the subject handlers need from the database (F-79). The port exists because the rules that need
/// data — the area exists, the name is free — live in a handler, and this project never references EF Core.
/// </summary>
public interface ISubjectStore
{
    /// <summary>The subject with this id, or null when it does not exist or was deleted.</summary>
    Task<Subject?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>True when an area with this id is in the seeded list (BR4).</summary>
    Task<bool> AreaExistsAsync(Guid areaId, CancellationToken cancellationToken);

    /// <summary>
    /// True when another subject already holds this normalized name. Deleted rows count: their names stay
    /// taken, exactly as the unique index sees them (BR5).
    /// </summary>
    Task<bool> NameIsTakenAsync(string normalizedName, Guid? exceptId, CancellationToken cancellationToken);

    void Add(Subject subject);

    /// <summary>Soft-deletes the subject: the interceptor turns the removal into a flag.</summary>
    void Remove(Subject subject);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Saves, and answers with the 409 for a duplicate name when the unique index refuses the row after the
    /// "is taken" check passed: another writer committed in between (the B-14 lesson).
    /// </summary>
    Task<Error?> TrySaveChangesAsync(CancellationToken cancellationToken);
}
