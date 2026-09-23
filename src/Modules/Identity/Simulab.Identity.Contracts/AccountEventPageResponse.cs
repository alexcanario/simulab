namespace Simulab.Identity.Contracts;

/// <summary>
/// One page of the account event trail and the total across all pages (rule: api-contracts, <c>{ items, total }</c>).
/// <paramref name="Account"/> is the account asked for by <see cref="AccountEventListQuery.UserId"/>, so the screen
/// can name it on its chip even when that account has no event yet; null when no account was asked for, and its
/// email is null when the account was erased (BR12). F-14 needs a filters endpoint for this; here one field does.
/// </summary>
public sealed record AccountEventPageResponse(
    IReadOnlyList<AccountEventResponse> Items,
    int Total,
    AccountEventAccountResponse? Account = null);
