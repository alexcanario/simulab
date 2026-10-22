namespace Simulab.QuestionBank.Contracts;

/// <summary>One blank of a fill-in-the-blanks question as stored (F-107).</summary>
/// <param name="Number">The number of its marker <c>[[n]]</c> in the statement.</param>
/// <param name="AcceptedAnswers">The accepted answers, trimmed, in the order the curator wrote them; empty on a draft blank with none yet.</param>
public sealed record BlankResponse(int Number, IReadOnlyList<string> AcceptedAnswers);
