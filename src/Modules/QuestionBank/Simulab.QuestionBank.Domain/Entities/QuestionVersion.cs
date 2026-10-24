using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Entities;

namespace Simulab.QuestionBank.Domain.Entities;

/// <summary>
/// An immutable snapshot of a published question (F-98, BR1): what students were shown and graded on, numbered
/// from 1 for the question. A version is made by <see cref="Question.NextVersion"/> and never edited or
/// deleted (BR6); the author and the date are the audit columns (BR7). Topic, difficulty and origin are catalog
/// data and are not part of it.
/// </summary>
public sealed class QuestionVersion : TenantEntity
{
    private readonly List<QuestionVersionOption> _options = [];
    private readonly List<QuestionVersionMatchItem>? _matchItems = [];
    private readonly List<QuestionVersionBlank>? _blanks = [];

    // For EF.
    private QuestionVersion()
    {
    }

    internal QuestionVersion(
        Guid questionId,
        int number,
        QuestionType type,
        string contentLanguage,
        string statementHtml,
        string? baseTextHtml,
        string? explanationHtml,
        bool? trueFalseKey,
        IEnumerable<QuestionVersionOption> options,
        bool answerKeyChanged,
        string? reason,
        bool annulled = false,
        IEnumerable<QuestionVersionMatchItem>? matchItems = null,
        IEnumerable<QuestionVersionBlank>? blanks = null)
    {
        QuestionId = questionId;
        Number = number;
        Type = type;
        ContentLanguage = contentLanguage;
        StatementHtml = statementHtml;
        BaseTextHtml = baseTextHtml;
        ExplanationHtml = explanationHtml;
        TrueFalseKey = trueFalseKey;
        _options.AddRange(options);
        AnswerKeyChanged = answerKeyChanged;
        Reason = reason;
        Annulled = annulled;
        _matchItems = [.. matchItems ?? []];
        _blanks = [.. blanks ?? []];
    }

    /// <summary>
    /// The version an annulment or a reinstatement records (F-101, BR2, BR6): this one's content, numbered
    /// <paramref name="number"/>, not a key change, with the state and the reason given. The options, the left
    /// items (F-106) and the blanks (F-107) are new instances, so the two versions share nothing.
    /// </summary>
    internal QuestionVersion CopyAs(int number, bool annulled, string reason) =>
        new(
            QuestionId,
            number,
            Type,
            ContentLanguage,
            StatementHtml,
            BaseTextHtml,
            ExplanationHtml,
            TrueFalseKey,
            _options.Select(option => new QuestionVersionOption
            {
                Position = option.Position,
                TextHtml = option.TextHtml,
                IsCorrect = option.IsCorrect
            }),
            answerKeyChanged: false,
            reason,
            annulled,
            MatchItems.Select(item => new QuestionVersionMatchItem
            {
                Position = item.Position,
                TextHtml = item.TextHtml,
                MatchPosition = item.MatchPosition
            }),
            Blanks.Select(blank => new QuestionVersionBlank { Number = blank.Number, AcceptedAnswers = [.. blank.AcceptedAnswers] }));

    /// <summary>The question the version belongs to, held by id with no navigation back.</summary>
    public Guid QuestionId { get; private set; }

    /// <summary>1, 2, ...; unique for the question (BR7). The latest version is the highest number.</summary>
    public int Number { get; private set; }

    public QuestionType Type { get; private set; }

    public string ContentLanguage { get; private set; } = string.Empty;

    public string StatementHtml { get; private set; } = string.Empty;

    public string? BaseTextHtml { get; private set; }

    public string? ExplanationHtml { get; private set; }

    /// <summary>The key of a true/false question; null for any other type.</summary>
    public bool? TrueFalseKey { get; private set; }

    /// <summary>The options in position order; empty for a true/false question.</summary>
    public IReadOnlyList<QuestionVersionOption> Options => _options;

    /// <summary>
    /// The left items of a matching question in position order (F-106, BR8); empty for any other type and for a
    /// version recorded before F-106, whose JSON column is null.
    /// </summary>
    public IReadOnlyList<QuestionVersionMatchItem> MatchItems => _matchItems ?? [];

    /// <summary>
    /// The blanks of a fill-in-the-blanks question in statement order (F-107, BR10); empty for any other type and for a
    /// version recorded before F-107, whose JSON column is null.
    /// </summary>
    public IReadOnlyList<QuestionVersionBlank> Blanks => _blanks ?? [];

    /// <summary>
    /// The criteria of an open-answer or essay question in rubric order (F-109, BR10); empty for any other type and for a
    /// version recorded before F-109, whose JSON column is null.
    /// </summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped] // skeleton: removed when the column exists
    public IReadOnlyList<QuestionVersionCriterion> Rubric => [];

    /// <summary>The size limit the question had (F-109, BR10): minimum lines of an essay; null when not set.</summary>
    public int? AnswerSizeMin => null;

    /// <summary>The size limit the question had (F-109, BR10): maximum lines of an essay or characters of an open answer; null when not set.</summary>
    public int? AnswerSizeMax => null;

    /// <summary>True when the answer key differs from the previous version's (BR4); never true for version 1.</summary>
    public bool AnswerKeyChanged { get; private set; }

    /// <summary>The reason the curator wrote, trimmed; always set when <see cref="AnswerKeyChanged"/> is (BR5).</summary>
    public string? Reason { get; private set; }

    /// <summary>
    /// True when the question was annulled when this version was recorded (F-101, BR2). A reinstatement is
    /// the first version after an annulled one with this false; it is derived, not stored.
    /// </summary>
    public bool Annulled { get; private set; }
}
