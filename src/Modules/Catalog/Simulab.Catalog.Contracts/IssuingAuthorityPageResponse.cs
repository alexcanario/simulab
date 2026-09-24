namespace Simulab.Catalog.Contracts;

/// <summary>One page of issuing authorities and the total across all pages (rule: api-contracts).</summary>
public sealed record IssuingAuthorityPageResponse(IReadOnlyList<IssuingAuthorityResponse> Items, int Total);
