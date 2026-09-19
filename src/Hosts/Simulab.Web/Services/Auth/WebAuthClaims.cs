namespace Simulab.Web.Services.Auth;

/// <summary>
/// Claim types the Web's own auth cookie carries besides the standard ones. Since B-3 the cookie holds no
/// token: only the id of the server-side <see cref="WebSession"/> that does.
/// </summary>
public static class WebAuthClaims
{
    /// <summary>Finds the <see cref="WebSession"/> in <see cref="IWebSessionStore"/> (B-3, BR1). Stable for the cookie's life.</summary>
    public const string WebSessionId = "simulab:web_session";

    /// <summary>
    /// One claim per effective permission (F-6, BR5): a UI-comfort copy, added to the principal on each
    /// request from the stored session (B-3, BR4), never written to the cookie. Never the security boundary:
    /// every enforcement check still asks the Api, which never trusts a claim.
    /// </summary>
    public const string Permission = "simulab:permission";
}
