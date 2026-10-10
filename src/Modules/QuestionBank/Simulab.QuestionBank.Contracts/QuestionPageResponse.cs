namespace Simulab.QuestionBank.Contracts;

/// <summary>One page of questions and the total across all pages (rule: api-contracts).</summary>
public sealed record QuestionPageResponse(IReadOnlyList<QuestionListItemResponse> Items, int Total);
