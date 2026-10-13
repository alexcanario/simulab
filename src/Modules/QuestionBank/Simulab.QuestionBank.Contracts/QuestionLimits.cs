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
    public const int ExplanationMaxLength = 10_000;
    public const int OptionMaxLength = 2_000;

    /// <summary>The fewest options a published question has (BR4).</summary>
    public const int OptionsMin = 2;

    /// <summary>The most options a question has, draft or published (BR3).</summary>
    public const int OptionsMax = 5;

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
}
