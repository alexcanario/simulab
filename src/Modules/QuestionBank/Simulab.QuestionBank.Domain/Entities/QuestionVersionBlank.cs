namespace Simulab.QuestionBank.Domain.Entities;

/// <summary>
/// One blank of a <see cref="QuestionVersion"/> of a fill-in-the-blanks question (F-107, BR10): a copy of what the blank
/// held when the version was recorded. It lives inside the version's JSON column, in statement order, and is never changed.
/// </summary>
public sealed class QuestionVersionBlank
{
    /// <summary>The number of its marker.</summary>
    public required int Number { get; init; }

    /// <summary>The accepted answers, trimmed, in the order the curator wrote them.</summary>
    public required List<string> AcceptedAnswers { get; init; }
}
