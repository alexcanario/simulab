using Simulab.QuestionBank.Application.Content;
using Simulab.QuestionBank.Application.Questions;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.BaseTexts;

/// <summary>
/// Creates a base text or replaces the content of an existing one (F-102, UC1, UC2). The text is sanitized
/// first, then the shape rules of the entity run; when the text changes, the versions of the published questions
/// that link it are recorded in the same save (BR4). The row is locked for update from the read, so a question
/// save that links it, a publish or a delete waits for this one (BR3, BR4, BR6).
/// </summary>
public sealed class SaveBaseTextHandler(
    IBaseTextStore baseTexts,
    IQuestionStore questions,
    IBaseTextQueries queries,
    QuestionHtmlSanitizer sanitizer)
{
    public async Task<Result<BaseTextResponse>> HandleAsync(
        Guid? id,
        SaveBaseTextRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        BaseText? existing = null;
        if (id is { } existingId)
        {
            existing = await baseTexts.FindForUpdateAsync(existingId, cancellationToken);
            if (existing is null)
            {
                return Result.Failure<BaseTextResponse>(new Error(QuestionBankErrorCodes.BaseTextNotFound, ErrorKind.NotFound));
            }
        }

        var text = sanitizer.Sanitize(request.TextHtml);
        var previousHtml = existing?.TextHtml;
        BaseText baseText;
        if (existing is null)
        {
            var created = BaseText.Create(request.Title, request.ContentLanguage, text);
            if (created.IsFailure)
            {
                return Result.Failure<BaseTextResponse>(created.Error!);
            }

            baseText = created.Value!;
            baseTexts.Add(baseText);
        }
        else
        {
            var hasLinked = await baseTexts.HasLinkedQuestionsAsync(existing.Id, cancellationToken);
            var updated = existing.Update(request.Title, request.ContentLanguage, text, hasLinked);
            if (updated.IsFailure)
            {
                return Result.Failure<BaseTextResponse>(updated.Error!);
            }

            baseText = existing;
        }

        // BR4: the passage changed, so every published question that shows it gets the next version, in this save.
        if (existing is not null && baseText.TextHtml != previousHtml)
        {
            var versioned = await RecordVersionsAsync(baseText, cancellationToken);
            if (versioned is not null)
            {
                return Result.Failure<BaseTextResponse>(versioned);
            }
        }

        if (await questions.TrySaveChangesAsync(cancellationToken) is { } refused)
        {
            return Result.Failure<BaseTextResponse>(refused);
        }

        var saved = await queries.FindAsync(baseText.Id, cancellationToken);

        return saved is null
            ? Result.Failure<BaseTextResponse>(new Error(QuestionBankErrorCodes.BaseTextNotFound, ErrorKind.NotFound))
            : Result.Success(saved);
    }

    private async Task<Error?> RecordVersionsAsync(BaseText baseText, CancellationToken cancellationToken)
    {
        var published = await baseTexts.ListPublishedLinkedAsync(baseText.Id, cancellationToken);
        if (published.Count == 0)
        {
            return null;
        }

        var latest = await baseTexts.FindLatestVersionsAsync([.. published.Select(question => question.Id)], cancellationToken);
        foreach (var question in published)
        {
            latest.TryGetValue(question.Id, out var latestVersion);

            // The key never changes here, so no reason is asked; the same HTML makes nothing (BR3 of F-98).
            var next = question.NextVersion(latestVersion, null, baseText.TextHtml);
            if (next.IsFailure)
            {
                return next.Error;
            }

            if (next.Value is { } version)
            {
                questions.AddVersion(version);
                baseTexts.GuardStatus(question);
            }
        }

        return null;
    }
}
