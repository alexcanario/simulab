namespace Simulab.ExamEngine.Contracts;

/// <summary>The limits of the attempt routes, read by the Api and the tests (F-166, BR5).</summary>
public static class AttemptLimits
{
    /// <summary>The most characters one typed text of an answer has, whatever the question type.</summary>
    public const int TextMaxLength = 20_000;

    /// <summary>
    /// The most bytes the body of a save has: every blank of a fill-in-the-blanks answer at the longest text, every
    /// character escaped as six bytes in JSON, and room for the rest of the body.
    /// </summary>
    public const int SaveBodyMaxBytes = 1_500_000;
}
