using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>
/// Takes an annulled question back to published with a reason and records the version that says so
/// (F-101, UC2, BR6). The status and the version are one save, as the annulment's are.
/// </summary>
public sealed class ReinstateQuestionHandler(IQuestionStore store, IQuestionQueries queries)
{
    public async Task<Result<QuestionResponse>> HandleAsync(
        Guid id,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var question = await store.FindAsync(id, cancellationToken);
        if (question is null)
        {
            return Result.Failure<QuestionResponse>(new Error(QuestionBankErrorCodes.QuestionNotFound, ErrorKind.NotFound));
        }

        var latest = await store.FindLatestVersionAsync(id, cancellationToken);
        var reinstated = question.Reinstate(latest, reason);
        if (reinstated.IsFailure)
        {
            return Result.Failure<QuestionResponse>(reinstated.Error!);
        }

        store.AddVersion(reinstated.Value!);

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
