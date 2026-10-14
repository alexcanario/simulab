namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// One row of a question's history (F-98, UC5): enough to draw the History card without reading every
/// snapshot. The list is newest first.
/// </summary>
/// <param name="Id">The version's stable id; an attempt of a later simulator may name it (UC6).</param>
/// <param name="Number">1, 2, ...; unique for the question.</param>
/// <param name="CreatedAt">When the version was recorded.</param>
/// <param name="AnswerKeyChanged">True when the answer key differs from the previous version's (BR4).</param>
/// <param name="Reason">The reason the curator wrote, or null; always set when <paramref name="AnswerKeyChanged"/> is.</param>
/// <param name="IsCurrent">True for the highest number (BR7).</param>
/// <param name="Annulled">True when the question was annulled when this version was recorded (F-101, BR2).</param>
/// <param name="Reinstated">True for the version that took an annulled question back to published (F-101, BR6).</param>
public sealed record QuestionVersionListItemResponse(
    Guid Id,
    int Number,
    DateTimeOffset CreatedAt,
    bool AnswerKeyChanged,
    string? Reason,
    bool IsCurrent,
    bool Annulled,
    bool Reinstated);
