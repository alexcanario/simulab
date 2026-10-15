namespace Simulab.QuestionBank.Contracts;

/// <summary>The paper number transcription mode suggests for the next question of an edition (F-99, BR4).</summary>
/// <param name="PaperNumber">The highest number among the edition's live questions plus one, or 1 when it has none.</param>
public sealed record NextPaperNumberResponse(int PaperNumber);
