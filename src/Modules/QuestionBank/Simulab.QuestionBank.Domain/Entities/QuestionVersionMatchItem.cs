namespace Simulab.QuestionBank.Domain.Entities;

/// <summary>
/// One left item of a <see cref="QuestionVersion"/> of a matching question (F-106, BR8): a copy of what the item held
/// when the version was recorded. It lives inside the version's JSON column and is never changed.
/// </summary>
public sealed class QuestionVersionMatchItem
{
    /// <summary>Where the item sat, from 0; the number is the position counted from 1.</summary>
    public required int Position { get; init; }

    /// <summary>The sanitized HTML of the item text.</summary>
    public required string TextHtml { get; init; }

    /// <summary>The position of the option it matched; null while it was not set.</summary>
    public int? MatchPosition { get; init; }
}
