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
/// <param name="OptionPosition">The position of the option a refusal is about, when there is one.</param>
public sealed record SaveQuestionOutcome(QuestionResponse? Question, Error? Error = null, int? OptionPosition = null)
{
    public bool IsSuccess => Error is null;
}
