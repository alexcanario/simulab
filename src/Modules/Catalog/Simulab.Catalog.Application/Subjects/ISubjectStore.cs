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

    /// <summary>True when a live notice subject maps this subject as a whole subject (F-75, BR9).</summary>
    Task<bool> IsMappedWholeAsync(Guid subjectId, CancellationToken cancellationToken);

    /// <summary>The live other names of the subject, tracked, so the handler can keep, rename or remove them (F-77).</summary>
    Task<IReadOnlyList<SubjectAlias>> ListAliasesAsync(Guid subjectId, CancellationToken cancellationToken);

    /// <summary>
    /// True when a live other name of another subject has this normalized form (F-77, BR6). Deleted aliases do
    /// not count: the text is free again (BR8).
    /// </summary>
    Task<bool> AliasIsTakenAsync(string normalizedName, Guid? exceptSubjectId, CancellationToken cancellationToken);

    void AddAlias(SubjectAlias alias);

    /// <summary>Soft-deletes the other name: the interceptor turns the removal into a flag.</summary>
    void RemoveAlias(SubjectAlias alias);

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
