using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>Takes a question back out of review; it is a draft again (F-105, UC2, BR4). Any holder of the manage permission may.</summary>
public sealed class WithdrawQuestionHandler(IQuestionStore store, IQuestionQueries queries)
{
    public async Task<Result<QuestionResponse>> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var question = await store.FindAsync(id, cancellationToken);
        if (question is null)
        {
            return Result.Failure<QuestionResponse>(new Error(QuestionBankErrorCodes.QuestionNotFound, ErrorKind.NotFound));
        }

        var withdrawn = question.Withdraw();
        if (withdrawn.IsFailure)
        {
            return Result.Failure<QuestionResponse>(withdrawn.Error!);
        }

        store.AddReviewEntry(withdrawn.Value!);

        if (await store.TrySaveChangesAsync(cancellationToken) is { } refused)
        {
            return Result.Failure<QuestionResponse>(refused);
        }

        return await ReviewAnswer.ReadAsync(queries, id, cancellationToken);
    }
}
