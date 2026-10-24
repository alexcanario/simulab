using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Domain;

/// <summary>
/// What a question's type says about its answer (F-97, BR9): the one place a type declares the shape of its key
/// and the rules that decide whether it is acceptable as a draft and as a published question. A later type adds
/// a record beside <see cref="SingleChoiceKey"/> and <see cref="TrueFalseKey"/> and one line to
/// <see cref="From"/>; the entity never branches on the type.
/// </summary>
public abstract record AnswerKey
{
    /// <summary>The type this key belongs to.</summary>
    public abstract QuestionType Type { get; }

    /// <summary>The options the question stores for this key, in order; empty for a type with none.</summary>
    public abstract IReadOnlyList<OptionDraft> Options { get; }

    /// <summary>The true/false key the question stores; null for any other type or when it is not set.</summary>
    public abstract bool? TrueFalseValue { get; }

    /// <summary>The left items the question stores (F-106, BR10); empty for every type but matching.</summary>
    public virtual IReadOnlyList<MatchItemDraft> MatchItems => [];

    /// <summary>
    /// The blanks the question stores, one per marker of the statement in statement order (F-107, BR1); for a short
    /// answer (F-108, BR1) the one blank numbered 1. Empty for every other type.
    /// </summary>
    public virtual IReadOnlyList<BlankDraft> Blanks => [];

    /// <summary>
    /// The first thing wrong with this key, or null (BR3, BR4). A draft may be incomplete; a published question
    /// must be complete. A key given to a type that has none, and options given to a type that has none, are
    /// refused on both: nothing is ignored.
    /// </summary>
    public abstract OptionFault? Check(bool published);

    /// <summary>F-106 BR6: left items written to a type that has none; null when there are none.</summary>
    protected static OptionFault? MatchItemsRefused(IReadOnlyList<MatchItemDraft>? written) =>
        written is { Count: > 0 }
            ? new OptionFault(new Error(QuestionBankErrorCodes.QuestionMatchItemsNotAllowed, ErrorKind.Validation), null)
            : null;

    /// <summary>F-107 BR7: blanks written to a type that has none; null when there are none.</summary>
    protected static OptionFault? BlanksRefused(IReadOnlyList<BlankDraft>? written) =>
        written is { Count: > 0 }
            ? new OptionFault(new Error(QuestionBankErrorCodes.QuestionBlanksNotAllowed, ErrorKind.Validation), null)
            : null;

    /// <summary>
    /// The key of a question of <paramref name="type"/> from what was written: the options, the true/false key, the
    /// left items and the blanks as they came, so that the key itself can refuse what its type does not have.
    /// <paramref name="blankMarkers"/> are the marker numbers of the statement, which only a fill-in-the-blanks key reads.
    /// </summary>
    public static AnswerKey From(
        QuestionType type,
        IReadOnlyList<OptionDraft> options,
        bool? trueFalseKey,
        IReadOnlyList<MatchItemDraft>? matchItems = null,
        IReadOnlyList<BlankDraft>? blanks = null,
        IReadOnlyList<int>? blankMarkers = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        var items = matchItems ?? [];
        var written = blanks ?? [];

        return type switch
        {
            QuestionType.TrueFalse => new TrueFalseKey(options, trueFalseKey, items, written),
            QuestionType.MultipleAnswer => new MultipleAnswerKey(options, trueFalseKey, items, written),
            QuestionType.Matching => new MatchingKey(options, items, trueFalseKey, written),
            QuestionType.FillInTheBlanks => new FillInTheBlanksKey(blankMarkers ?? [], written, options, trueFalseKey, items),
            QuestionType.ShortAnswer => new ShortAnswerKey(written, options, trueFalseKey, items),
            _ => new SingleChoiceKey(options, trueFalseKey, items, written)
        };
    }
}
