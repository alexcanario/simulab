namespace Simulab.Identity.Application.GoogleSignIn;

/// <summary>Checks a Google ID token against Google's published keys (F-20 BR2).</summary>
public interface IGoogleIdTokenValidator
{
    /// <summary>The identity the token carries, or null when any check fails (issuer, audience, signature, lifetime, shape).</summary>
    Task<GoogleIdentity?> ValidateAsync(string? idToken, CancellationToken cancellationToken = default);
}
