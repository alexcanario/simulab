namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// One immutable snapshot of a published question (F-98, BR1): what students were shown and graded on. Every
/// text is the sanitized HTML that was stored (F-95, BR8). Topic, difficulty and origin are catalog data and
/// are not part of it.
/// </summary>
/// <param name="Id">The version's stable id.</param>
/// <param name="QuestionId">The question the version belongs to.</param>
/// <param name="Number">1, 2, ...; unique for the question.</param>
/// <param name="Type">How the question is answered.</param>
/// <param name="ContentLanguage">The language the content is written in.</param>
/// <param name="StatementHtml">The statement.</param>
/// <param name="BaseTextHtml">The base text, or null.</param>
/// <param name="ExplanationHtml">The explanation, or null.</param>
/// <param name="Options">The options in position order; empty for a true/false question.</param>
/// <param name="TrueFalseKey">The key of a true/false question; null for any other type.</param>
/// <param name="AnswerKeyChanged">True when the answer key differs from the previous version's (BR4).</param>
/// <param name="Reason">The reason the curator wrote, or null.</param>
/// <param name="CreatedAt">When the version was recorded.</param>
public sealed record QuestionVersionResponse(
    Guid Id,
    Guid QuestionId,
    int Number,
    QuestionType Type,
    string ContentLanguage,
    string StatementHtml,
    string? BaseTextHtml,
    string? ExplanationHtml,
    IReadOnlyList<QuestionOptionResponse> Options,
    bool? TrueFalseKey,
    bool AnswerKeyChanged,
    string? Reason,
    DateTimeOffset CreatedAt);
