using Simulab.Identity.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Entities;
using Simulab.SharedKernel.Results;
using Simulab.SharedKernel.Text;

namespace Simulab.QuestionBank.Domain.Entities;

/// <summary>
/// A single-choice question in the bank (F-95, BR1 to BR12): a statement, an optional base text, from 0 to 5
/// options with at most one marked as the key once published, an optional explanation, a difficulty, a topic and
/// the language it is written in. Global data: <see cref="TenantEntity.TenantId"/> is null in v1.
/// <para>
/// The rules answer with a <see cref="Result"/> and never throw. A refused edit leaves the entity as it was:
/// the content is checked as a whole before anything is assigned (AC7).
/// </para>
/// </summary>
public sealed class Question : TenantEntity
{
    private readonly List<QuestionOption> _options = [];

    private Question()
    {
    }

    public QuestionType Type { get; private set; }

    public QuestionStatus Status { get; private set; }

    /// <summary>The topic the question belongs to; held by id, never by a foreign key (BR13).</summary>
    public Guid TopicId { get; private set; }

    /// <summary>The language the question is written in, canonical (BR7); never translated.</summary>
    public string ContentLanguage { get; private set; } = string.Empty;

    public string StatementHtml { get; private set; } = string.Empty;

    /// <summary>The statement without tags, for the list excerpt (BR10); derived on every write.</summary>
    public string StatementText { get; private set; } = string.Empty;

    /// <summary>The comparable form of <see cref="StatementText"/>: what the list search reads (BR10).</summary>
    public string NormalizedStatement { get; private set; } = string.Empty;

    public string? BaseTextHtml { get; private set; }

    public string? ExplanationHtml { get; private set; }

    public int? Difficulty { get; private set; }

    /// <summary>The past edition the question came from; held by id, never by a foreign key (F-96, BR1). Null means authored.</summary>
    public Guid? ExamEditionId { get; private set; }

    /// <summary>The question's number in the paper, from 1 to 500; set exactly when there is an edition (F-96, BR2).</summary>
    public int? PaperNumber { get; private set; }

    /// <summary>The notice subject of that edition the question belongs to, when it has one (F-96, BR2).</summary>
    public Guid? NoticeSubjectId { get; private set; }

    /// <summary>The options in position order.</summary>
    public IReadOnlyList<QuestionOption> Options => _options;

    public static Result<Question> Create(QuestionContent content)
    {
        var question = new Question { Type = QuestionType.SingleChoice, Status = QuestionStatus.Draft };
        var applied = question.Apply(content);

        return applied.IsFailure ? Result.Failure<Question>(applied.Error!) : Result.Success(question);
    }

    /// <summary>Replaces the content; a published question must stay complete (BR4).</summary>
    public Result Update(QuestionContent content) => Apply(content);

    /// <summary>Draft to published; the status only (BR12).</summary>
    public Result Publish()
    {
        if (Status == QuestionStatus.Published)
        {
            return Result.Failure(new Error(QuestionBankErrorCodes.QuestionAlreadyPublished, ErrorKind.Conflict));
        }

        var fault = QuestionRules.CheckOptions(Drafts(), published: true);
        if (fault is not null)
        {
            return Result.Failure(fault.Error);
        }

        Status = QuestionStatus.Published;

        return Result.Success();
    }

    /// <summary>Published to draft; the content is untouched (BR12).</summary>
    public Result Unpublish()
    {
        if (Status != QuestionStatus.Published)
        {
            return Result.Failure(new Error(QuestionBankErrorCodes.QuestionNotPublished, ErrorKind.Conflict));
        }

        Status = QuestionStatus.Draft;

        return Result.Success();
    }

    /// <summary>Ok for a draft; <c>question.delete_requires_draft</c> for a published question (BR11).</summary>
    public Result EnsureDeletable() =>
        Status == QuestionStatus.Draft
            ? Result.Success()
            : Result.Failure(new Error(QuestionBankErrorCodes.QuestionDeleteRequiresDraft, ErrorKind.Conflict));

    // The stored options as drafts, the shape the option rules read. An option whose text was empty once its
    // tags were removed is stored as "", so for these rules it is empty exactly when its HTML is.
    private List<OptionDraft> Drafts() =>
        _options
            .OrderBy(option => option.Position)
            .Select(option => new OptionDraft(new SanitizedHtml(option.TextHtml, option.TextHtml), option.IsCorrect))
            .ToList();

    private Result Apply(QuestionContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (content.TopicId is not { } topicId || topicId == Guid.Empty)
        {
            return Result.Failure(new Error(QuestionBankErrorCodes.QuestionTopicNotFound, ErrorKind.NotFound));
        }

        var language = SupportedLanguages.Canonical(content.ContentLanguage);
        if (language is null)
        {
            return Result.Failure(new Error(QuestionBankErrorCodes.QuestionContentLanguageInvalid, ErrorKind.Validation));
        }

        if (content.Statement.IsEmpty)
        {
            return Result.Failure(new Error(QuestionBankErrorCodes.QuestionStatementRequired, ErrorKind.Validation));
        }

        if (content.Statement.Html.Length > QuestionLimits.StatementMaxLength)
        {
            return Result.Failure(new Error(QuestionBankErrorCodes.QuestionStatementTooLong, ErrorKind.Validation));
        }

        if (content.BaseText.Html.Length > QuestionLimits.BaseTextMaxLength)
        {
            return Result.Failure(new Error(QuestionBankErrorCodes.QuestionBaseTextTooLong, ErrorKind.Validation));
        }

        if (content.Explanation.Html.Length > QuestionLimits.ExplanationMaxLength)
        {
            return Result.Failure(new Error(QuestionBankErrorCodes.QuestionExplanationTooLong, ErrorKind.Validation));
        }

        if (content.Difficulty is { } difficulty
            && (difficulty < QuestionLimits.DifficultyMin || difficulty > QuestionLimits.DifficultyMax))
        {
            return Result.Failure(new Error(QuestionBankErrorCodes.QuestionDifficultyInvalid, ErrorKind.Validation));
        }

        // F-96 BR2: an edition needs a number from 1 to 500; a number or a notice subject needs an edition.
        // An empty id counts as none, as the topic's does.
        var editionId = content.ExamEditionId is { } id && id != Guid.Empty ? id : (Guid?)null;
        var subjectId = content.NoticeSubjectId is { } subject && subject != Guid.Empty ? subject : (Guid?)null;
        if (editionId is null && (content.PaperNumber is not null || subjectId is not null))
        {
            return Result.Failure(new Error(QuestionBankErrorCodes.QuestionOriginRequiresEdition, ErrorKind.Validation));
        }

        if (editionId is not null)
        {
            if (content.PaperNumber is not { } paperNumber)
            {
                return Result.Failure(new Error(QuestionBankErrorCodes.QuestionPaperNumberRequired, ErrorKind.Validation));
            }

            if (paperNumber < QuestionLimits.PaperNumberMin || paperNumber > QuestionLimits.PaperNumberMax)
            {
                return Result.Failure(new Error(QuestionBankErrorCodes.QuestionPaperNumberInvalid, ErrorKind.Validation));
            }
        }

        // BR4: editing a published question runs the publish check, so it cannot be made incomplete.
        var fault = QuestionRules.CheckOptions(content.Options, Status == QuestionStatus.Published);
        if (fault is not null)
        {
            return Result.Failure(fault.Error);
        }

        // Everything checked: from here on nothing can fail, so a refusal never leaves a half-written entity.
        TopicId = topicId;
        ContentLanguage = language;
        StatementHtml = content.Statement.Html;
        StatementText = content.Statement.PlainText;
        NormalizedStatement = ComparableText.Normalize(content.Statement.PlainText);
        BaseTextHtml = content.BaseText.IsEmpty ? null : content.BaseText.Html;
        ExplanationHtml = content.Explanation.IsEmpty ? null : content.Explanation.Html;
        Difficulty = content.Difficulty;
        ExamEditionId = editionId;
        PaperNumber = editionId is null ? null : content.PaperNumber;
        NoticeSubjectId = subjectId;
        ReplaceOptions(content.Options);

        return Result.Success();
    }

    // The rows are reused by position, so a save that keeps three options writes three updates and no row
    // churn; the surplus rows leave the list and EF removes them as orphans.
    private void ReplaceOptions(IReadOnlyList<OptionDraft> drafts)
    {
        var ordered = _options.OrderBy(option => option.Position).ToList();

        for (var position = 0; position < drafts.Count; position++)
        {
            var draft = drafts[position];
            var html = draft.Text.IsEmpty ? string.Empty : draft.Text.Html;
            if (position < ordered.Count)
            {
                ordered[position].Set(html, draft.IsCorrect);
            }
            else
            {
                _options.Add(new QuestionOption(position, html, draft.IsCorrect));
            }
        }

        for (var position = ordered.Count - 1; position >= drafts.Count; position--)
        {
            _options.Remove(ordered[position]);
        }
    }
}
