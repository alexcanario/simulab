namespace Simulab.Catalog.Contracts;

/// <summary>
/// Value equality for the alias lists the contract records carry (F-77). A record compares an
/// <see cref="IReadOnlyList{T}"/> by reference, which would make two equal responses differ and break every
/// test and screen that compares a request or a response as a value.
/// </summary>
internal static class AliasLists
{
    /// <summary>True when both lists are null, or both hold the same names in the same order.</summary>
    public static bool Equal(IReadOnlyList<string>? left, IReadOnlyList<string>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        return left is not null && right is not null && left.SequenceEqual(right, StringComparer.Ordinal);
    }

    /// <summary>A hash that agrees with <see cref="Equal"/>.</summary>
    public static int Hash(IReadOnlyList<string>? list)
    {
        var hash = new HashCode();
        if (list is null)
        {
            return hash.ToHashCode();
        }

        foreach (var name in list)
        {
            hash.Add(name, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }
}
