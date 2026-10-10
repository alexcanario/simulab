using Simulab.QuestionBank.Application.Content;
using Simulab.QuestionBank.Contracts;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>
/// Creates a question as a draft or replaces the content of an existing one (F-95, UC1, UC2). Every text is
/// sanitized first, then the shape rules of the entity run on the sanitized result, then the topic is looked
/// up; the answer is the stored question (BR8).
/// </summary>
public sealed class SaveQuestionHandler(
    IQuestionStore store,
    IQuestionQueries queries,
    QuestionHtmlSanitizer sanitizer)
{
    public Task<SaveQuestionOutcome> HandleAsync(
        Guid? id,
        SaveQuestionRequest request,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
