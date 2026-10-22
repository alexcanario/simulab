namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// The syntax of a blank marker (F-107, BR3): <c>[[n]]</c>, <c>n</c> a whole number from 1 to 99 with no leading zero,
/// written as one run of text. The Api (the authority) and the Web (comfort) read markers with this one pattern, and
/// only in text between tags: a marker split by formatting, or inside an attribute, is plain text.
/// </summary>
public static class BlankMarker
{
    /// <summary>The regular expression of one marker; group 1 is the number.</summary>
    public const string Pattern = @"\[\[([1-9][0-9]?)\]\]";

    /// <summary>What a marker reads as in the plain text of the statement: the list excerpt and the search (BR9).</summary>
    public const string PlainText = "____";

    /// <summary>The marker text of blank <paramref name="number"/>.</summary>
    public static string For(int number) => $"[[{number}]]";
}
