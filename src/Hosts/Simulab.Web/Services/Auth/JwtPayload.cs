using System.Text.Json;

namespace Simulab.Web.Services.Auth;

/// <summary>
/// Reads the claims out of an access token's own payload. OpenIddict issues self-contained JWTs, and
/// the Web only needs <c>sub</c>, <c>email</c> and <c>session_jti</c> to build its own cookie (F-5) - it
/// never re-verifies the signature, since the token only ever travels straight back from the Api that
/// just issued it, over the same connection its own client credentials were trusted on.
/// </summary>
public static class JwtPayload
{
    public static IReadOnlyDictionary<string, string> ReadClaims(string accessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var segments = accessToken.Split('.');
        if (segments.Length < 2)
        {
            return new Dictionary<string, string>();
        }

        var json = Convert.FromBase64String(PadBase64Url(segments[1]));
        using var document = JsonDocument.Parse(json);

        return document.RootElement.EnumerateObject()
            .Where(property => property.Value.ValueKind == JsonValueKind.String)
            .ToDictionary(property => property.Name, property => property.Value.GetString()!, StringComparer.Ordinal);
    }

    private static string PadBase64Url(string segment)
    {
        var base64 = segment.Replace('-', '+').Replace('_', '/');
        return base64.PadRight(base64.Length + ((4 - (base64.Length % 4)) % 4), '=');
    }
}
