namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// What the question form sends, for a create and for an update (F-95, UC1 and UC2). The type is implied:
/// single choice is the only one (BR2). Every text is HTML written by a person; the Api sanitizes it (BR8).
/// </summary>
/// <param name="TopicId">The topic the question belongs to. Required (BR3).</param>
/// <param name="ContentLanguage">One of the app's languages, canonicalized (BR7). Required.</param>
/// <param name="StatementHtml">What the student is asked. Required, not empty once its tags are removed (BR3).</param>
/// <param name="BaseTextHtml">A text the question refers to, shown before the statement. Optional.</param>
/// <param name="ExplanationHtml">Why the key is right. Optional.</param>
/// <param name="Difficulty">A whole number from 1 to 5, or null (BR6).</param>
/// <param name="Options">The options in order; null counts as none (BR3).</param>
public sealed record SaveQuestionRequest(
    Guid? TopicId = null,
    string? ContentLanguage = null,
    string? StatementHtml = null,
    string? BaseTextHtml = null,
    string? ExplanationHtml = null,
    int? Difficulty = null,
    IReadOnlyList<SaveQuestionOptionRequest>? Options = null);
