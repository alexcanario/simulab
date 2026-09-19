namespace Simulab.Identity.Contracts;

/// <summary>
/// What the Web needs to build its own cookie right after sign-in (F-5): read from the access token's
/// own claims by the Api, which validates the token anyway, instead of the Web trying to decode it
/// (the token may be encrypted, and decoding it is not the Web's job either way).
/// </summary>
/// <remarks>
/// F-6, BR5: <paramref name="Permissions"/> is a snapshot for UI comfort only (menu visibility), read
/// once at sign-in and refreshed on the Web's own silent-refresh cadence. The Api never trusts it back;
/// every enforcement check re-reads the database through <see cref="IPermissionQueryService"/>.
/// </remarks>
public sealed record SessionInfoResponse(string Subject, string Email, string SessionJti, IReadOnlyList<string> Permissions);
