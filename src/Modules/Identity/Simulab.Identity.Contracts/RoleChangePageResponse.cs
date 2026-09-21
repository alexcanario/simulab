namespace Simulab.Identity.Contracts;

/// <summary>One page of the role history and the total across all pages (rule: api-contracts, <c>{ items, total }</c>).</summary>
public sealed record RoleChangePageResponse(IReadOnlyList<RoleChangeResponse> Items, int Total);
