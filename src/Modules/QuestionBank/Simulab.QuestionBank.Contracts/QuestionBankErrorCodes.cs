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

    /// <summary>F-97 BR2: an update names another type than the stored one; the type never changes.</summary>
    public const string QuestionTypeChanged = "question.type_changed";

    /// <summary>F-97 BR3: a true/false question was sent options (a non-empty list); nothing is truncated or ignored.</summary>
    public const string QuestionOptionsNotAllowed = "question.options_not_allowed";

    /// <summary>F-97 BR3: a single-choice question was sent a true/false key.</summary>
    public const string QuestionKeyNotAllowed = "question.key_not_allowed";

    /// <summary>F-97 BR4: a published true/false question needs its key; a publish or an edit left it empty.</summary>
    public const string QuestionTrueFalseKeyRequired = "question.true_false_key_required";

    /// <summary>F-98 BR5: a save or a publish would create a version whose answer key changed and gave no reason.</summary>
    public const string QuestionKeyChangeReasonRequired = "question.key_change_reason_required";

    /// <summary>F-98 BR5: the reason of a change has more than 500 characters.</summary>
    public const string QuestionChangeReasonTooLong = "question.change_reason_too_long";

    /// <summary>F-98 BR8: the question has no version with that number.</summary>
    public const string QuestionVersionNotFound = "question.version_not_found";

    /// <summary>F-98 BR10: another save created the next version number first; reload and try again.</summary>
    public const string QuestionVersionConflict = "question.version_conflict";

    /// <summary>F-101 BR5: the question is annulled and frozen; save, publish, unpublish and delete are refused.</summary>
    public const string QuestionAnnulled = "question.annulled";

    /// <summary>F-101 BR1: the question is already annulled.</summary>
    public const string QuestionAlreadyAnnulled = "question.already_annulled";

    /// <summary>F-101 BR6: only an annulled question is reinstated.</summary>
    public const string QuestionNotAnnulled = "question.not_annulled";

    /// <summary>F-101 BR3: an annulment or a reinstatement has no reason (missing, blank or no body).</summary>
    public const string QuestionStatusReasonRequired = "question.status_reason_required";

    /// <summary>F-101 BR3: the reason of an annulment or a reinstatement has more than 500 characters.</summary>
    public const string QuestionStatusReasonTooLong = "question.status_reason_too_long";

    /// <summary>F-102 BR7: the base text the question links is unknown or deleted.</summary>
    public const string QuestionBaseTextNotFound = "question.base_text_not_found";

    /// <summary>F-102 BR3: the question and the base text it links are in different content languages.</summary>
    public const string QuestionBaseTextLanguageMismatch = "question.base_text_language_mismatch";

    /// <summary>F-102: the base text is unknown or deleted.</summary>
    public const string BaseTextNotFound = "base_text.not_found";

    /// <summary>F-102 BR1: the title is empty once trimmed.</summary>
    public const string BaseTextTitleRequired = "base_text.title_required";

    public const string BaseTextTitleTooLong = "base_text.title_too_long";

    /// <summary>F-102 BR1: the text is empty once its tags are removed.</summary>
    public const string BaseTextTextRequired = "base_text.text_required";

    public const string BaseTextTextTooLong = "base_text.text_too_long";
    public const string BaseTextContentLanguageInvalid = "base_text.content_language_invalid";

    /// <summary>F-102 BR6: a live question still links the base text; remove the links first.</summary>
    public const string BaseTextInUse = "base_text.in_use";

    /// <summary>F-102 BR3: the language of a base text that has linked questions cannot change.</summary>
    public const string BaseTextLanguageInUse = "base_text.language_in_use";
}
