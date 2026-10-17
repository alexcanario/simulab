namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// The body of a rejection (F-105, BR6). The body is optional on the wire so a missing one is answered with the
/// code of a missing reason, not with a framework error.
/// </summary>
/// <param name="Reason">Plain text, 1 to 500 characters after trimming; null or blank counts as missing.</param>
/// <param name="SubmittedAt">The send the reviewer saw; see <see cref="ApproveQuestionRequest.SubmittedAt"/>.</param>
public sealed record RejectQuestionRequest(string? Reason = null, DateTimeOffset? SubmittedAt = null);
