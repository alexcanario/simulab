using Simulab.QuestionBank.Application.Content;
using Simulab.QuestionBank.Application.Questions;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.BaseTexts;

/// <summary>
/// Creates a base text or replaces the content of an existing one (F-102, UC1, UC2). The text is sanitized
/// first, then the shape rules of the entity run; when the text changes, the versions of the published questions
/// that link it are recorded in the same save (BR4).
/// </summary>
public sealed class SaveBaseTextHandler(
    IBaseTextStore baseTexts,
    IQuestionStore questions,
    IBaseTextQueries queries,
    QuestionHtmlSanitizer sanitizer)
{
    public Task<Result<BaseTextResponse>> HandleAsync(
        Guid? id,
        SaveBaseTextRequest request,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
