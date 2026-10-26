using Simulab.ExamEngine.Contracts;
using Simulab.SharedKernel.Entities;

namespace Simulab.ExamEngine.Domain.Entities;

/// <summary>
/// One question of an attempt and the answer to it (F-166, BR2, BR4): the question id, its paper number, the number of
/// the version the student saw and its type are fixed at the start; <see cref="Answer"/> is null while the question is
/// blank, and saving or clearing only ever changes that.
/// </summary>
public sealed class AttemptAnswer : TenantEntity
{
    private AttemptAnswer()
    {
    }

    public Guid AttemptId { get; private set; }

    public Guid QuestionId { get; private set; }

    public int PaperNumber { get; private set; }

    public int VersionNumber { get; private set; }

    public AttemptQuestionType QuestionType { get; private set; }

    /// <summary>The answer saved, or null while the question is blank.</summary>
    public AnswerPayload? Answer { get; private set; }

    internal static AttemptAnswer Blank(Guid attemptId, Guid? tenantId, AttemptQuestion question)
    {
        var row = new AttemptAnswer
        {
            AttemptId = attemptId,
            QuestionId = question.QuestionId,
            PaperNumber = question.PaperNumber,
            VersionNumber = question.VersionNumber,
            QuestionType = question.Type
        };
        row.TenantId = tenantId;

        return row;
    }

    internal void Set(AnswerPayload? answer) => Answer = answer;
}
