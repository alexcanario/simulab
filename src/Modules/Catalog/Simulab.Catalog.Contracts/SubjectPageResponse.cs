namespace Simulab.Catalog.Contracts;

/// <summary>One page of subjects and the total across all pages (rule: api-contracts).</summary>
public sealed record SubjectPageResponse(IReadOnlyList<SubjectResponse> Items, int Total);
