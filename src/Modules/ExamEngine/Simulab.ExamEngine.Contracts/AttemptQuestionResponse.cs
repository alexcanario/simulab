namespace Simulab.ExamEngine.Contracts;

/// <summary>
/// One question of an attempt, fixed at the start (F-166, BR2): its id, its number in the paper, the number of the
/// version the student saw and its type. <c>Answer</c> is null while the question is blank.
/// </summary>
public sealed record AttemptQuestionResponse(
    Guid QuestionId,
    int PaperNumber,
    int VersionNumber,
    AttemptQuestionType Type,
    AnswerPayload? Answer = null);
