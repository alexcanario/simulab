using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>Publishes a complete draft; the status only (F-95, UC3, BR4, BR12).</summary>
public sealed class PublishQuestionHandler(IQuestionStore store, IQuestionQueries queries)
{
    public Task<Result<QuestionResponse>> HandleAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
