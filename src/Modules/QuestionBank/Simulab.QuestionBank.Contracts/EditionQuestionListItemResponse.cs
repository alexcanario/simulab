namespace Simulab.QuestionBank.Contracts;

/// <summary>One row of the "Questions" section of the edition page (F-96, BR10).</summary>
/// <param name="Id">The question.</param>
/// <param name="PaperNumber">Its number in the paper.</param>
/// <param name="StatementExcerpt">The plain-text statement cut at <see cref="QuestionLimits.ExcerptLength"/> characters.</param>
/// <param name="NoticeSubjectId">The notice subject, when it has one.</param>
/// <param name="NoticeSubjectLabel">The notice subject's label, when it has one and it resolves.</param>
/// <param name="Status">Draft or Published.</param>
/// <param name="ContentLanguage">The language the question is written in.</param>
public sealed record EditionQuestionListItemResponse(
    Guid Id,
    int PaperNumber,
    string StatementExcerpt,
    Guid? NoticeSubjectId,
    string? NoticeSubjectLabel,
    QuestionStatus Status,
    string ContentLanguage);
