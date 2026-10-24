using Simulab.QuestionBank.Contracts;

namespace Simulab.QuestionBank.Domain;

/// <summary>
/// The key of an essay question (F-109, BR1, BR7): a rubric and an optional limit in lines, a minimum and a maximum each
/// from 1 to 100, the minimum not above the maximum. The statement is the theme. The parameters are those of
/// <see cref="RubricKey"/>.
/// </summary>
public sealed record EssayKey(
    IReadOnlyList<CriterionDraft> CriteriaWritten,
    int? SizeMinWritten,
    int? SizeMaxWritten,
    IReadOnlyList<OptionDraft>? OptionsWritten = null,
    bool? TrueFalseWritten = null,
    IReadOnlyList<MatchItemDraft>? MatchItemsWritten = null,
    IReadOnlyList<BlankDraft>? BlanksWritten = null)
    : RubricKey(CriteriaWritten, SizeMinWritten, SizeMaxWritten, OptionsWritten, TrueFalseWritten, MatchItemsWritten, BlanksWritten)
{
    public override QuestionType Type => QuestionType.Essay;

    protected override bool SizeLimitIsValid() =>
        (SizeMinWritten is null or >= 1 and <= QuestionLimits.EssayLinesMax)
        && (SizeMaxWritten is null or >= 1 and <= QuestionLimits.EssayLinesMax)
        && (SizeMinWritten is null || SizeMaxWritten is null || SizeMinWritten <= SizeMaxWritten);
}
