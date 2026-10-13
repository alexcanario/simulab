namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// The body of a reinstatement (F-101, BR6): why an annulled question is published again. The body is
/// optional on the wire so a missing one is answered with the code of a missing reason.
/// </summary>
/// <param name="Reason">Plain text, 1 to 500 characters after trimming; null or blank counts as missing.</param>
public sealed record ReinstateQuestionRequest(string? Reason = null);
