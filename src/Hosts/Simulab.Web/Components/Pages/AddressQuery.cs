using Microsoft.AspNetCore.WebUtilities;

namespace Simulab.Web.Components.Pages;

/// <summary>
/// Reads the keys a page keeps in its address (F-36 BR13, F-100 BR8): every value of every key it knows, keys in
/// any case, the rest of the address ignored. The pages that own a state in the address write their own keys and
/// parse their own values; this is only the reading both have in common.
/// </summary>
internal static class AddressQuery
{
    /// <summary>
    /// The values of each of <paramref name="keys"/> found in the query of <paramref name="uri"/>, in the order
    /// they appear. A key that is absent has no entry; a value that is only a key (<c>?a</c>) is empty text.
    /// </summary>
    public static Dictionary<string, List<string>> Read(string uri, IEnumerable<string> keys)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(keys);

        var values = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var start = uri.IndexOf('?', StringComparison.Ordinal);
        if (start < 0)
        {
            return values;
        }

        var end = uri.IndexOf('#', start);
        var query = end < 0 ? uri[start..] : uri[start..end];
        var known = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);

        foreach (var pair in QueryHelpers.ParseQuery(query))
        {
            if (!known.Contains(pair.Key) || pair.Value.Count == 0)
            {
                continue;
            }

            if (!values.TryGetValue(pair.Key, out var list))
            {
                values[pair.Key] = list = [];
            }

            list.AddRange(pair.Value.Select(value => value ?? string.Empty));
        }

        return values;
    }
}
