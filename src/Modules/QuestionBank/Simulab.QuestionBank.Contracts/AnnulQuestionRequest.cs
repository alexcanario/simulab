namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// The body of an annulment (F-101, BR3): why the question is annulled. The body is optional on the wire so a
/// missing one is answered with the code of a missing reason, not with a framework error.
/// </summary>
/// <param name="Reason">Plain text, 1 to 500 characters after trimming; null or blank counts as missing.</param>
public sealed record AnnulQuestionRequest(string? Reason = null);
