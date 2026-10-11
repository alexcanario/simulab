namespace Simulab.QuestionBank.Contracts;

/// <summary>One option as stored (F-95). The letter is the position counted from A, never stored (BR5).</summary>
/// <param name="Position">Where the option sits, from 0.</param>
/// <param name="TextHtml">The sanitized HTML that was stored.</param>
/// <param name="IsCorrect">True for the answer key.</param>
public sealed record QuestionOptionResponse(int Position, string TextHtml, bool IsCorrect);
