namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// What the base text form sends, for a create and for an update (F-102, UC1 and UC2). The text is HTML written
/// by a person; the Api sanitizes it (BR8).
/// </summary>
/// <param name="Title">What the curator calls the text, 1 to 200 characters after trimming (BR1).</param>
/// <param name="ContentLanguage">One of the app's languages, canonicalized (BR1). Required.</param>
/// <param name="TextHtml">The text. Required, not empty once its tags are removed, up to 20,000 characters (BR1).</param>
public sealed record SaveBaseTextRequest(
    string? Title = null,
    string? ContentLanguage = null,
    string? TextHtml = null);
