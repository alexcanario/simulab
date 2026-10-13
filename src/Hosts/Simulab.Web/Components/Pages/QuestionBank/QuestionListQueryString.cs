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

    /// <summary>The state an address holds; an address with none of the keys gives the plain list.</summary>
    public static QuestionListState Parse(string uri) => throw new NotImplementedException();

    /// <summary>The query string of a state ("?a=b&amp;c=d"), or an empty string when every key is at its default.</summary>
    public static string Build(QuestionListState state) => throw new NotImplementedException();

    /// <summary>Only the keys of an address, as a query string: what the form carries so its links go back to the list as it was.</summary>
    public static string Carried(string uri) => throw new NotImplementedException();
}
