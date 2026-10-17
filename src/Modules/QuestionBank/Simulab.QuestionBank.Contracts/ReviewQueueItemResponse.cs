namespace Simulab.QuestionBank.Contracts;

/// <summary>One row of the review queue (F-105, UC3, BR8).</summary>
/// <param name="Id">The question's id; the review screen opens by it.</param>
/// <param name="StatementExcerpt">The statement without tags, cut as the question list cuts it.</param>
/// <param name="ContentLanguage">The language the question is written in.</param>
/// <param name="Type">How the question is answered.</param>
/// <param name="TopicName">The topic's name, read from the catalog.</param>
/// <param name="SubjectName">The topic's subject name, read from the catalog.</param>
/// <param name="Difficulty">1 to 5, or null when not set.</param>
/// <param name="SubmittedAt">When the latest send happened; the queue is ordered by it, oldest first.</param>
public sealed record ReviewQueueItemResponse(
    Guid Id,
    string StatementExcerpt,
    string ContentLanguage,
    QuestionType Type,
    string TopicName,
    string SubjectName,
    int? Difficulty,
    DateTimeOffset SubmittedAt);
