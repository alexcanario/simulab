using System.Security.Cryptography;
using System.Text;

namespace Simulab.Web.Services.Auth;

/// <summary>
/// A first Google sign-in waiting for the confirmation page (F-20 BR8). The ID token stays on this server; the page
/// gets only the address and the name. The ticket belongs to the browser that came back from Google: it keeps a
/// secret in an HttpOnly cookie, and only the hash of that secret is here, so the ticket id alone (in the URL, a
/// browser history, a log) opens nothing.
/// </summary>
public sealed record GoogleSignUpTicket(string IdToken, string Email, string? Name, string BindingHash)
{
    /// <summary>The cookie that holds the browser's secret, sent only to the confirmation page.</summary>
    public const string BindingCookie = "simulab.google.signup";

    /// <summary>A new secret for the cookie, and its hash for the ticket.</summary>
    public static (string Secret, string Hash) NewBinding()
    {
        var secret = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        return (secret, Hash(secret));
    }

    /// <summary>Whether <paramref name="secret"/> is the one this ticket was issued with. Constant time.</summary>
    public bool IsHeldBy(string? secret) =>
        !string.IsNullOrEmpty(secret)
        && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Hash(secret)), Encoding.ASCII.GetBytes(BindingHash));

    private static string Hash(string secret) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
}
