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

    /// <summary>F-104 BR4: a published multiple-answer question needs from 3 to 5 options.</summary>
    public const string QuestionMultipleOptionsCount = "question.multiple_options_count";

    /// <summary>F-104 BR4: a published multiple-answer question needs two or more correct options and at least one wrong.</summary>
    public const string QuestionMultipleKeyInvalid = "question.multiple_key_invalid";

    /// <summary>F-106 BR3, BR4: a matching question has more than 8 items on either side, or is published with fewer than 2 on the left or fewer on the right than on the left.</summary>
    public const string QuestionMatchingItemsCount = "question.matching_items_count";

    /// <summary>F-106 BR3, BR4: a left item names a right item that does not exist, or a published one has no match.</summary>
    public const string QuestionMatchingKeyInvalid = "question.matching_key_invalid";

    /// <summary>F-106 BR3: two left items name the same right item; the match is one to one.</summary>
    public const string QuestionMatchingKeyRepeated = "question.matching_key_repeated";

    /// <summary>F-106 BR4: a published matching question has a left item with no text.</summary>
    public const string QuestionMatchItemTextRequired = "question.match_item_text_required";

    /// <summary>F-106 BR7: a left item is longer than the option limit.</summary>
    public const string QuestionMatchItemTooLong = "question.match_item_too_long";

    /// <summary>F-106 BR6: a question of another type was sent left items (a non-empty list); nothing is ignored.</summary>
    public const string QuestionMatchItemsNotAllowed = "question.match_items_not_allowed";

    /// <summary>F-107 BR4, BR5: a fill-in-the-blanks statement has more than 10 markers, or is published with none.</summary>
    public const string QuestionBlanksCount = "question.blanks_count";

    /// <summary>F-107 BR4: the same blank number appears twice, in the statement or in the blanks sent.</summary>
    public const string QuestionBlankRepeated = "question.blank_repeated";

    /// <summary>F-107 BR4: a blank was sent for a number with no marker in the statement.</summary>
    public const string QuestionBlankMarkerMissing = "question.blank_marker_missing";

    /// <summary>F-107 BR5: a published fill-in-the-blanks question has a blank with no accepted answer. The problem's <c>position</c> is the blank number.</summary>
    public const string QuestionBlankAnswersRequired = "question.blank_answers_required";

    /// <summary>F-107 BR4: a blank has more than 5 accepted answers. The problem's <c>position</c> is the blank number.</summary>
    public const string QuestionBlankAnswersCount = "question.blank_answers_count";

    /// <summary>F-107 BR4: an accepted answer is empty once trimmed. The problem's <c>position</c> is the blank number.</summary>
    public const string QuestionBlankAnswerRequired = "question.blank_answer_required";

    /// <summary>F-107 BR4: an accepted answer has more than 100 characters once trimmed. The problem's <c>position</c> is the blank number.</summary>
    public const string QuestionBlankAnswerTooLong = "question.blank_answer_too_long";

    /// <summary>F-107 BR4, BR6: two accepted answers of one blank are equal under the comparison rule. The problem's <c>position</c> is the blank number.</summary>
    public const string QuestionBlankAnswerRepeated = "question.blank_answer_repeated";

    /// <summary>F-107 BR7: a question of another type was sent blanks (a non-empty list); nothing is ignored.</summary>
    public const string QuestionBlanksNotAllowed = "question.blanks_not_allowed";

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

    /// <summary>F-105 BR3: the question is in review and frozen; save, publish, unpublish and delete are refused.</summary>
    public const string QuestionInReview = "question.in_review";

    /// <summary>F-105 BR2: the question is already in review.</summary>
    public const string QuestionAlreadyInReview = "question.already_in_review";

    /// <summary>F-105 BR4, BR5, BR6: only a question in review is withdrawn, approved or rejected.</summary>
    public const string QuestionNotInReview = "question.not_in_review";

    /// <summary>F-105 BR6: a rejection has no reason (missing, blank or no body).</summary>
    public const string QuestionReviewReasonRequired = "question.review_reason_required";

    /// <summary>F-105 BR5, BR6: the reason of a rejection or the note of an approval has more than 500 characters.</summary>
    public const string QuestionReviewReasonTooLong = "question.review_reason_too_long";

    /// <summary>F-103 BR2: the file is not a PNG, JPEG, WebP or GIF image (decided by its first bytes), or no file was sent.</summary>
    public const string QuestionImageNotAnImage = "question_image.not_an_image";

    /// <summary>F-103 BR2: the file is larger than 2 MB.</summary>
    public const string QuestionImageTooLarge = "question_image.too_large";

    /// <summary>F-103 BR12: the image is unknown, deleted, or its file is gone.</summary>
    public const string QuestionImageNotFound = "question_image.not_found";

    /// <summary>F-103 BR4: an image in the question's texts has no alternative text attribute.</summary>
    public const string QuestionImageAltRequired = "question.image_alt_required";

    /// <summary>F-103 BR4 (change note v2): the alternative text of an image has more than 250 characters.</summary>
    public const string QuestionImageAltTooLong = "question.image_alt_too_long";

    /// <summary>F-103 BR6: an image id in the question's texts does not exist or belongs to another question or base text.</summary>
    public const string QuestionImageNotFoundOnSave = "question.image_not_found";

    /// <summary>F-103 BR7: the statement and options hold more than 10 images.</summary>
    public const string QuestionImagesTooMany = "question.images_too_many";

    /// <summary>F-103 BR4: an image in the base text has no alternative text attribute.</summary>
    public const string BaseTextImageAltRequired = "base_text.image_alt_required";

    /// <summary>F-103 BR4 (change note v2): the alternative text of an image has more than 250 characters.</summary>
    public const string BaseTextImageAltTooLong = "base_text.image_alt_too_long";

    /// <summary>F-103 BR6: an image id in the base text does not exist or belongs to another question or base text.</summary>
    public const string BaseTextImageNotFound = "base_text.image_not_found";

    /// <summary>F-103 BR7: the base text holds more than 10 images.</summary>
    public const string BaseTextImagesTooMany = "base_text.images_too_many";
}
