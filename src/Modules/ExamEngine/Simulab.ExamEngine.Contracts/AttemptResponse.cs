namespace Simulab.ExamEngine.Contracts;

/// <summary>
/// An attempt with its questions in paper-number order and the student's own answers so far (F-166, UC4). It never
/// carries a statement, an answer key, a rubric's model answer or an explanation (BR8). <c>SubmittedAt</c> is null
/// while the attempt is in progress.
/// </summary>
public sealed record AttemptResponse(
    Guid Id,
    Guid ExamEditionId,
    AttemptStatus Status,
    DateTimeOffset StartedAt,
    IReadOnlyList<AttemptQuestionResponse> Questions,
    DateTimeOffset? SubmittedAt = null);
