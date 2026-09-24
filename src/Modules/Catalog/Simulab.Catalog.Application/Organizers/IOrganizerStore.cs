using Simulab.Catalog.Domain.Entities;

namespace Simulab.Catalog.Application.Organizers;

/// <summary>
/// What the organizer handlers need from the database (F-33). The port exists because the rule that
/// needs data — uniqueness (BR9) — lives in a handler, and this project never references EF Core.
/// </summary>
public interface IOrganizerStore
{
    /// <summary>The organizer with this id, or null when it does not exist or was deleted.</summary>
    Task<Organizer?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// True when another organizer already holds this normalized name (BR9). Deleted rows count:
    /// their names stay taken, exactly as the unique index sees them.
    /// </summary>
    Task<bool> NameIsTakenAsync(string normalizedName, Guid? exceptId, CancellationToken cancellationToken);

    /// <summary>True when another organizer already holds this normalized acronym (BR9).</summary>
    Task<bool> AcronymIsTakenAsync(string normalizedAcronym, Guid? exceptId, CancellationToken cancellationToken);

    void Add(Organizer organizer);

    /// <summary>Soft-deletes the organizer (BR11): the interceptor turns the removal into a flag.</summary>
    void Remove(Organizer organizer);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
