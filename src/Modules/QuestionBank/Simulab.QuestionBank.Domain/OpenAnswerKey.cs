using Simulab.QuestionBank.Contracts;

namespace Simulab.QuestionBank.Domain;

/// <summary>
/// The key of an open-answer question (F-109, BR1, BR7): a rubric and an optional maximum number of characters, from 1 to
/// 5,000. It has no minimum: one that is written is refused. The parameters are those of <see cref="RubricKey"/>.
/// </summary>
public sealed record OpenAnswerKey(
    IReadOnlyList<CriterionDraft> CriteriaWritten,
    int? SizeMinWritten,
    int? SizeMaxWritten,
    IReadOnlyList<OptionDraft>? OptionsWritten = null,
    bool? TrueFalseWritten = null,
    IReadOnlyList<MatchItemDraft>? MatchItemsWritten = null,
    IReadOnlyList<BlankDraft>? BlanksWritten = null)
    : RubricKey(CriteriaWritten, SizeMinWritten, SizeMaxWritten, OptionsWritten, TrueFalseWritten, MatchItemsWritten, BlanksWritten)
{
    public override QuestionType Type => QuestionType.OpenAnswer;

    protected override bool SizeLimitIsValid() =>
        SizeMinWritten is null && (SizeMaxWritten is null || SizeMaxWritten is >= 1 and <= QuestionLimits.OpenAnswerCharactersMax);
}
