namespace Simulab.QuestionBank.Domain;

/// <summary>One option as the Application hands it to the question: its sanitized text and whether it is the key.</summary>
/// <param name="Text">The sanitized option text.</param>
/// <param name="IsCorrect">True for the answer key.</param>
public sealed record OptionDraft(SanitizedHtml Text, bool IsCorrect);
