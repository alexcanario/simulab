using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>
/// What a publication writes besides the status (F-98, BR2, BR3): the version the content makes. A direct publish
/// and an approval (F-105, BR5) both end here, so "published" means one thing.
/// </summary>
internal static class QuestionPublication
{
    /// <summary>
    /// Adds the version a question just published makes, when its content differs from the latest one; the
    /// failure is the missing reason of a key change. The caller saves, in the same save as the status.
    /// </summary>
    public static async Task<Result> AddVersionAsync(
        IQuestionStore store,
        Question question,
        string? changeReason,
        CancellationToken cancellationToken)
    {
        // A refusal here happens before the save, so the question stays as it was.
        var latest = await store.FindLatestVersionAsync(question.Id, cancellationToken);
        // F-102 BR5: the version snapshots the passage as it is now; the read holds a shared lock until the save,
        // so an edit of the text cannot commit between this read and the version.
        var baseText = question.BaseTextId is { } baseTextId
            ? await store.FindBaseTextAsync(baseTextId, cancellationToken)
            : null;
        var next = question.NextVersion(latest, changeReason, baseText?.TextHtml);
        if (next.IsFailure)
        {
            return Result.Failure(next.Error!);
        }

        if (next.Value is { } version)
        {
            store.AddVersion(version);
        }

        return Result.Success();
    }
}
