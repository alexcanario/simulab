using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>
/// What saving a question answers: the stored question, or the refusal and, when the refusal belongs to one
/// option, its position (F-95 decision of 2026-10-10). <c>Error</c> carries no position, so the endpoint adds
/// it to the problem as an extension from here.
/// </summary>
/// <param name="Question">The stored question; null on a refusal.</param>
/// <param name="Error">The refusal; null on success.</param>
/// <param name="OptionPosition">
/// The position, from 0, of the option or left item a refusal is about, when there is one; for the
/// <c>question.blank_*</c> codes it is the blank number, 1 to 99 (F-107, architect #4).
/// </param>
public sealed record SaveQuestionOutcome(QuestionResponse? Question, Error? Error = null, int? OptionPosition = null)
{
    public bool IsSuccess => Error is null;
}
