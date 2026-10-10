namespace Simulab.QuestionBank.Domain;

/// <summary>
/// A text field after the Application sanitized it (F-95, BR8): the HTML that is stored and its plain text.
/// The sanitizer parses the document once and hands both over, so one parser decides what "empty" means
/// (BR3) and the Domain only judges length and emptiness.
/// </summary>
/// <param name="Html">The sanitized HTML, empty when nothing was sent.</param>
/// <param name="PlainText">The text content of <paramref name="Html"/>, whitespace collapsed; empty when it is only empty tags.</param>
public sealed record SanitizedHtml(string Html, string PlainText)
{
    /// <summary>A field that was not sent.</summary>
    public static SanitizedHtml Empty { get; } = new(string.Empty, string.Empty);

    public bool IsEmpty => PlainText.Length == 0;
}
