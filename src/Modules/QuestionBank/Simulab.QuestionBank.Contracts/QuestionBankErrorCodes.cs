namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// Every failure code this module can return (rule: api-contracts). A code is stable: it is the
/// resource key the UI translates, so it is never renamed or reused.
/// </summary>
public static class QuestionBankErrorCodes
{
    /// <summary>BR16: the id is unknown or the question was deleted.</summary>
    public const string QuestionNotFound = "question.not_found";

    /// <summary>BR16: the topic is unknown or deleted, or none was sent.</summary>
    public const string QuestionTopicNotFound = "question.topic_not_found";

    /// <summary>BR3: the statement is empty once its tags are removed.</summary>
    public const string QuestionStatementRequired = "question.statement_required";

    public const string QuestionStatementTooLong = "question.statement_too_long";
    public const string QuestionBaseTextTooLong = "question.base_text_too_long";
    public const string QuestionExplanationTooLong = "question.explanation_too_long";
    public const string QuestionOptionTooLong = "question.option_too_long";

    /// <summary>BR3 and BR4: more than five options, or fewer than two on a published question.</summary>
    public const string QuestionOptionsCount = "question.options_count";

    /// <summary>BR4: a published question has an option with no text.</summary>
    public const string QuestionOptionTextRequired = "question.option_text_required";

    /// <summary>BR4: a published question has no correct option, or more than one.</summary>
    public const string QuestionKeyInvalid = "question.key_invalid";

    public const string QuestionDifficultyInvalid = "question.difficulty_invalid";
    public const string QuestionContentLanguageInvalid = "question.content_language_invalid";

    /// <summary>BR11: only a draft is deleted; unpublish first.</summary>
    public const string QuestionDeleteRequiresDraft = "question.delete_requires_draft";

    public const string QuestionAlreadyPublished = "question.already_published";
    public const string QuestionNotPublished = "question.not_published";

    /// <summary>F-96 BR2: the edition is unknown or deleted.</summary>
    public const string QuestionExamEditionNotFound = "question.exam_edition_not_found";

    /// <summary>F-96 BR2: the notice subject is unknown, deleted or belongs to another edition.</summary>
    public const string QuestionNoticeSubjectNotFound = "question.notice_subject_not_found";

    /// <summary>F-96 BR2: an edition was given with no paper number.</summary>
    public const string QuestionPaperNumberRequired = "question.paper_number_required";

    /// <summary>F-96 BR2: the paper number is not a whole number from 1 to 500.</summary>
    public const string QuestionPaperNumberInvalid = "question.paper_number_invalid";

    /// <summary>F-96 BR2: a paper number or a notice subject was given with no edition.</summary>
    public const string QuestionOriginRequiresEdition = "question.origin_requires_edition";

    /// <summary>F-96 BR3: a live question of the edition already has this number.</summary>
    public const string QuestionPaperNumberTaken = "question.paper_number_taken";

    /// <summary>B-28 BR2: the subject whose topics were asked for is unknown or deleted.</summary>
    public const string QuestionSubjectNotFound = "question.subject_not_found";
}
