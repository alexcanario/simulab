using Simulab.QuestionBank.Contracts;

namespace Simulab.QuestionBank.Domain;

/// <summary>
/// Everything the question form edits, already sanitized: what <see cref="Entities.Question.Create"/> and
/// <see cref="Entities.Question.Update"/> take, so a create and an update are one validation (BR3, BR4).
/// </summary>
/// <param name="TopicId">The topic; null or empty is refused.</param>
/// <param name="ContentLanguage">One of the app's languages, any case.</param>
/// <param name="Statement">The statement.</param>
/// <param name="BaseTextId">The base text the question links; null when it has none (F-102, BR2).</param>
/// <param name="Explanation">The explanation; empty when there is none.</param>
/// <param name="Difficulty">1 to 5, or null.</param>
/// <param name="Options">The options in order.</param>
/// <param name="ExamEditionId">The past edition the question came from; null or empty means authored (F-96).</param>
/// <param name="PaperNumber">The number in that paper; required with an edition, 1 to 500 (F-96, BR2).</param>
/// <param name="NoticeSubjectId">The notice subject of that edition, when the question has one (F-96, BR2).</param>
/// <param name="Type">How the question is answered; fixed when the question is created (F-97, BR2).</param>
/// <param name="TrueFalseKey">The key of a true/false question; null when not set. Refused on any other type (F-97, BR3).</param>
/// <param name="MatchItems">The left items of a matching question; null counts as none. Refused on any other type (F-106, BR6).</param>
/// <param name="Blanks">The blanks of a fill-in-the-blanks question; null counts as none. Refused on any other type (F-107, BR7).</param>
public sealed record QuestionContent(
    Guid? TopicId,
    string? ContentLanguage,
    SanitizedHtml Statement,
    Guid? BaseTextId,
    SanitizedHtml Explanation,
    int? Difficulty,
    IReadOnlyList<OptionDraft> Options,
    Guid? ExamEditionId = null,
    int? PaperNumber = null,
    Guid? NoticeSubjectId = null,
    QuestionType Type = QuestionType.SingleChoice,
    bool? TrueFalseKey = null,
    IReadOnlyList<MatchItemDraft>? MatchItems = null,
    IReadOnlyList<BlankDraft>? Blanks = null);
