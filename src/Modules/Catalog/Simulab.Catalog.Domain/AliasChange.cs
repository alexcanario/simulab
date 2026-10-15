namespace Simulab.Catalog.Domain;

/// <summary>
/// What replacing a stored set of other names by a requested one comes to (F-77, BR4), by normalized form: the
/// stored rows to remove, the kept rows whose typed form changed, and the names to add. A row that stays is never
/// removed and added again: the soft delete is an UPDATE and the partial unique index could refuse the INSERT first.
/// </summary>
/// <typeparam name="T">The alias row.</typeparam>
/// <param name="Remove">Stored rows whose normalized form is not in the requested set.</param>
/// <param name="Rename">Stored rows that stay, with the typed form the request gave them.</param>
/// <param name="Add">Requested names with no stored row.</param>
public sealed record AliasChange<T>(
    IReadOnlyList<T> Remove,
    IReadOnlyList<(T Alias, string Name)> Rename,
    IReadOnlyList<AliasText> Add)
    where T : IAlias;
