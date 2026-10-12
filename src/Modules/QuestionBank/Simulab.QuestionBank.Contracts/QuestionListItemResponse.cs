namespace Simulab.QuestionBank.Contracts;

/// <summary>One row of the question list (F-95, UC5).</summary>
/// <param name="Id">The question.</param>
/// <param name="StatementExcerpt">The plain-text statement cut at <see cref="QuestionLimits.ExcerptLength"/> characters (BR10).</param>
/// <param name="TopicId">The topic.</param>
/// <param name="TopicName">The topic's name.</param>
/// <param name="SubjectId">The subject the topic sits under.</param>
/// <param name="SubjectName">The subject's name.</param>
/// <param name="Difficulty">From 1 to 5, or null.</param>
/// <param name="ContentLanguage">The language the question is written in.</param>
/// <param name="Status">Draft or Published.</param>
/// <param name="Origin">Where the question came from, or null when it is authored (F-96).</param>
public sealed record QuestionListItemResponse(
    Guid Id,
    string StatementExcerpt,
    Guid TopicId,
    string TopicName,
    Guid SubjectId,
    string SubjectName,
    int? Difficulty,
    string ContentLanguage,
    QuestionStatus Status,
    QuestionOriginResponse? Origin = null);
