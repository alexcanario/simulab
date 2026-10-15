namespace Simulab.QuestionBank.Contracts;

/// <summary>One page of base texts and the total across all pages (rule: api-contracts).</summary>
public sealed record BaseTextPageResponse(IReadOnlyList<BaseTextListItemResponse> Items, int Total);
