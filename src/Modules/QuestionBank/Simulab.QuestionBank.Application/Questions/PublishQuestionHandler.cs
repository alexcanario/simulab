using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>
/// Publishes a complete draft (F-95, UC3, BR4, BR12). It also records the version the publication makes
/// (F-98, BR2, BR3): version 1 the first time, and version N+1 when the content differs from the latest
/// version; a key that differs from it needs the reason.
/// </summary>
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

        // A refusal here happens before the save, so the question stays a draft.
        var latest = await store.FindLatestVersionAsync(id, cancellationToken);
        // F-102 BR5: the version snapshots the passage as it is now; the read holds a shared lock until the save,
        // so an edit of the text cannot commit between this read and the version.
        var baseText = question.BaseTextId is { } baseTextId
            ? await store.FindBaseTextAsync(baseTextId, cancellationToken)
            : null;
        var next = question.NextVersion(latest, changeReason, baseText?.TextHtml);
        if (next.IsFailure)
        {
            return Result.Failure<QuestionResponse>(next.Error!);
        }

        if (next.Value is { } version)
        {
            store.AddVersion(version);
        }

        // The status and the version are one save: a lost race for the number (F-98 BR10) leaves both undone.
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
