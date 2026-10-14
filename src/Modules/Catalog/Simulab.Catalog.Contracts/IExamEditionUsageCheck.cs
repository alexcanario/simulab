namespace Simulab.Catalog.Contracts;

/// <summary>
/// A module that holds a reference to an exam edition says, through one of these, which of them it still
/// uses (F-96, BR8). The catalog asks every registered check before it deletes an edition, so a module that
/// adds a reference adds its own check and never edits the catalog.
/// </summary>
public interface IExamEditionUsageCheck
{
    /// <summary>The ids among <paramref name="examEditionIds"/> that this module still uses with a live row.</summary>
    Task<IReadOnlySet<Guid>> InUseAsync(IReadOnlyCollection<Guid> examEditionIds, CancellationToken cancellationToken);
}
