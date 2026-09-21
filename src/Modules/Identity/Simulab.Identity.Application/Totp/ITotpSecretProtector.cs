namespace Simulab.Identity.Application.Totp;

/// <summary>
/// Keeps the second factor useless to whoever reads the database (F-11 BR3, BR5): the authenticator secret is
/// encrypted and the recovery codes are hashed, both with the key from configuration.
/// </summary>
public interface ITotpSecretProtector
{
    string Protect(string secret);

    /// <summary>Throws <see cref="System.Security.Cryptography.CryptographicException"/> when the value was not written with this key.</summary>
    string Unprotect(string protectedSecret);

    /// <summary>HMAC-SHA256 of <paramref name="value"/> with the key, lowercase hex.</summary>
    string Hash(string value);
}
