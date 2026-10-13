using System.Globalization;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Components.Pages.QuestionBank;

/// <summary>
/// How the address of <c>/admin/questions</c> is read and written (F-100, BR8). Reading never fails: a value that
/// cannot be read is no filter, so a stale bookmark shows results, not an error. Writing leaves out every key at
/// its default, so the plain list is just <c>/admin/questions</c>; a key with several values is repeated.
/// </summary>
public static class QuestionListQueryString
{
    public const string SearchKey = "search";
    public const string StatusKey = "status";
    public const string TypeKey = "type";
    public const string DifficultyKey = "difficulty";
    public const string SubjectKey = "subject";
    public const string TopicKey = "topic";
    public const string OrganizerKey = "organizer";
    public const string EditionKey = "edition";
    public const string PageKey = "page";
    public const string PageSizeKey = "pageSize";

    /// <summary>The word that stands for "a question with no difficulty" in the <c>difficulty</c> key.</summary>
    public const string DifficultyNone = "none";

    private static readonly string[] Keys =
        [SearchKey, StatusKey, TypeKey, DifficultyKey, SubjectKey, TopicKey, OrganizerKey, EditionKey, PageKey, PageSizeKey];

    /// <summary>The state an address holds; an address with none of the keys gives the plain list.</summary>
    public static QuestionListState Parse(string uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        var values = AddressQuery.Read(uri, Keys);
        List<string> Of(string key) => values.TryGetValue(key, out var found) ? found : [];
        string? First(string key) => Of(key) is [var first, ..] ? first : null;

        var search = First(SearchKey)?.Trim();
        var difficulties = new List<int>();
        var notSet = false;
        foreach (var value in Of(DifficultyKey))
        {
            if (string.Equals(value.Trim(), DifficultyNone, StringComparison.OrdinalIgnoreCase))
            {
                notSet = true;
            }
            else if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
                && number is >= 1 and <= 5
                && !difficulties.Contains(number))
            {
                difficulties.Add(number);
            }
        }

        var page = int.TryParse(First(PageKey), NumberStyles.None, CultureInfo.InvariantCulture, out var number1) && number1 >= 1
            ? number1 - 1
            : 0;
        var pageSize = int.TryParse(First(PageSizeKey), NumberStyles.None, CultureInfo.InvariantCulture, out var size)
            && AppDataTable<object>.PageSizes.Contains(size)
                ? size
                : AppDataTable<object>.DefaultPageSize;

        return new QuestionListState(
            string.IsNullOrWhiteSpace(search) ? null : search,
            NullIfEmpty(Enums<QuestionStatus>(Of(StatusKey))),
            NullIfEmpty(Enums<QuestionType>(Of(TypeKey))),
            NullIfEmpty(difficulties),
            notSet,
            NullIfEmpty(Guids(Of(SubjectKey))),
            NullIfEmpty(Guids(Of(TopicKey))),
            NullIfEmpty(Guids(Of(OrganizerKey))),
            Guid.TryParse(First(EditionKey), out var edition) ? edition : null,
            page,
            pageSize);
    }

    /// <summary>The query string of a state ("?a=b&amp;c=d"), or an empty string when every key is at its default.</summary>
    public static string Build(QuestionListState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        List<string> parts = [];
        if (!string.IsNullOrWhiteSpace(state.Search))
        {
            parts.Add($"{SearchKey}={Uri.EscapeDataString(state.Search.Trim())}");
        }

        parts.AddRange((state.Statuses ?? []).Select(status => $"{StatusKey}={status}"));
        parts.AddRange((state.Types ?? []).Select(type => $"{TypeKey}={type}"));
        parts.AddRange((state.Difficulties ?? []).Select(number => $"{DifficultyKey}={number.ToString(CultureInfo.InvariantCulture)}"));
        if (state.DifficultyNotSet)
        {
            parts.Add($"{DifficultyKey}={DifficultyNone}");
        }

        parts.AddRange((state.SubjectIds ?? []).Select(id => $"{SubjectKey}={id}"));
        parts.AddRange((state.TopicIds ?? []).Select(id => $"{TopicKey}={id}"));
        parts.AddRange((state.OrganizerIds ?? []).Select(id => $"{OrganizerKey}={id}"));
        if (state.ExamEditionId is { } edition)
        {
            parts.Add($"{EditionKey}={edition}");
        }

        if (state.Page > 0)
        {
            parts.Add($"{PageKey}={(state.Page + 1).ToString(CultureInfo.InvariantCulture)}");
        }

        if (state.PageSize != AppDataTable<object>.DefaultPageSize)
        {
            parts.Add($"{PageSizeKey}={state.PageSize.ToString(CultureInfo.InvariantCulture)}");
        }

        return parts.Count == 0 ? string.Empty : "?" + string.Join('&', parts);
    }

    /// <summary>
    /// Only the keys of an address, as a query string: what the form carries so its links go back to the list as
    /// it was. Anything else in the address is dropped.
    /// </summary>
    public static string Carried(string uri) => Build(Parse(uri));

    private static List<T>? NullIfEmpty<T>(List<T> values) => values.Count == 0 ? null : values;

    // An enum name, in any case. A number is not a name: Enum.TryParse would accept "1" and "99".
    private static List<TEnum> Enums<TEnum>(IEnumerable<string> values)
        where TEnum : struct, Enum
    {
        var names = Enum.GetNames<TEnum>();
        var result = new List<TEnum>();
        foreach (var value in values)
        {
            var name = names.FirstOrDefault(candidate => candidate.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
            if (name is not null && Enum.Parse<TEnum>(name) is var parsed && !result.Contains(parsed))
            {
                result.Add(parsed);
            }
        }

        return result;
    }

    private static List<Guid> Guids(IEnumerable<string> values)
    {
        var result = new List<Guid>();
        foreach (var value in values)
        {
            if (Guid.TryParse(value, out var id) && !result.Contains(id))
            {
                result.Add(id);
            }
        }

        return result;
    }
}
