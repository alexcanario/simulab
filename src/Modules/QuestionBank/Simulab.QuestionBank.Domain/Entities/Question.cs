using Simulab.Identity.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Entities;
using Simulab.SharedKernel.Results;
using Simulab.SharedKernel.Text;

namespace Simulab.QuestionBank.Domain.Entities;

/// <summary>
/// A question in the bank (F-95, BR1 to BR12): a statement, an optional base text, an answer key whose shape
/// its <see cref="Type"/> decides (F-97: from 0 to 5 options with exactly one correct once published, or one
/// true/false key), an optional explanation, a difficulty, a topic and the language it is written in. Global
/// data: <see cref="TenantEntity.TenantId"/> is null in v1.
/// <para>
/// The rules answer with a <see cref="Result"/> and never throw. A refused edit leaves the entity as it was:
/// the content is checked as a whole before anything is assigned (AC7).
/// </para>
/// </summary>
public sealed class Question : TenantEntity
{
    private readonly List<QuestionOption> _options = [];
    private readonly List<QuestionMatchItem> _matchItems = [];
    private readonly List<QuestionBlank> _blanks = [];

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

    /// <summary>The base text shown before the statement; held by id (F-102, BR2). Null means none.</summary>
    public Guid? BaseTextId { get; private set; }

    public string? ExplanationHtml { get; private set; }

    public int? Difficulty { get; private set; }

    /// <summary>The past edition the question came from; held by id, never by a foreign key (F-96, BR1). Null means authored.</summary>
    public Guid? ExamEditionId { get; private set; }

    /// <summary>The question's number in the paper, from 1 to 500; set exactly when there is an edition (F-96, BR2).</summary>
    public int? PaperNumber { get; private set; }

    /// <summary>The notice subject of that edition the question belongs to, when it has one (F-96, BR2).</summary>
    public Guid? NoticeSubjectId { get; private set; }

    /// <summary>The key of a true/false question: true is "certo", false "errado", null not set (F-97, BR1).</summary>
    public bool? TrueFalseKey { get; private set; }

    /// <summary>The options in position order.</summary>
    public IReadOnlyList<QuestionOption> Options => _options;

    /// <summary>The left items of a matching question in position order; empty for any other type (F-106, BR1).</summary>
    public IReadOnlyList<QuestionMatchItem> MatchItems => _matchItems;

    /// <summary>The blanks of a fill-in-the-blanks question, one per marker, in no set order; empty for any other type (F-107, BR1).</summary>
    public IReadOnlyList<QuestionBlank> Blanks => _blanks;

    public static Result<Question> Create(QuestionContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        // F-97 BR2: the type is chosen here and never changes; content that names none is single choice.
        var question = new Question { Type = content.Type, Status = QuestionStatus.Draft };
        var applied = question.Apply(content);

        return applied.IsFailure ? Result.Failure<Question>(applied.Error!) : Result.Success(question);
    }

    /// <summary>
    /// Replaces the content; a published question must stay complete (BR4). Content of another type is
    /// refused: the type is fixed when the question is created (F-97, BR2).
    /// </summary>
    public Result Update(QuestionContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        // F-101 BR5: an annulled question is frozen; only a reinstatement leaves the state. F-105 BR3: so is a
        // question in review; only a decision or a withdrawal leaves it.
        if (Status == QuestionStatus.Annulled)
        {
            return Result.Failure(Frozen());
        }

        if (Status == QuestionStatus.InReview)
        {
            return Result.Failure(InReviewFrozen());
        }

        return content.Type == Type
            ? Apply(content)
            : Result.Failure(new Error(QuestionBankErrorCodes.QuestionTypeChanged, ErrorKind.Conflict));
    }

    /// <summary>
    /// The version a save or a publish records now (F-98, BR2 to BR5), or success with null when none is made:
    /// a draft never makes one; a published question makes one only when its versioned content differs from
    /// <paramref name="latest"/> (the first publication makes version 1). A key that differs from the latest
    /// version's needs <paramref name="changeReason"/>, 1 to 500 characters after trimming; a reason on any
    /// other version is kept as its note and a reason where no version is made is ignored.
    /// <paramref name="baseTextHtml"/> is the HTML of the base text the question links, read by the caller (F-102,
    /// BR4, BR5): it is part of what a version snapshots, and null means no base text.
    /// </summary>
    public Result<QuestionVersion?> NextVersion(QuestionVersion? latest, string? changeReason, string? baseTextHtml = null)
    {
        if (Status != QuestionStatus.Published)
        {
            return Result.Success<QuestionVersion?>(null);
        }

        var options = _options
            .OrderBy(option => option.Position)
            .Select(option => new QuestionVersionOption
            {
                Position = option.Position,
                TextHtml = option.TextHtml,
                IsCorrect = option.IsCorrect
            })
            .ToList();
        var matchItems = _matchItems
            .OrderBy(item => item.Position)
            .Select(item => new QuestionVersionMatchItem
            {
                Position = item.Position,
                TextHtml = item.TextHtml,
                MatchPosition = item.MatchPosition
            })
            .ToList();

        // BR3: nothing differs from the latest version, so nothing is recorded and a reason is ignored (BR5).
        if (latest is not null && SameVersionedContent(latest, options, matchItems, baseTextHtml))
        {
            return Result.Success<QuestionVersion?>(null);
        }

        var reason = string.IsNullOrWhiteSpace(changeReason) ? null : changeReason.Trim();

        // BR4: version 1 is never a key change; later ones are when the set of correct positions or the true/false
        // value moved (F-104 BR7: a question may have several correct options, so the whole set is compared), or,
        // F-106 BR8, when the number of left items or the option any of them matches moved.
        var keyChanged = latest is not null
            && (latest.TrueFalseKey != TrueFalseKey
                || !CorrectPositions(latest.Options.Select(option => (option.Position, option.IsCorrect)))
                    .SequenceEqual(CorrectPositions(options.Select(option => (option.Position, option.IsCorrect))))
                || !latest.MatchItems.OrderBy(item => item.Position).Select(item => item.MatchPosition)
                    .SequenceEqual(matchItems.Select(item => item.MatchPosition)));

        if (keyChanged && reason is null)
        {
            return Result.Failure<QuestionVersion?>(
                new Error(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired, ErrorKind.Validation));
        }

        if (reason is { Length: > QuestionLimits.ChangeReasonMaxLength })
        {
            return Result.Failure<QuestionVersion?>(
                new Error(QuestionBankErrorCodes.QuestionChangeReasonTooLong, ErrorKind.Validation));
        }

        return Result.Success<QuestionVersion?>(new QuestionVersion(
            Id,
            (latest?.Number ?? 0) + 1,
            Type,
            ContentLanguage,
            StatementHtml,
            baseTextHtml,
            ExplanationHtml,
            TrueFalseKey,
            options,
            keyChanged,
            reason,
            matchItems: matchItems));
    }

    private static List<int> CorrectPositions(IEnumerable<(int Position, bool IsCorrect)> options) =>
        [.. options.Where(option => option.IsCorrect).Select(option => option.Position).Order()];

    // BR1: what is versioned is exactly the language, the statement, the base text, the explanation, the options
    // with their mark, the true/false key and the left items with their match (F-106 BR8); topic, difficulty and
    // origin are catalog data and never compared.
    private bool SameVersionedContent(
        QuestionVersion version,
        List<QuestionVersionOption> options,
        List<QuestionVersionMatchItem> matchItems,
        string? baseTextHtml) =>
        version.MatchItems.Count == matchItems.Count
        && version.MatchItems
            .OrderBy(item => item.Position)
            .Zip(matchItems)
            .All(pair => pair.First.Position == pair.Second.Position
                && pair.First.TextHtml == pair.Second.TextHtml
                && pair.First.MatchPosition == pair.Second.MatchPosition)
        && version.Type == Type
        && version.ContentLanguage == ContentLanguage
        && version.StatementHtml == StatementHtml
        && version.BaseTextHtml == baseTextHtml
        && version.ExplanationHtml == ExplanationHtml
        && version.TrueFalseKey == TrueFalseKey
        && version.Options.Count == options.Count
        && version.Options
            .OrderBy(option => option.Position)
            .Zip(options)
            .All(pair => pair.First.Position == pair.Second.Position
                && pair.First.TextHtml == pair.Second.TextHtml
                && pair.First.IsCorrect == pair.Second.IsCorrect);

    /// <summary>Draft to published; the status only (BR12). A question in review is published by a reviewer's approval, not here (F-105, BR3).</summary>
    public Result Publish()
    {
        if (Status == QuestionStatus.Annulled)
        {
            return Result.Failure(Frozen());
        }

        if (Status == QuestionStatus.InReview)
        {
            return Result.Failure(InReviewFrozen());
        }

        if (Status == QuestionStatus.Published)
        {
            return Result.Failure(new Error(QuestionBankErrorCodes.QuestionAlreadyPublished, ErrorKind.Conflict));
        }

        return MakePublished();
    }

    // The one rule of what publishing a question means (F-105, BR5): the content is checked as a published key is,
    // then the status changes. A direct publish and an approval both end here.
    private Result MakePublished()
    {
        var fault = StoredKey().Check(published: true);
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
        if (Status == QuestionStatus.Annulled)
        {
            return Result.Failure(Frozen());
        }

        if (Status == QuestionStatus.InReview)
        {
            return Result.Failure(InReviewFrozen());
        }

        if (Status != QuestionStatus.Published)
        {
            return Result.Failure(new Error(QuestionBankErrorCodes.QuestionNotPublished, ErrorKind.Conflict));
        }

        Status = QuestionStatus.Draft;

        return Result.Success();
    }

    /// <summary>
    /// Published to annulled (F-101, BR1 to BR4): the version this makes, a copy of <paramref name="latest"/> with
    /// <see cref="QuestionVersion.Annulled"/> true and the trimmed <paramref name="reason"/>. Nothing changes when
    /// it fails.
    /// </summary>
    public Result<QuestionVersion> Annul(QuestionVersion? latest, string? reason)
    {
        // BR1: the state is judged before the reason, as a publish judges its state first.
        if (Status == QuestionStatus.Annulled)
        {
            return Result.Failure<QuestionVersion>(
                new Error(QuestionBankErrorCodes.QuestionAlreadyAnnulled, ErrorKind.Conflict));
        }

        if (Status != QuestionStatus.Published || latest is null)
        {
            return Result.Failure<QuestionVersion>(
                new Error(QuestionBankErrorCodes.QuestionNotPublished, ErrorKind.Conflict));
        }

        var checkedReason = StatusReason(reason);
        if (checkedReason.IsFailure)
        {
            return Result.Failure<QuestionVersion>(checkedReason.Error!);
        }

        // Everything checked: the status changes only now, so a refusal leaves the question as it was.
        Status = QuestionStatus.Annulled;

        return Result.Success(latest.CopyAs(latest.Number + 1, annulled: true, checkedReason.Value!));
    }

    /// <summary>
    /// Annulled to published (F-101, BR6): the version this makes, a copy of <paramref name="latest"/> with
    /// <see cref="QuestionVersion.Annulled"/> false and the trimmed <paramref name="reason"/> as its note.
    /// </summary>
    public Result<QuestionVersion> Reinstate(QuestionVersion? latest, string? reason)
    {
        if (Status != QuestionStatus.Annulled || latest is null)
        {
            return Result.Failure<QuestionVersion>(
                new Error(QuestionBankErrorCodes.QuestionNotAnnulled, ErrorKind.Conflict));
        }

        var checkedReason = StatusReason(reason);
        if (checkedReason.IsFailure)
        {
            return Result.Failure<QuestionVersion>(checkedReason.Error!);
        }

        Status = QuestionStatus.Published;

        return Result.Success(latest.CopyAs(latest.Number + 1, annulled: false, checkedReason.Value!));
    }

    /// <summary>
    /// Draft to in review (F-105, BR1, BR2): the entry that records the send. The state is judged first, as a
    /// publish judges it, then the content is checked as publishing checks it, so a reviewer never receives a
    /// question that could not be published. A send makes no version (versions belong to published content).
    /// </summary>
    public Result<QuestionReviewEntry> Submit()
    {
        if (Status == QuestionStatus.Annulled)
        {
            return Result.Failure<QuestionReviewEntry>(Frozen());
        }

        if (Status == QuestionStatus.InReview)
        {
            return Result.Failure<QuestionReviewEntry>(
                new Error(QuestionBankErrorCodes.QuestionAlreadyInReview, ErrorKind.Conflict));
        }

        if (Status == QuestionStatus.Published)
        {
            return Result.Failure<QuestionReviewEntry>(
                new Error(QuestionBankErrorCodes.QuestionAlreadyPublished, ErrorKind.Conflict));
        }

        var fault = StoredKey().Check(published: true);
        if (fault is not null)
        {
            return Result.Failure<QuestionReviewEntry>(fault.Error);
        }

        Status = QuestionStatus.InReview;

        return Result.Success(new QuestionReviewEntry(Id, QuestionReviewKind.Submitted, null));
    }

    /// <summary>In review to draft (F-105, BR4): the content is untouched, and the entry records the withdrawal.</summary>
    public Result<QuestionReviewEntry> Withdraw()
    {
        if (Status != QuestionStatus.InReview)
        {
            return Result.Failure<QuestionReviewEntry>(NotInReview());
        }

        Status = QuestionStatus.Draft;

        return Result.Success(new QuestionReviewEntry(Id, QuestionReviewKind.Withdrawn, null));
    }

    /// <summary>
    /// In review to published (F-105, BR5): the same publication as <see cref="Publish"/> (the caller makes the
    /// version with <see cref="NextVersion"/>, as a publish does), and the entry holds the trimmed
    /// <paramref name="note"/>, optional, 1 to 500 characters. The state is judged before the note. Nothing changes
    /// when it fails.
    /// </summary>
    public Result<QuestionReviewEntry> Approve(string? note)
    {
        if (Status != QuestionStatus.InReview)
        {
            return Result.Failure<QuestionReviewEntry>(NotInReview());
        }

        var text = note?.Trim();
        if (text is { Length: > QuestionLimits.ChangeReasonMaxLength })
        {
            return Result.Failure<QuestionReviewEntry>(
                new Error(QuestionBankErrorCodes.QuestionReviewReasonTooLong, ErrorKind.Validation));
        }

        var published = MakePublished();
        if (published.IsFailure)
        {
            return Result.Failure<QuestionReviewEntry>(published.Error!);
        }

        return Result.Success(new QuestionReviewEntry(Id, QuestionReviewKind.Approved, string.IsNullOrEmpty(text) ? null : text));
    }

    /// <summary>
    /// In review to draft (F-105, BR6): the entry holds the trimmed <paramref name="reason"/>, required, 1 to 500
    /// characters. The state is judged before the reason. There is no rejected status.
    /// </summary>
    public Result<QuestionReviewEntry> Reject(string? reason)
    {
        if (Status != QuestionStatus.InReview)
        {
            return Result.Failure<QuestionReviewEntry>(NotInReview());
        }

        var checkedReason = StatusReason(
            reason,
            QuestionBankErrorCodes.QuestionReviewReasonRequired,
            QuestionBankErrorCodes.QuestionReviewReasonTooLong);
        if (checkedReason.IsFailure)
        {
            return Result.Failure<QuestionReviewEntry>(checkedReason.Error!);
        }

        Status = QuestionStatus.Draft;

        return Result.Success(new QuestionReviewEntry(Id, QuestionReviewKind.Rejected, checkedReason.Value!));
    }

    /// <summary>
    /// Ok for a draft; <c>question.annulled</c> for an annulled question (F-101, BR5);
    /// <c>question.in_review</c> for a question in review (F-105, BR3);
    /// <c>question.delete_requires_draft</c> for a published question (BR11).
    /// </summary>
    public Result EnsureDeletable() =>
        Status switch
        {
            QuestionStatus.Draft => Result.Success(),
            QuestionStatus.Annulled => Result.Failure(Frozen()),
            QuestionStatus.InReview => Result.Failure(InReviewFrozen()),
            _ => Result.Failure(new Error(QuestionBankErrorCodes.QuestionDeleteRequiresDraft, ErrorKind.Conflict))
        };

    private static Error Frozen() => new(QuestionBankErrorCodes.QuestionAnnulled, ErrorKind.Conflict);

    // F-105 BR3: a question in review is frozen as an annulled one is, with a code of its own.
    private static Error InReviewFrozen() => new(QuestionBankErrorCodes.QuestionInReview, ErrorKind.Conflict);

    private static Error NotInReview() => new(QuestionBankErrorCodes.QuestionNotInReview, ErrorKind.Conflict);

    // F-101 BR3: the reason of an annulment or a reinstatement, 1 to 500 characters once trimmed. F-105 BR6 asks
    // the same of a rejection, with the codes of its own.
    private static Result<string> StatusReason(
        string? reason,
        string requiredCode = QuestionBankErrorCodes.QuestionStatusReasonRequired,
        string tooLongCode = QuestionBankErrorCodes.QuestionStatusReasonTooLong)
    {
        var text = reason?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return Result.Failure<string>(new Error(requiredCode, ErrorKind.Validation));
        }

        return text.Length > QuestionLimits.ChangeReasonMaxLength
            ? Result.Failure<string>(new Error(tooLongCode, ErrorKind.Validation))
            : Result.Success(text);
    }

    // The stored options as drafts, the shape the option rules read. An option whose text was empty once its
    // tags were removed is stored as "", so for these rules it is empty exactly when its HTML is.
    private List<OptionDraft> Drafts() =>
        _options
            .OrderBy(option => option.Position)
            .Select(option => new OptionDraft(new SanitizedHtml(option.TextHtml, option.TextHtml), option.IsCorrect))
            .ToList();

    // The stored left items as drafts, read the same way (F-106).
    private List<MatchItemDraft> MatchItemDrafts() =>
        _matchItems
            .OrderBy(item => item.Position)
            .Select(item => new MatchItemDraft(new SanitizedHtml(item.TextHtml, item.TextHtml), item.MatchPosition))
            .ToList();

    // The key as stored: the type decides what it is made of (F-97, BR9).
    private AnswerKey StoredKey() => AnswerKey.From(Type, Drafts(), TrueFalseKey, MatchItemDrafts());

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

        if (content.Explanation.Html.Length > QuestionLimits.ExplanationMaxLength)
        {
            return Result.Failure(new Error(QuestionBankErrorCodes.QuestionExplanationTooLong, ErrorKind.Validation));
        }

        // F-103 BR4, BR7: the statement, the options and (F-106 BR7) the left items together hold at most ten images,
        // each with its alternative text.
        var matchItems = content.MatchItems ?? [];
        if (QuestionImageRules.Check(
                content.Options.Select(option => option.Text)
                    .Concat(matchItems.Select(item => item.Text))
                    .Prepend(content.Statement),
                QuestionBankErrorCodes.QuestionImageAltRequired,
                QuestionBankErrorCodes.QuestionImageAltTooLong,
                QuestionBankErrorCodes.QuestionImagesTooMany) is { } imageFault)
        {
            return Result.Failure(imageFault);
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
        var key = AnswerKey.From(Type, content.Options, content.TrueFalseKey, matchItems);
        var fault = key.Check(Status == QuestionStatus.Published);
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
        BaseTextId = content.BaseTextId is { } baseTextId && baseTextId != Guid.Empty ? baseTextId : null;
        ExplanationHtml = content.Explanation.IsEmpty ? null : content.Explanation.Html;
        Difficulty = content.Difficulty;
        ExamEditionId = editionId;
        PaperNumber = editionId is null ? null : content.PaperNumber;
        NoticeSubjectId = subjectId;
        TrueFalseKey = key.TrueFalseValue;
        ReplaceOptions(key.Options);
        ReplaceMatchItems(key.MatchItems);

        return Result.Success();
    }

    // As the options: rows reused by position, the surplus removed as orphans (F-106).
    private void ReplaceMatchItems(IReadOnlyList<MatchItemDraft> drafts)
    {
        var ordered = _matchItems.OrderBy(item => item.Position).ToList();

        for (var position = 0; position < drafts.Count; position++)
        {
            var draft = drafts[position];
            var html = draft.Text.IsEmpty ? string.Empty : draft.Text.Html;
            if (position < ordered.Count)
            {
                ordered[position].Set(html, draft.MatchPosition);
            }
            else
            {
                _matchItems.Add(new QuestionMatchItem(position, html, draft.MatchPosition));
            }
        }

        for (var position = ordered.Count - 1; position >= drafts.Count; position--)
        {
            _matchItems.Remove(ordered[position]);
        }
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
