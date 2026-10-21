namespace Simulab.QuestionBank.Domain;

/// <summary>One left item of a matching question as the Application hands it to the question (F-106, BR1).</summary>
/// <param name="Text">The sanitized item text.</param>
/// <param name="MatchPosition">The position, from 0, of the option (right item) it matches; null while not set.</param>
public sealed record MatchItemDraft(SanitizedHtml Text, int? MatchPosition);
