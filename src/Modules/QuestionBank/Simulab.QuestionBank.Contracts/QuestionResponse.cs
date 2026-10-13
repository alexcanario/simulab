namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// One question with its options and key, as stored (F-95). The topic and subject travel with their names so
/// one call draws the form. Every text is the sanitized HTML that was stored (BR8). <c>CurrentVersion</c> is
/// the latest version number (F-98, BR11), null while the question was never published.
/// </summary>
public sealed record QuestionResponse(
    Guid Id,
    QuestionType Type,
    QuestionStatus Status,
    Guid TopicId,
    string TopicName,
    Guid SubjectId,
    string SubjectName,
    string ContentLanguage,
    string StatementHtml,
    string? BaseTextHtml,
    string? ExplanationHtml,
    int? Difficulty,
    IReadOnlyList<QuestionOptionResponse> Options,
    QuestionOriginResponse? Origin = null,
    bool? TrueFalseKey = null,
    int? CurrentVersion = null);
