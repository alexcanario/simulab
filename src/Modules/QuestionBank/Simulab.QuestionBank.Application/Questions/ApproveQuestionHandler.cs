using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>F-105 Approve: skeleton, written in the build.</summary>
public sealed class ApproveQuestionHandler(IQuestionStore store, IQuestionQueries queries)
{
    public Task<Result<QuestionResponse>> HandleAsync(
        Guid id,
        string? note,
        DateTimeOffset? submittedAt = null,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
