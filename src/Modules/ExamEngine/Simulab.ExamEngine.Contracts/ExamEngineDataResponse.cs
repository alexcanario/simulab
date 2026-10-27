using System.Text.Json.Serialization;

namespace Simulab.ExamEngine.Contracts;

/// <summary>
/// The module's section of the file a user downloads (F-167 BR7, BR8): every attempt the user started, oldest first.
/// <see cref="Attempts"/> is an empty list, never missing, when there is none. Its own records, not the simulator's
/// <see cref="AttemptResponse"/>: a member added for the screen must not enter this file unasked.
/// </summary>
public sealed record ExamEngineDataResponse(IReadOnlyList<ExportedAttemptResponse> Attempts);

/// <summary>
/// One attempt as the student left it, with its questions in paper-number order. No statement, option text, answer key,
/// model answer, explanation or tenant id (BR7). <see cref="SubmittedAt"/> is written as <c>null</c> while in progress.
/// </summary>
public sealed record ExportedAttemptResponse(
    Guid Id,
    Guid ExamEditionId,
    AttemptStatus Status,
    DateTimeOffset StartedAt,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] DateTimeOffset? SubmittedAt,
    IReadOnlyList<ExportedAttemptQuestionResponse> Questions);

/// <summary>
/// One question of an exported attempt and the answer exactly as saved, its <c>type</c> member included;
/// <see cref="Answer"/> is written as <c>null</c> while the question is blank (BR7).
/// </summary>
public sealed record ExportedAttemptQuestionResponse(
    Guid QuestionId,
    int PaperNumber,
    int VersionNumber,
    AttemptQuestionType Type,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] AnswerPayload? Answer);
