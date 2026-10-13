using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>
/// Annuls a published question with a reason and records the version that says so (F-101, UC1, BR1 to BR4).
/// </summary>
public sealed class AnnulQuestionHandler(IQuestionStore store, IQuestionQueries queries)
{
    public Task<Result<QuestionResponse>> HandleAsync(
        Guid id,
        string? reason,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
