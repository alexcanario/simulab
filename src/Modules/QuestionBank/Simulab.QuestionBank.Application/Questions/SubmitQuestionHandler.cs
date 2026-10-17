using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>
/// Sends a complete draft for review (F-105, UC1, BR1, BR2): the status and the Submitted entry are one save, so a
/// lost race (BR11) leaves both undone.
/// </summary>
public sealed class SubmitQuestionHandler(IQuestionStore store, IQuestionQueries queries)
{
    public async Task<Result<QuestionResponse>> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var question = await store.FindAsync(id, cancellationToken);
        if (question is null)
        {
            return Result.Failure<QuestionResponse>(new Error(QuestionBankErrorCodes.QuestionNotFound, ErrorKind.NotFound));
        }

        var submitted = question.Submit();
        if (submitted.IsFailure)
        {
            return Result.Failure<QuestionResponse>(submitted.Error!);
        }

        store.AddReviewEntry(submitted.Value!);

        if (await store.TrySaveChangesAsync(cancellationToken) is { } refused)
        {
            return Result.Failure<QuestionResponse>(refused);
        }

        return await ReviewAnswer.ReadAsync(queries, id, cancellationToken);
    }
}
