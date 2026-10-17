using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>
/// Rejects a question in review: it is a draft again and the Rejected entry holds the reason (F-105, UC5, BR6).
/// The status and the entry are one save.
/// </summary>
public sealed class RejectQuestionHandler(IQuestionStore store, IQuestionQueries queries)
{
    public async Task<Result<QuestionResponse>> HandleAsync(
        Guid id,
        string? reason,
        DateTimeOffset? submittedAt = null,
        CancellationToken cancellationToken = default)
    {
        var question = await store.FindAsync(id, cancellationToken);
        if (question is null)
        {
            return Result.Failure<QuestionResponse>(new Error(QuestionBankErrorCodes.QuestionNotFound, ErrorKind.NotFound));
        }

        if (question.Status == QuestionStatus.InReview
            && await ReviewAnswer.SendDiffersAsync(queries, id, submittedAt, cancellationToken))
        {
            return Result.Failure<QuestionResponse>(ReviewAnswer.VersionConflict());
        }

        var rejected = question.Reject(reason);
        if (rejected.IsFailure)
        {
            return Result.Failure<QuestionResponse>(rejected.Error!);
        }

        store.AddReviewEntry(rejected.Value!);

        if (await store.TrySaveChangesAsync(cancellationToken) is { } refused)
        {
            return Result.Failure<QuestionResponse>(refused);
        }

        return await ReviewAnswer.ReadAsync(queries, id, cancellationToken);
    }
}
