using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>Soft-deletes a draft; a published question is refused (F-95, UC7, BR11).</summary>
public sealed class DeleteQuestionHandler(IQuestionStore store)
{
    public Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
