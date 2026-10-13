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

        // F-97 BR2: the type never changes. Naming another one is a 409 before anything else is judged, so the
        // shape checks never answer 400 for a request that is really asking to change the type.
        if (existing is not null && request.Type is { } named && named != existing.Type)
        {
            return Refused(new Error(QuestionBankErrorCodes.QuestionTypeChanged, ErrorKind.Conflict));
        }

        // BR16: a topic that is unknown or deleted answers 404 before anything about the content is judged.
        if (request.TopicId is { } topicId && topicId != Guid.Empty
            && !await store.TopicExistsAsync(topicId, cancellationToken))
        {
            return Refused(new Error(QuestionBankErrorCodes.QuestionTopicNotFound, ErrorKind.NotFound));
        }

        // F-96 BR2: an edition or a notice subject that is unknown, deleted or of another edition answers 404
        // before anything about the content is judged, as the topic does.
        if (request.ExamEditionId is { } editionId && editionId != Guid.Empty)
        {
            if (!await store.ExamEditionExistsAsync(editionId, cancellationToken))
            {
                return Refused(new Error(QuestionBankErrorCodes.QuestionExamEditionNotFound, ErrorKind.NotFound));
            }

            if (request.NoticeSubjectId is { } subjectId && subjectId != Guid.Empty
                && !await store.NoticeSubjectBelongsToAsync(subjectId, editionId, cancellationToken))
            {
                return Refused(new Error(QuestionBankErrorCodes.QuestionNoticeSubjectNotFound, ErrorKind.NotFound));
            }
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
            drafts,
            request.ExamEditionId,
            request.PaperNumber,
            request.NoticeSubjectId,
            existing?.Type ?? request.Type ?? QuestionType.SingleChoice,
            request.TrueFalseKey);

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

        // F-98 BR3 to BR5: a saved published question records a version when its versioned content differs
        // from the latest one. A refusal here (the reason) happens before anything is written; the version goes
        // in the same save as the content, so a lost race (BR10) rolls the content back with it.
        if (existing is not null && question!.Status == QuestionStatus.Published)
        {
            var latest = await store.FindLatestVersionAsync(question.Id, cancellationToken);
            var next = question.NextVersion(latest, request.ChangeReason);
            if (next.IsFailure)
            {
                return Refused(next.Error!);
            }

            if (next.Value is { } version)
            {
                store.AddVersion(version);
            }
        }

        // F-96 BR3: the number is unique among the live questions of the edition. The pre-check gives the
        // normal answer; the unique index and its translation below catch two saves racing for one number.
        if (question!.ExamEditionId is { } origin && question.PaperNumber is { } number
            && await store.IsPaperNumberTakenAsync(origin, number, existing?.Id, cancellationToken))
        {
            return Refused(new Error(QuestionBankErrorCodes.QuestionPaperNumberTaken, ErrorKind.Conflict));
        }

        if (existing is null)
        {
            store.Add(question);
        }

        if (await store.TrySaveChangesAsync(cancellationToken) is { } refused)
        {
            return Refused(refused);
        }

        var saved = await queries.FindAsync(question.Id, cancellationToken);

        return saved is null
            ? Refused(new Error(QuestionBankErrorCodes.QuestionNotFound, ErrorKind.NotFound))
            : new SaveQuestionOutcome(saved);
    }

    private static SaveQuestionOutcome Refused(Error error, int? position = null) =>
        new(null, error, position);
}
