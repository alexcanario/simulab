namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// One left item of a matching question as the form sends it (F-106, BR1). Its number is its index in the array, so
/// the order the curator sees is the order that is stored.
/// </summary>
/// <param name="TextHtml">The item text as HTML written by a person; the Api sanitizes it (F-95, BR8).</param>
/// <param name="MatchPosition">The position, from 0, of the option (right item) this item matches; null while not set.</param>
public sealed record SaveMatchItemRequest(string? TextHtml = null, int? MatchPosition = null);
