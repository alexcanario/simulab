namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// The body of an approval (F-105, BR5). The body is optional on the wire: a missing one is an approval with no
/// note.
/// </summary>
/// <param name="Note">Plain text, 1 to 500 characters after trimming; null or blank means none. It also serves as the
/// reason of the version when the answer key differs from the latest version's.</param>
/// <param name="SubmittedAt">The send the reviewer saw (<see cref="QuestionResponse.SubmittedAt"/>); when it differs
/// from the latest send the answer is 409 <c>question.version_conflict</c>. Null skips the check.</param>
public sealed record ApproveQuestionRequest(string? Note = null, DateTimeOffset? SubmittedAt = null);
