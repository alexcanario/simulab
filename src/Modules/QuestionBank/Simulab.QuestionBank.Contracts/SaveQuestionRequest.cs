namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// What the question form sends, for a create and for an update (F-95, UC1 and UC2). The type is implied:
/// single choice is the only one (BR2). Every text is HTML written by a person; the Api sanitizes it (BR8).
/// </summary>
/// <param name="TopicId">The topic the question belongs to. Required (BR3).</param>
/// <param name="ContentLanguage">One of the app's languages, canonicalized (BR7). Required.</param>
/// <param name="StatementHtml">What the student is asked. Required, not empty once its tags are removed (BR3).</param>
/// <param name="BaseTextHtml">A text the question refers to, shown before the statement. Optional.</param>
/// <param name="ExplanationHtml">Why the key is right. Optional.</param>
/// <param name="Difficulty">A whole number from 1 to 5, or null (BR6).</param>
/// <param name="Options">The options in order; null counts as none (BR3).</param>
/// <param name="ExamEditionId">The past edition the question came from; null with no number and no notice subject means authored (F-96, BR1).</param>
/// <param name="PaperNumber">The question's number in that paper, from 1 to 500; required with an edition (F-96, BR2).</param>
/// <param name="NoticeSubjectId">A notice subject of that edition, or null (F-96, BR2).</param>
/// <param name="Type">
/// The type, chosen when the question is created; null on a create means single choice, and on an update it
/// must be null or the stored type (F-97, BR2).
/// </param>
/// <param name="TrueFalseKey">The key of a true/false question: true is "certo", false "errado", null not set yet (F-97, BR1).</param>
/// <param name="ChangeReason">
/// Why the answer key changed (F-98, BR5): plain text, 1 to 500 characters after trimming. Required when the save
/// creates a version whose key differs from the latest one; kept as a note on any other version the save creates
/// and ignored when it creates none.
/// </param>
public sealed record SaveQuestionRequest(
    Guid? TopicId = null,
    string? ContentLanguage = null,
    string? StatementHtml = null,
    string? BaseTextHtml = null,
    string? ExplanationHtml = null,
    int? Difficulty = null,
    IReadOnlyList<SaveQuestionOptionRequest>? Options = null,
    Guid? ExamEditionId = null,
    int? PaperNumber = null,
    Guid? NoticeSubjectId = null,
    QuestionType? Type = null,
    bool? TrueFalseKey = null,
    string? ChangeReason = null);
