namespace Simulab.Identity.Application.Totp;

/// <summary>The RFC 6238 arithmetic (F-11 BR2, BR4): secrets, the <c>otpauth://</c> URI and its QR code, and code checks.</summary>
public interface ITotpAuthenticator
{
    /// <summary>A new random secret, base32, as an authenticator app expects it.</summary>
    string GenerateSecret();

    /// <summary>The <c>otpauth://totp/...</c> URI an app scans, labelled with the account's email.</summary>
    string OtpAuthUri(string email, string secret);

    /// <summary>The URI as a QR code, a PNG data URI the page shows as an image.</summary>
    string QrCodeDataUri(string otpAuthUri);

    /// <summary>
    /// The 30-second step <paramref name="code"/> belongs to when it is valid at <paramref name="at"/> — its own
    /// step or one either side (BR4) — or null. The replay rule is the caller's (<c>User.AcceptTotpStep</c>).
    /// </summary>
    long? MatchStep(string secret, string code, DateTimeOffset at);
}
