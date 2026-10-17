using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>F-105 Withdraw: skeleton, written in the build.</summary>
public sealed class WithdrawQuestionHandler(IQuestionStore store, IQuestionQueries queries)
{
    public Task<Result<QuestionResponse>> HandleAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
