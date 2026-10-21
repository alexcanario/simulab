namespace Simulab.QuestionBank.Contracts;

/// <summary>One left item of a matching question as stored (F-106). The number is the position counted from 1, never stored.</summary>
/// <param name="Position">Where the item sits, from 0.</param>
/// <param name="TextHtml">The sanitized HTML that was stored.</param>
/// <param name="MatchPosition">The position of the option (right item) it matches; null while not set on a draft.</param>
public sealed record MatchItemResponse(int Position, string TextHtml, int? MatchPosition);
