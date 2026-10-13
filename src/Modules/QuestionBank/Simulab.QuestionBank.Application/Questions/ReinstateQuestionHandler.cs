using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>
/// Takes an annulled question back to published with a reason and records the version that says so
/// (F-101, UC2, BR6).
/// </summary>
public sealed class ReinstateQuestionHandler(IQuestionStore store, IQuestionQueries queries)
{
    public Task<Result<QuestionResponse>> HandleAsync(
        Guid id,
        string? reason,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
