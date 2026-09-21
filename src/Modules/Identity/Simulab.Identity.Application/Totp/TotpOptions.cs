namespace Simulab.Identity.Application.Totp;

/// <summary>
/// The two-factor switch and its key (F-11 BR3, BR12), in the <c>Identity</c> configuration section. Off by
/// default: ADR-0001 #13 ships the code switched off, and development turns it on.
/// </summary>
public sealed class TotpOptions
{
    public const string SectionName = "Identity";

    /// <summary><c>Identity:TotpEnabled</c>. False: no routes, no grant, and the password alone signs anyone in.</summary>
    public bool TotpEnabled { get; set; }

    /// <summary><c>Identity:TotpEncryptionKey</c>: a base64 256-bit AES key. Required while <see cref="TotpEnabled"/> is true.</summary>
    public string? TotpEncryptionKey { get; set; }

    /// <summary>The key length AES-256-GCM needs, in bytes.</summary>
    public const int EncryptionKeyBytes = 32;

    /// <summary>
    /// Why these options cannot start the Api, or null when they can (AC14). A switched-off feature needs no key,
    /// so turning it off never fails a start.
    /// </summary>
    public string? Problem()
    {
        if (!TotpEnabled)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(TotpEncryptionKey))
        {
            return "Identity:TotpEncryptionKey is required while Identity:TotpEnabled is true.";
        }

        var key = new byte[EncryptionKeyBytes + 1];
        return Convert.TryFromBase64String(TotpEncryptionKey, key, out var written) && written == EncryptionKeyBytes
            ? null
            : "Identity:TotpEncryptionKey must be a base64 256-bit (32-byte) key.";
    }
}
