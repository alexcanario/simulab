using Simulab.QuestionBank.Application.Questions;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.BaseTexts;

/// <summary>Deletes a base text that no live question links (F-102, UC5, BR6).</summary>
public sealed class DeleteBaseTextHandler(IBaseTextStore baseTexts, IQuestionStore questions)
{
    public Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
