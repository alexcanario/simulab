namespace Simulab.QuestionBank.Contracts;

/// <summary>One row of the base text list and of the picker (F-102, BR11).</summary>
/// <param name="Id">The base text.</param>
/// <param name="Title">The title the curator gave it.</param>
/// <param name="ContentLanguage">The language the text is written in.</param>
/// <param name="QuestionCount">How many live questions, of any status, link it.</param>
/// <param name="UpdatedAt">When it was last changed, or when it was created.</param>
public sealed record BaseTextListItemResponse(
    Guid Id,
    string Title,
    string ContentLanguage,
    int QuestionCount,
    DateTimeOffset UpdatedAt);
