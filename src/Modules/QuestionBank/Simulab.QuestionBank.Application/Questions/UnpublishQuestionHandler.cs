using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>Takes a published question back to draft; the content is untouched (F-95, UC4, BR12).</summary>
public sealed class UnpublishQuestionHandler(IQuestionStore store, IQuestionQueries queries)
{
    public async Task<Result<QuestionResponse>> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var question = await store.FindAsync(id, cancellationToken);
        if (question is null)
        {
            return Result.Failure<QuestionResponse>(new Error(QuestionBankErrorCodes.QuestionNotFound, ErrorKind.NotFound));
        }

        var unpublished = question.Unpublish();
        if (unpublished.IsFailure)
        {
            return Result.Failure<QuestionResponse>(unpublished.Error!);
        }

        // F-101: the status is a concurrency token; an annulment that got there first answers 409.
        if (await store.TrySaveChangesAsync(cancellationToken) is { } refused)
        {
            return Result.Failure<QuestionResponse>(refused);
        }

        var saved = await queries.FindAsync(id, cancellationToken);

        return saved is null
            ? Result.Failure<QuestionResponse>(new Error(QuestionBankErrorCodes.QuestionNotFound, ErrorKind.NotFound))
            : Result.Success(saved);
    }
}
