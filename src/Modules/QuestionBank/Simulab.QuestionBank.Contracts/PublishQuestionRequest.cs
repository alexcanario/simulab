namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// The optional body of a publish (F-98, BR5): the reason for an answer key change that the publish records.
/// A publish with no body is still valid; the reason is only needed when the publish creates a version whose
/// key differs from the latest one.
/// </summary>
/// <param name="ChangeReason">Plain text, 1 to 500 characters after trimming; null or blank counts as missing.</param>
public sealed record PublishQuestionRequest(string? ChangeReason = null);
