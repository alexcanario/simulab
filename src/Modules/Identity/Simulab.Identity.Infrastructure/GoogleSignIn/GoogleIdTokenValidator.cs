using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Simulab.Identity.Application.GoogleSignIn;

namespace Simulab.Identity.Infrastructure.GoogleSignIn;

/// <summary>
/// F-20 BR2: checks a Google ID token here, never trusting what the Web host read from it. The keys come from
/// Google's discovery document through <paramref name="configuration"/>, which caches and refreshes them; the tests
/// replace it with keys of their own, so no test calls Google.
/// </summary>
public sealed class GoogleIdTokenValidator(
    IConfigurationManager<OpenIdConnectConfiguration> configuration,
    IOptions<GoogleClientOptions> options,
    ILogger<GoogleIdTokenValidator> logger) : IGoogleIdTokenValidator
{
    /// <summary>Google's issuer, in both spellings its tokens use.</summary>
    public static readonly IReadOnlyList<string> Issuers = ["https://accounts.google.com", "accounts.google.com"];

    /// <summary>Where Google publishes its discovery document and keys.</summary>
    public const string DiscoveryDocument = "https://accounts.google.com/.well-known/openid-configuration";

    /// <summary>An ID token far larger than this is not one Google issued; it is refused before any parsing.</summary>
    private const int MaxTokenLength = 8192;

    private readonly JsonWebTokenHandler _handler = new() { MapInboundClaims = false };

    public async Task<GoogleIdentity?> ValidateAsync(string? idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken) || idToken.Length > MaxTokenLength || string.IsNullOrWhiteSpace(options.Value.ClientId))
        {
            return null;
        }

        var result = await ValidateAgainstKeysAsync(idToken, cancellationToken);
        if (result.Exception is SecurityTokenSignatureKeyNotFoundException)
        {
            // Google rotated its keys since the last fetch: read them again once, then decide.
            configuration.RequestRefresh();
            result = await ValidateAgainstKeysAsync(idToken, cancellationToken);
        }

        if (!result.IsValid)
        {
            // English, for the log only; the caller answers with a code (rule: i18n).
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("A Google ID token was refused: {Reason}", result.Exception?.GetType().Name ?? "unknown");
            }

            return null;
        }

        var subject = Claim(result, "sub");
        var email = Claim(result, "email");
        if (subject is null || email is null)
        {
            return null;
        }

        var verified = result.Claims.TryGetValue("email_verified", out var value)
            && (value is true || string.Equals(value?.ToString(), "true", StringComparison.OrdinalIgnoreCase));

        return new GoogleIdentity(subject, email, verified, Claim(result, "name"), Claim(result, "hd"));
    }

    private async Task<TokenValidationResult> ValidateAgainstKeysAsync(string idToken, CancellationToken cancellationToken)
    {
        var keys = await configuration.GetConfigurationAsync(cancellationToken);
        return await _handler.ValidateTokenAsync(idToken, new TokenValidationParameters
        {
            ValidIssuers = Issuers,
            ValidAudience = options.Value.ClientId,
            IssuerSigningKeys = keys.SigningKeys,
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ValidateLifetime = true,
        });
    }

    private static string? Claim(TokenValidationResult result, string type) =>
        result.Claims.TryGetValue(type, out var value) && value?.ToString() is { Length: > 0 } text ? text : null;
}
