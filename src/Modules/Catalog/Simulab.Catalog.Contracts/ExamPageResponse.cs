namespace Simulab.Catalog.Contracts;

/// <summary>One page of exams and the total across all pages (rule: api-contracts).</summary>
public sealed record ExamPageResponse(IReadOnlyList<ExamResponse> Items, int Total);
