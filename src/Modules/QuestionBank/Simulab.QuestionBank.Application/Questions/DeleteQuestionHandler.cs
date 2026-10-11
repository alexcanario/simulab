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
        await store.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
