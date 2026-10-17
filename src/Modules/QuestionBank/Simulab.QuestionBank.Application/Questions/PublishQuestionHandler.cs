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

        var versioned = await QuestionPublication.AddVersionAsync(store, question, changeReason, cancellationToken);
        if (versioned.IsFailure)
        {
            return Result.Failure<QuestionResponse>(versioned.Error!);
        }

        // The status and the version are one save: a lost race for the number (F-98 BR10) leaves both undone.
        if (await store.TrySaveChangesAsync(cancellationToken) is { } refused)
        {
            return Result.Failure<QuestionResponse>(refused);
        }

        return await ReviewAnswer.ReadAsync(queries, id, cancellationToken);
    }
}
