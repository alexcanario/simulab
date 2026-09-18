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
}
