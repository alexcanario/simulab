using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>Soft-deletes a draft; a published question is refused (F-95, UC7, BR11).</summary>
public sealed class DeleteQuestionHandler(IQuestionStore store)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var question = await store.FindAsync(id, cancellationToken);
        if (question is null)
        {
            return Result.Failure(new Error(QuestionBankErrorCodes.QuestionNotFound, ErrorKind.NotFound));
        }

        var deletable = question.EnsureDeletable();
        if (deletable.IsFailure)
        {
            return deletable;
        }

        // The question and its options leave together; the interceptor turns both removals into flags.
        store.Remove(question);
        // F-101: the status is a concurrency token; a publish or an annulment that got there first answers 409.
        if (await store.TrySaveChangesAsync(cancellationToken) is { } refused)
        {
            return Result.Failure(refused);
        }

        return Result.Success();
    }
}
