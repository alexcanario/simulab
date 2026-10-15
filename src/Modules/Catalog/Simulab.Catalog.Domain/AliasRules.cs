namespace Simulab.Catalog.Domain;

/// <summary>
/// The rules of a requested set of other names that need no database (F-77): a name typed twice counts once,
/// the first as typed kept (BR4). The shape of each name — its length and its error code — belongs to the
/// alias entities; whether the text is free needs the tables, so the handler asks the store for it.
/// </summary>
public static class AliasRules
{
    /// <summary>
    /// Trims every name and collapses the ones with the same normalized form, in the order of first appearance.
    /// A null list is empty and a null or blank entry stays, so the entity can refuse it as too short.
    /// </summary>
    public static IReadOnlyList<AliasText> Distinct(IReadOnlyList<string?>? requested)
    {
        if (requested is null or { Count: 0 })
        {
            return [];
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var distinct = new List<AliasText>(requested.Count);
        foreach (var name in requested)
        {
            var trimmed = name?.Trim() ?? string.Empty;
            var normalized = CatalogText.Normalize(trimmed);
            if (seen.Add(normalized))
            {
                distinct.Add(new AliasText(trimmed, normalized));
            }
        }

        return distinct;
    }

    /// <summary>
    /// Compares the stored rows with the requested set by normalized form: which rows go, which stay with a new
    /// typed form, and which names are new (BR4). The stored rows are unique by normalized form (the index).
    /// </summary>
    public static AliasChange<T> Diff<T>(IReadOnlyList<T> stored, IReadOnlyList<AliasText> wanted)
        where T : IAlias
    {
        ArgumentNullException.ThrowIfNull(stored);
        ArgumentNullException.ThrowIfNull(wanted);

        var wantedByForm = wanted.ToDictionary(text => text.NormalizedName, StringComparer.Ordinal);
        var storedForms = stored.Select(alias => alias.NormalizedName).ToHashSet(StringComparer.Ordinal);

        var remove = stored.Where(alias => !wantedByForm.ContainsKey(alias.NormalizedName)).ToList();
        var rename = stored
            .Where(alias => wantedByForm.TryGetValue(alias.NormalizedName, out var text) && text.Name != alias.Name)
            .Select(alias => (alias, wantedByForm[alias.NormalizedName].Name))
            .ToList();
        var add = wanted.Where(text => !storedForms.Contains(text.NormalizedName)).ToList();

        return new AliasChange<T>(remove, rename, add);
    }

    /// <summary>True when <paramref name="alias"/> is the same text as the owner's own name once normalized (BR5).</summary>
    public static bool IsSameAsName(AliasText alias, string ownerNormalizedName)
    {
        ArgumentNullException.ThrowIfNull(alias);

        return alias.NormalizedName == ownerNormalizedName;
    }
}
