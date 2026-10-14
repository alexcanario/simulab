using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>
/// Annuls a published question with a reason and records the version that says so (F-101, UC1, BR1 to BR4).
/// The status and the version are one save, so a lost race (BR7) leaves both undone.
/// </summary>
public sealed class AnnulQuestionHandler(IQuestionStore store, IQuestionQueries queries)
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
        var annulled = question.Annul(latest, reason);
        if (annulled.IsFailure)
        {
            return Result.Failure<QuestionResponse>(annulled.Error!);
        }

        store.AddVersion(annulled.Value!);

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
