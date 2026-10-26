using Simulab.ExamEngine.Contracts;
using Simulab.ExamEngine.Domain.Entities;

namespace Simulab.ExamEngine.Application.Attempts;

/// <summary>Turns an attempt into what the routes answer: ids, numbers, versions, types and the student's own answers (BR8).</summary>
internal static class AttemptMapper
{
    public static AttemptResponse ToResponse(Attempt attempt) =>
        new(
            attempt.Id,
            attempt.ExamEditionId,
            attempt.Status,
            attempt.StartedAt,
            [.. attempt.Answers.OrderBy(answer => answer.PaperNumber).Select(ToResponse)],
            attempt.SubmittedAt);

    public static AttemptQuestionResponse ToResponse(AttemptAnswer answer) =>
        new(answer.QuestionId, answer.PaperNumber, answer.VersionNumber, answer.QuestionType, answer.Answer);
}
