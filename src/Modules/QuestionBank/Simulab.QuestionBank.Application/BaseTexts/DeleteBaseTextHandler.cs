using Simulab.QuestionBank.Application.Questions;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.BaseTexts;

/// <summary>
/// Deletes a base text that no live question links (F-102, UC5, BR6). The row is locked for update from the
/// read, so a question save that links it waits and the check below sees the link it made.
/// </summary>
public sealed class DeleteBaseTextHandler(IBaseTextStore baseTexts, IQuestionStore questions)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var baseText = await baseTexts.FindForUpdateAsync(id, cancellationToken);
        if (baseText is null)
        {
            return Result.Failure(new Error(QuestionBankErrorCodes.BaseTextNotFound, ErrorKind.NotFound));
        }

        if (await baseTexts.HasLinkedQuestionsAsync(id, cancellationToken))
        {
            return Result.Failure(new Error(QuestionBankErrorCodes.BaseTextInUse, ErrorKind.Conflict));
        }

        baseTexts.Remove(baseText);

        return await questions.TrySaveChangesAsync(cancellationToken) is { } refused
            ? Result.Failure(refused)
            : Result.Success();
    }
}
