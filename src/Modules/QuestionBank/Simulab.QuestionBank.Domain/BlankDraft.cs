namespace Simulab.QuestionBank.Domain;

/// <summary>One blank of a fill-in-the-blanks question as the Application hands it to the question (F-107, BR1).</summary>
/// <param name="Number">The number of its marker <c>[[n]]</c>.</param>
/// <param name="AcceptedAnswers">The accepted answers as written, plain text; the key trims and checks them.</param>
public sealed record BlankDraft(int Number, IReadOnlyList<string> AcceptedAnswers);
