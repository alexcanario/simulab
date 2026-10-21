using Simulab.QuestionBank.Contracts;

namespace Simulab.QuestionBank.Domain;

/// <summary>
/// The key of a matching question (F-106, BR1, BR3 to BR5): the left items, each naming the option (right item) it
/// matches, one to one. The options are the right column and carry no mark; extra options match nothing. A draft
/// may leave a match unset; a published question has 2 to 8 left items, at least as many options, every item
/// matched and every text written. A true/false key or a marked option is refused.
/// </summary>
/// <param name="Written">The options (right items) as written.</param>
/// <param name="Items">The left items as written.</param>
/// <param name="TrueFalseWritten">A true/false key that came with them; always a refusal.</param>
public sealed record MatchingKey(
    IReadOnlyList<OptionDraft> Written,
    IReadOnlyList<MatchItemDraft> Items,
    bool? TrueFalseWritten = null) : AnswerKey
{
    public override QuestionType Type => QuestionType.Matching;

    public override IReadOnlyList<OptionDraft> Options => Written;

    public override IReadOnlyList<MatchItemDraft> MatchItems => Items;

    public override bool? TrueFalseValue => null;

    public override OptionFault? Check(bool published) => throw new NotImplementedException();
}
