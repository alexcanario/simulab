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
    /// <summary>The ids of the stored images the HTML holds, in order of appearance (F-103, BR6).</summary>
    public IReadOnlyList<Guid> ImageIds { get; init; } = [];

    /// <summary>How many images carry no <c>alt</c> attribute at all; an empty one marks a decorative image (F-103, BR4).</summary>
    public int ImagesWithoutAlt { get; init; }

    /// <summary>How many images carry an <c>alt</c> longer than <c>QuestionLimits.ImageAltMaxLength</c> (F-103, BR4).</summary>
    public int AltTooLong { get; init; }

    /// <summary>A field that was not sent.</summary>
    public static SanitizedHtml Empty { get; } = new(string.Empty, string.Empty);

    /// <summary>True when there is no text and no image: a text that holds only a picture is content (F-103, BR8).</summary>
    public bool IsEmpty => PlainText.Length == 0 && ImageIds.Count == 0;
}
