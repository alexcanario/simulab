namespace Simulab.QuestionBank.Contracts;

/// <summary>One page of the questions of an edition, in paper order, and the total across all pages (F-96, BR10).</summary>
public sealed record EditionQuestionPageResponse(IReadOnlyList<EditionQuestionListItemResponse> Items, int Total);
