namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// One question with its options and key, as stored (F-95). The topic and subject travel with their names so
/// one call draws the form. Every text is the sanitized HTML that was stored (BR8). <c>CurrentVersion</c> is
/// the latest version number (F-98, BR11), null while the question was never published. <c>Annulment</c> says
/// why and when, and is null unless the status is <see cref="QuestionStatus.Annulled"/> (F-101). <c>SubmittedAt</c> is the time of the latest send for review, set only while
/// the status is <see cref="QuestionStatus.InReview"/> (F-105). <c>MatchItems</c> are the left items of a matching
/// question in order (F-106); the Api always sends the list, empty for the other types. <c>Blanks</c> are the blanks
/// of a fill-in-the-blanks question in the order their markers appear in the statement (F-107); the Api always sends
/// the list, empty for the other types.
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
    QuestionBaseTextResponse? BaseText,
    string? ExplanationHtml,
    int? Difficulty,
    IReadOnlyList<QuestionOptionResponse> Options,
    QuestionOriginResponse? Origin = null,
    bool? TrueFalseKey = null,
    int? CurrentVersion = null,
    QuestionAnnulmentResponse? Annulment = null,
    DateTimeOffset? SubmittedAt = null,
    IReadOnlyList<MatchItemResponse>? MatchItems = null,
    IReadOnlyList<BlankResponse>? Blanks = null);
