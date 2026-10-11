using Simulab.QuestionBank.Application.Content;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>
/// Creates a question as a draft or replaces the content of an existing one (F-95, UC1, UC2). Every text is
/// sanitized first, then the topic is looked up, then the shape rules of the entity run on the sanitized
/// result; the answer is the stored question (BR8). A refused edit leaves the stored question as it was.
/// </summary>
public sealed class SaveQuestionHandler(
    IQuestionStore store,
    IQuestionQueries queries,
    QuestionHtmlSanitizer sanitizer)
{
    private static readonly string[] OptionCodes =
    [
        QuestionBankErrorCodes.QuestionOptionsCount,
        QuestionBankErrorCodes.QuestionOptionTooLong,
        QuestionBankErrorCodes.QuestionOptionTextRequired,
        QuestionBankErrorCodes.QuestionKeyInvalid
    ];

    public async Task<SaveQuestionOutcome> HandleAsync(
        Guid? id,
        SaveQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = id is { } existingId ? await store.FindAsync(existingId, cancellationToken) : null;
        if (id is not null && existing is null)
        {
            return Refused(new Error(QuestionBankErrorCodes.QuestionNotFound, ErrorKind.NotFound));
        }

        // BR16: a topic that is unknown or deleted answers 404 before anything about the content is judged.
        if (request.TopicId is { } topicId && topicId != Guid.Empty
            && !await store.TopicExistsAsync(topicId, cancellationToken))
        {
            return Refused(new Error(QuestionBankErrorCodes.QuestionTopicNotFound, ErrorKind.NotFound));
        }

        var drafts = (request.Options ?? [])
            .Select(option => new OptionDraft(sanitizer.Sanitize(option.TextHtml), option.IsCorrect))
            .ToList();
        var content = new QuestionContent(
            request.TopicId,
            request.ContentLanguage,
            sanitizer.Sanitize(request.StatementHtml),
            sanitizer.Sanitize(request.BaseTextHtml),
            sanitizer.Sanitize(request.ExplanationHtml),
            request.Difficulty,
            drafts);

        var question = existing;
        Result applied;
        if (existing is null)
        {
            var created = Question.Create(content);
            question = created.IsSuccess ? created.Value : null;
            applied = created.IsSuccess ? Result.Success() : Result.Failure(created.Error!);
        }
        else
        {
            applied = existing.Update(content);
        }

        if (applied.IsFailure)
        {
            // The entity answers with the code; the position of the option it is about comes from the same rule.
            var position = OptionCodes.Contains(applied.Error!.Code)
                ? QuestionRules.CheckOptions(drafts, existing?.Status == QuestionStatus.Published)?.Position
                : null;

            return Refused(applied.Error, position);
        }

        if (existing is null)
        {
            store.Add(question!);
        }

        await store.SaveChangesAsync(cancellationToken);

        var saved = await queries.FindAsync(question!.Id, cancellationToken);

        return saved is null
            ? Refused(new Error(QuestionBankErrorCodes.QuestionNotFound, ErrorKind.NotFound))
            : new SaveQuestionOutcome(saved);
    }

    private static SaveQuestionOutcome Refused(Error error, int? position = null) =>
        new(null, error, position);
}
