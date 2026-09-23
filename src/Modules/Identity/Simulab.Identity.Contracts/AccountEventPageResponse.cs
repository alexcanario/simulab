namespace Simulab.Identity.Contracts;

/// <summary>One page of the account event trail and the total across all pages (rule: api-contracts, <c>{ items, total }</c>).</summary>
public sealed record AccountEventPageResponse(IReadOnlyList<AccountEventResponse> Items, int Total);
