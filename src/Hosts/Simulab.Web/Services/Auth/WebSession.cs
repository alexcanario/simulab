namespace Simulab.Web.Services.Auth;

/// <summary>
/// What the Web keeps on the server for one signed-in browser (B-3, BR1). The cookie carries only the
/// web session id that finds this entry; the tokens never leave the server.
/// </summary>
/// <param name="ApiSessionJti">The Api's current session id. It changes on every refresh (F-5 BR5), which is why it is not the key.</param>
/// <param name="AccessToken">The bearer token for Api calls; refreshed by <see cref="WebSessionTokenAccessor"/>.</param>
/// <param name="RefreshToken">Single use (F-5 BR5): each refresh replaces it.</param>
/// <param name="AccessTokenExpiresAt">When <paramref name="AccessToken"/> stops working.</param>
/// <param name="Permissions">The effective permissions, re-read on each refresh and each check (F-6 BR5).</param>
/// <param name="CheckedAt">When the Api last confirmed the session; limits the checks to one a minute outside page loads (BR4).</param>
/// <param name="ExpiresAt">When the auth cookie that points at this entry expires (30 days after sign-in, no sliding); the entry goes with it.</param>
public sealed record WebSession(
    string ApiSessionJti,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    IReadOnlyList<string> Permissions,
    DateTimeOffset CheckedAt,
    DateTimeOffset ExpiresAt);
