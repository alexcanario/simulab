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
/// <remarks>
/// F-8: <paramref name="FullName"/> and <paramref name="PreferredLanguage"/> are read from the account at
/// each call, so the Web can write the menu label and the culture cookie at sign-in (BR5, BR8).
/// </remarks>
public sealed record SessionInfoResponse(
    string Subject,
    string Email,
    string SessionJti,
    IReadOnlyList<string> Permissions,
    string? FullName = null,
    string? PreferredLanguage = null);
