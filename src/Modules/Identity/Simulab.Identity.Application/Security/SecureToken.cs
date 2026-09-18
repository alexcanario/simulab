using System.Security.Cryptography;

namespace Simulab.Identity.Application.Security;

/// <summary>
/// The verification link value. The raw token goes in the email and nowhere else; the database keeps
/// only the hash, so reading the table does not let anyone activate an account (BR8).
/// </summary>
public static class SecureToken
{
    private const int TokenBytes = 32;

    /// <summary>A new random token and the hash to store.</summary>
    public static (string RawToken, string TokenHash) Generate()
    {
        var raw = Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));
        return (raw, Hash(raw));
    }

    /// <summary>
    /// SHA-256 of the raw token. A verification token is high-entropy and single-use, so a plain hash
    /// is enough: there is nothing to guess and nothing to slow down.
    /// </summary>
    public static string Hash(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);
        return Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
