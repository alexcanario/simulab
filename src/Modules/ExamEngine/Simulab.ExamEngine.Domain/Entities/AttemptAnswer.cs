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

    /// <summary>
    /// F-167 BR1: the texts of the answer become null, the shape and the blank numbers stay. The answer is assigned only
    /// when a text is there, so a row with nothing to erase is not written and a second call changes nothing (BR4).
    /// </summary>
    internal void EraseText()
    {
        AnswerPayload? erased = Answer switch
        {
            ShortAnswerPayload { Text: not null } shortAnswer => shortAnswer with { Text = null },
            OpenAnswerPayload { Text: not null } openAnswer => openAnswer with { Text = null },
            EssayPayload { Text: not null } essay => essay with { Text = null },
            FillInTheBlanksPayload { Blanks: { } blanks } fillIn when blanks.Any(blank => blank.Text is not null) =>
                fillIn with { Blanks = [.. blanks.Select(blank => blank with { Text = null })] },
            _ => null
        };

        if (erased is not null)
        {
            Answer = erased;
        }
    }
}
