namespace Simulab.Identity.Contracts;

/// <summary>
/// One entry of the account event trail (F-21, BR2). <paramref name="Account"/> is null when no account matched
/// the attempt, and its email is null for an account erased since (BR12). <paramref name="Method"/> is set on a
/// sign-in, <paramref name="Reason"/> on a failure; <paramref name="IpAddress"/> is null when the host did not
/// know it or an erasure cleared it (BR9). The screen translates <paramref name="Event"/>, the method and the
/// reason by key.
/// </summary>
public sealed record AccountEventResponse(
    Guid Id,
    DateTimeOffset OccurredAt,
    AccountEventAccountResponse? Account,
    string Event,
    string? Method,
    string? Reason,
    string? IpAddress);
