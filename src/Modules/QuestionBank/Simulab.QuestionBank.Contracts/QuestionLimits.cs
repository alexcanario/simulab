namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// The question bank's limits: the width of their columns, read by the Api (the authority), the EF mapping
/// and the Web (comfort checks). One number per limit (F-95, BR9; rule from B-7 BR4). Text limits count
/// characters of sanitized HTML.
/// </summary>
public static class QuestionLimits
{
    public const int StatementMaxLength = 10_000;
    public const int BaseTextMaxLength = 20_000;

    /// <summary>The most characters the title of a base text has, after trimming (F-102, BR1).</summary>
    public const int BaseTextTitleMaxLength = 200;
    public const int ExplanationMaxLength = 10_000;
    public const int OptionMaxLength = 2_000;

    /// <summary>The fewest options a published question has (BR4).</summary>
    public const int OptionsMin = 2;

    /// <summary>The most options a question has, draft or published (BR3).</summary>
    public const int OptionsMax = 5;

    /// <summary>The fewest options a published multiple-answer question has: two correct and one wrong (F-104, BR4).</summary>
    public const int MultipleAnswerOptionsMin = 3;

    /// <summary>The fewest correct options a published multiple-answer question has (F-104, BR4).</summary>
    public const int MultipleAnswerCorrectMin = 2;

    /// <summary>The fewest left items a published matching question has (F-106, BR4).</summary>
    public const int MatchingItemsMin = 2;

    /// <summary>The most items a matching question has on each side, draft or published (F-106, BR3).</summary>
    public const int MatchingItemsMax = 8;

    /// <summary>The most blanks (markers in the statement) a fill-in-the-blanks question has, draft or published (F-107, BR4).</summary>
    public const int BlanksMax = 10;

    /// <summary>The highest number a blank marker <c>[[n]]</c> may carry; the lowest is 1 (F-107, BR3).</summary>
    public const int BlankNumberMax = 99;

    /// <summary>The most accepted answers a blank has (F-107, BR4).</summary>
    public const int BlankAnswersMax = 5;

    /// <summary>The most characters an accepted answer has, after trimming (F-107, BR4).</summary>
    public const int BlankAnswerMaxLength = 100;

    /// <summary>The most criteria a rubric has, draft or published (F-109, BR3).</summary>
    public const int RubricCriteriaMax = 10;

    /// <summary>The most characters a criterion name has, after trimming (F-109, BR3).</summary>
    public const int CriterionNameMaxLength = 100;

    /// <summary>The most characters a criterion description has, after trimming (F-109, BR3).</summary>
    public const int CriterionDescriptionMaxLength = 500;

    /// <summary>The fewest maximum points of a criterion (F-109, BR3).</summary>
    public const int CriterionPointsMin = 1;

    /// <summary>The most maximum points of a criterion (F-109, BR3).</summary>
    public const int CriterionPointsMax = 100;

    /// <summary>The most lines an essay size limit has, for the minimum and the maximum; the fewest is 1 (F-109, BR7).</summary>
    public const int EssayLinesMax = 100;

    /// <summary>The most characters an open-answer size limit has; the fewest is 1 (F-109, BR7).</summary>
    public const int OpenAnswerCharactersMax = 5000;

    public const int DifficultyMin = 1;
    public const int DifficultyMax = 5;

    /// <summary>The lowest number a question has in its paper (F-96, BR2).</summary>
    public const int PaperNumberMin = 1;

    /// <summary>The highest number a question has in its paper; the notice subject's question count shares the cap (F-96, BR2).</summary>
    public const int PaperNumberMax = 500;

    /// <summary>How many characters of the plain statement the list shows (BR10).</summary>
    public const int ExcerptLength = 200;

    /// <summary>The width of the content language column: a code such as pt-BR.</summary>
    public const int ContentLanguageMaxLength = 16;

    /// <summary>The most characters the reason of a change may have, after trimming (F-98, BR5).</summary>
    public const int ChangeReasonMaxLength = 500;

    /// <summary>The largest image file, in bytes: 2 MB (F-103, BR2).</summary>
    public const long ImageMaxBytes = 2L * 1024 * 1024;

    /// <summary>The most images a question (statement and options together) or a base text holds (F-103, BR7).</summary>
    public const int ImagesPerTextMax = 10;

    /// <summary>The most characters the alternative text of an image has (F-103, BR4).</summary>
    public const int ImageAltMaxLength = 250;
}
