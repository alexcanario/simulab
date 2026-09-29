using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Components.Pages.Catalog;

/// <summary>
/// The seven keys of <c>/catalog</c> and how they are read and written (F-36, BR13, BR6). Reading never fails: a value
/// that cannot be read is no filter, so a stale bookmark shows results, not an error. Writing leaves out every key at
/// its default, so the plain list is just <c>/catalog</c>.
/// </summary>
public static class CatalogQueryString
{
    public const string SearchKey = "search";
    public const string AssessmentTypeKey = "assessmentType";
    public const string ScopeKey = "scope";
    public const string OrganizerIdKey = "organizerId";
    public const string NoticeYearKey = "noticeYear";
    public const string PageKey = "page";
    public const string PageSizeKey = "pageSize";

    private static readonly string[] Keys =
        [SearchKey, AssessmentTypeKey, ScopeKey, OrganizerIdKey, NoticeYearKey, PageKey, PageSizeKey];

    /// <summary>The state an address holds; an address with none of the keys gives the plain list.</summary>
    public static CatalogSearchState Parse(string uri)
    {
        var values = Read(uri);
        var search = values.GetValueOrDefault(SearchKey)?.Trim();
        var page = int.TryParse(values.GetValueOrDefault(PageKey), NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number >= 1
            ? number - 1
            : 0;
        var pageSize = int.TryParse(values.GetValueOrDefault(PageSizeKey), NumberStyles.None, CultureInfo.InvariantCulture, out var size)
            && AppDataTable<object>.PageSizes.Contains(size)
                ? size
                : CatalogSearchState.DefaultPageSize;

        return new CatalogSearchState(
            string.IsNullOrWhiteSpace(search) ? null : search,
            ParseEnum<AssessmentType>(values.GetValueOrDefault(AssessmentTypeKey)),
            ParseEnum<ExamScope>(values.GetValueOrDefault(ScopeKey)),
            Guid.TryParse(values.GetValueOrDefault(OrganizerIdKey), out var organizerId) ? organizerId : null,
            int.TryParse(values.GetValueOrDefault(NoticeYearKey), NumberStyles.None, CultureInfo.InvariantCulture, out var year) ? year : null,
            page,
            pageSize);
    }

    /// <summary>The query string of a state ("?a=b&amp;c=d"), or an empty string when every key is at its default.</summary>
    public static string Build(CatalogSearchState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        List<string> parts = [];
        if (!string.IsNullOrWhiteSpace(state.Search))
        {
            parts.Add($"{SearchKey}={Uri.EscapeDataString(state.Search.Trim())}");
        }

        if (state.AssessmentType is { } type)
        {
            parts.Add($"{AssessmentTypeKey}={type}");
        }

        if (state.Scope is { } scope)
        {
            parts.Add($"{ScopeKey}={scope}");
        }

        if (state.OrganizerId is { } organizerId)
        {
            parts.Add($"{OrganizerIdKey}={organizerId}");
        }

        if (state.NoticeYear is { } year)
        {
            parts.Add($"{NoticeYearKey}={year.ToString(CultureInfo.InvariantCulture)}");
        }

        if (state.Page > 0)
        {
            parts.Add($"{PageKey}={(state.Page + 1).ToString(CultureInfo.InvariantCulture)}");
        }

        if (state.PageSize != CatalogSearchState.DefaultPageSize)
        {
            parts.Add($"{PageSizeKey}={state.PageSize.ToString(CultureInfo.InvariantCulture)}");
        }

        return parts.Count == 0 ? string.Empty : "?" + string.Join('&', parts);
    }

    /// <summary>
    /// Only the seven keys of an address, as a query string: what the exam page carries so its links go back to the list
    /// as it was. Anything else in the address is dropped.
    /// </summary>
    public static string Carried(string uri) => Build(Parse(uri));

    private static Dictionary<string, string> Read(string uri)
    {
        var start = uri.IndexOf('?', StringComparison.Ordinal);
        if (start < 0)
        {
            return [];
        }

        var end = uri.IndexOf('#', start);
        var query = end < 0 ? uri[start..] : uri[start..end];
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in QueryHelpers.ParseQuery(query))
        {
            if (Keys.Contains(pair.Key, StringComparer.OrdinalIgnoreCase) && pair.Value.Count > 0)
            {
                values.TryAdd(pair.Key, pair.Value[0] ?? string.Empty);
            }
        }

        return values;
    }

    // An enum name, in any case. A number is not a name: Enum.TryParse would accept "1" and "99".
    private static TEnum? ParseEnum<TEnum>(string? value)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var name = Enum.GetNames<TEnum>().FirstOrDefault(candidate => candidate.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
        return name is null ? null : Enum.Parse<TEnum>(name);
    }
}
