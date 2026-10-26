namespace Simulab.ExamEngine.Contracts;

/// <summary>
/// Every failure code the attempt routes can return (F-166, rule: api-contracts). A code is stable: it is the
/// resource key the UI translates, so it is never renamed or reused. The field codes are the <c>code</c> of one
/// entry of <c>errors</c> in an <see cref="AnswerInvalid"/> answer.
/// </summary>
public static class ExamAttemptErrorCodes
{
    /// <summary>BR1: the edition is unknown or still a draft.</summary>
    public const string EditionNotFound = "exam_attempt.edition_not_found";

    /// <summary>BR1: the edition has no published question.</summary>
    public const string EditionWithoutQuestions = "exam_attempt.edition_without_questions";

    /// <summary>BR7: the attempt is unknown, or belongs to another student.</summary>
    public const string NotFound = "exam_attempt.not_found";

    /// <summary>BR5: the question is not one of the attempt.</summary>
    public const string QuestionNotInAttempt = "exam_attempt.question_not_in_attempt";

    /// <summary>BR5: the answer does not have the shape of its question; one entry per field says why.</summary>
    public const string AnswerInvalid = "exam_attempt.answer_invalid";

    /// <summary>BR6: the attempt was submitted and takes no more answers.</summary>
    public const string AlreadySubmitted = "exam_attempt.already_submitted";

    /// <summary>Field code: the body is of another type than the question, or its <c>type</c> is missing or unknown.</summary>
    public const string AnswerTypeMismatch = "exam_attempt.answer_type_mismatch";

    /// <summary>Field code: an option position that the question's version does not have.</summary>
    public const string OptionUnknown = "exam_attempt.option_unknown";

    /// <summary>Field code: a matching item position that the question's version does not have.</summary>
    public const string ItemUnknown = "exam_attempt.item_unknown";

    /// <summary>Field code: a blank number that the question's version does not have.</summary>
    public const string BlankUnknown = "exam_attempt.blank_unknown";

    /// <summary>Field code: a text above the limit of the question or above <see cref="AttemptLimits.TextMaxLength"/>.</summary>
    public const string TextTooLong = "exam_attempt.text_too_long";

    /// <summary>Field code: a required part of the answer is missing (no option chosen, no value, no text).</summary>
    public const string AnswerEmpty = "exam_attempt.answer_empty";

    /// <summary>Field code: a matching item or a blank number named twice; each has at most one answer.</summary>
    public const string EntryRepeated = "exam_attempt.entry_repeated";
}
