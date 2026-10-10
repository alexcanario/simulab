using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>Takes a published question back to draft; the content is untouched (F-95, UC4, BR12).</summary>
public sealed class UnpublishQuestionHandler(IQuestionStore store, IQuestionQueries queries)
{
    public Task<Result<QuestionResponse>> HandleAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
