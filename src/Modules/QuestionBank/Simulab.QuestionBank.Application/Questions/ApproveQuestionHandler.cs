using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>
/// Approves a question in review: it is published exactly as a direct publish does, with the version F-98 makes,
/// and the Approved entry holds the optional note (F-105, UC4, BR5). The status, the version and the entry are one
/// save, so a lost race (BR11) leaves all three undone. The note also serves as the reason of the version when the
/// answer key differs from the latest version's (design trade-off 1).
/// </summary>
public sealed class ApproveQuestionHandler(IQuestionStore store, IQuestionQueries queries)
{
    public async Task<Result<QuestionResponse>> HandleAsync(
        Guid id,
        string? note,
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

        var approved = question.Approve(note);
        if (approved.IsFailure)
        {
            return Result.Failure<QuestionResponse>(approved.Error!);
        }

        var versioned = await QuestionPublication.AddVersionAsync(store, question, approved.Value!.Reason, cancellationToken);
        if (versioned.IsFailure)
        {
            return Result.Failure<QuestionResponse>(versioned.Error!);
        }

        store.AddReviewEntry(approved.Value!);

        if (await store.TrySaveChangesAsync(cancellationToken) is { } refused)
        {
            return Result.Failure<QuestionResponse>(refused);
        }

        return await ReviewAnswer.ReadAsync(queries, id, cancellationToken);
    }
}
