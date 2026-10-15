namespace Simulab.QuestionBank.Contracts;

/// <summary>One base text as stored (F-102): the sanitized HTML and the number of live questions that link it.</summary>
/// <param name="Id">The base text.</param>
/// <param name="Title">The title the curator gave it.</param>
/// <param name="ContentLanguage">The language the text is written in.</param>
/// <param name="TextHtml">The sanitized HTML that was stored (BR8).</param>
/// <param name="QuestionCount">How many live questions, of any status, link it (BR6).</param>
/// <param name="UpdatedAt">When it was last changed, or when it was created.</param>
public sealed record BaseTextResponse(
    Guid Id,
    string Title,
    string ContentLanguage,
    string TextHtml,
    int QuestionCount,
    DateTimeOffset UpdatedAt);
