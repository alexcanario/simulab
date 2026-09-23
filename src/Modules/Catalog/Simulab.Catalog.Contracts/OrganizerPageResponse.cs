namespace Simulab.Catalog.Contracts;

/// <summary>One page of organizers and the total across all pages (rule: api-contracts).</summary>
public sealed record OrganizerPageResponse(IReadOnlyList<OrganizerResponse> Items, int Total);
