namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// One option of a question as the form sends it. Its position is its index in the array (BR5), so the
/// order the curator sees is the order that is stored.
/// </summary>
/// <param name="TextHtml">The option text as HTML written by a person; the Api sanitizes it (BR8).</param>
/// <param name="IsCorrect">True for the option that is the answer key.</param>
public sealed record SaveQuestionOptionRequest(string? TextHtml = null, bool IsCorrect = false);
