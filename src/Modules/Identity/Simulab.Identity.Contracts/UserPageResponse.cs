namespace Simulab.Identity.Contracts;

/// <summary>One page of the user list and the total across all pages (rule: api-contracts, <c>{ items, total }</c>).</summary>
public sealed record UserPageResponse(IReadOnlyList<UserSummaryResponse> Items, int Total);
