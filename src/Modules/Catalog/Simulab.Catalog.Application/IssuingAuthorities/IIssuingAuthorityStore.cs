using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.IssuingAuthorities;

/// <summary>
/// What the issuing-authority handlers need from the database (F-34 BR18, v2). The port exists because the
/// rules that need data — uniqueness, and the exams that hold a body back — live in a handler, and this
/// project never references EF Core.
/// </summary>
public interface IIssuingAuthorityStore
{
    /// <summary>The body with this id, or null when it does not exist or was deleted.</summary>
    Task<IssuingAuthority?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// True when another body already holds this normalized name. Deleted rows count: their names stay
    /// taken, exactly as the unique index sees them.
    /// </summary>
    Task<bool> NameIsTakenAsync(string normalizedName, Guid? exceptId, CancellationToken cancellationToken);

    /// <summary>True when another body already holds this normalized acronym.</summary>
    Task<bool> AcronymIsTakenAsync(string normalizedAcronym, Guid? exceptId, CancellationToken cancellationToken);

    void Add(IssuingAuthority authority);

    /// <summary>Soft-deletes the body: the interceptor turns the removal into a flag.</summary>
    void Remove(IssuingAuthority authority);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Saves, and answers with the 409 for a duplicate name or acronym when the unique index refuses the row
    /// after the "is taken" checks passed: another writer committed in between (the B-14 lesson).
    /// </summary>
    Task<Error?> TrySaveChangesAsync(CancellationToken cancellationToken);
}
