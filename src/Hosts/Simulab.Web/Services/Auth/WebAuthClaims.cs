namespace Simulab.Web.Services.Auth;

/// <summary>
/// Claim types the Web's own auth cookie carries besides the standard ones (F-5, build decision): the
/// access and refresh tokens travel inside the same encrypted, HttpOnly cookie ASP.NET Core already
/// writes for cookie authentication, never anywhere the browser's JavaScript can read them.
/// </summary>
public static class WebAuthClaims
{
    public const string AccessToken = "simulab:access_token";

    public const string RefreshToken = "simulab:refresh_token";

    /// <summary>
    /// One claim per effective permission (F-6, BR5): a UI-comfort snapshot, read once at sign-in. Never
    /// the security boundary - every enforcement check still asks the Api, which never trusts a claim.
    /// </summary>
    public const string Permission = "simulab:permission";
}
