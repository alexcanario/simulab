namespace Simulab.QuestionBank.Domain.Entities;

/// <summary>
/// One option of a <see cref="QuestionVersion"/> (F-98, BR1): a copy of what the option held when the version was
/// recorded. It lives inside the version's JSON column, never in a table of its own, and is never changed.
/// </summary>
public sealed class QuestionVersionOption
{
    /// <summary>Where the option sat, from 0; the letter is the position counted from A.</summary>
    public required int Position { get; init; }

    /// <summary>The sanitized HTML of the option text.</summary>
    public required string TextHtml { get; init; }

    /// <summary>True for the answer key.</summary>
    public required bool IsCorrect { get; init; }
}
