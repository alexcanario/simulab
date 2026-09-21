using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Simulab.Identity.Application.Totp;

namespace Simulab.Identity.Infrastructure.Totp;

/// <summary>
/// AES-256-GCM over the authenticator secret (F-11 BR3), imported from Simulae: the stored value is base64 of
/// nonce, ciphertext and tag, so a changed byte fails the tag check instead of decrypting to garbage. The same
/// key also signs the recovery-code hashes (BR5).
/// </summary>
public sealed class AesGcmTotpSecretProtector : ITotpSecretProtector
{
    private const int NonceSize = 12; // 96 bits, the size AES-GCM is defined for.
    private const int TagSize = 16;   // 128 bits.

    private readonly byte[] _key;

    public AesGcmTotpSecretProtector(IOptions<TotpOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Registered only while the feature is on, and the options are validated at start (AC14).
        if (options.Value.Problem() is { } problem)
        {
            throw new InvalidOperationException(problem);
        }

        _key = Convert.FromBase64String(options.Value.TotpEncryptionKey!);
    }

    public string Protect(string secret)
    {
        ArgumentNullException.ThrowIfNull(secret);

        var plain = Encoding.UTF8.GetBytes(secret);
        var combined = new byte[NonceSize + plain.Length + TagSize];
        var nonce = combined.AsSpan(0, NonceSize);
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plain, combined.AsSpan(NonceSize, plain.Length), combined.AsSpan(NonceSize + plain.Length, TagSize));

        return Convert.ToBase64String(combined);
    }

    public string Unprotect(string protectedSecret)
    {
        ArgumentNullException.ThrowIfNull(protectedSecret);

        var combined = Convert.FromBase64String(protectedSecret);
        if (combined.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("The protected secret is too short to hold a nonce and a tag.");
        }

        var plain = new byte[combined.Length - NonceSize - TagSize];
        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(combined.AsSpan(0, NonceSize), combined.AsSpan(NonceSize, plain.Length), combined.AsSpan(NonceSize + plain.Length, TagSize), plain);

        return Encoding.UTF8.GetString(plain);
    }

    public string Hash(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Convert.ToHexStringLower(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(value)));
    }
}
