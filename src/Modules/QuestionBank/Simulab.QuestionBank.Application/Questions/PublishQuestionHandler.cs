using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>Publishes a complete draft; the status only (F-95, UC3, BR4, BR12).</summary>
public sealed class PublishQuestionHandler(IQuestionStore store, IQuestionQueries queries)
{
    public async Task<Result<QuestionResponse>> HandleAsync(
        Guid id,
        string? changeReason = null,
        CancellationToken cancellationToken = default)
    {
        var question = await store.FindAsync(id, cancellationToken);
        if (question is null)
        {
            return Result.Failure<QuestionResponse>(new Error(QuestionBankErrorCodes.QuestionNotFound, ErrorKind.NotFound));
        }

        var published = question.Publish();
        if (published.IsFailure)
        {
            return Result.Failure<QuestionResponse>(published.Error!);
        }

        await store.SaveChangesAsync(cancellationToken);

        var saved = await queries.FindAsync(id, cancellationToken);

        return saved is null
            ? Result.Failure<QuestionResponse>(new Error(QuestionBankErrorCodes.QuestionNotFound, ErrorKind.NotFound))
            : Result.Success(saved);
    }
}
