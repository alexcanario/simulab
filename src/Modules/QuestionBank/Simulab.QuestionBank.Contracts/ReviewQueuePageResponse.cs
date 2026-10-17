namespace Simulab.QuestionBank.Contracts;

/// <summary>One page of the review queue and the total across all pages (F-105, BR8; rule: api-contracts).</summary>
public sealed record ReviewQueuePageResponse(IReadOnlyList<ReviewQueueItemResponse> Items, int Total);
