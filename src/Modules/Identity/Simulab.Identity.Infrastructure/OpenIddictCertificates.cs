using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;

namespace Simulab.Identity.Infrastructure;

/// <summary>
/// F-64 BR4, D10: the certificates OpenIddict signs and encrypts tokens with outside Development. A container that
/// generated its own on every start would sign everybody out on every restart and every deploy, so they come from
/// configuration (Key Vault in the cloud, where a certificate is read as a secret holding its PKCS#12 in base64,
/// no password) and the host refuses to start without them. F-73 BR3: a certificate that is loaded but could not do
/// its job (no private key, the wrong key usage, outside its validity period) also stops the start, so a bad
/// renewal shows in the first minute of a deploy and not as sign-outs later.
/// </summary>
internal static class OpenIddictCertificates
{
    internal const string SigningKey = "OpenIddict:SigningCertificate";
    internal const string EncryptionKey = "OpenIddict:EncryptionCertificate";

    internal static (X509Certificate2 Signing, X509Certificate2 Encryption) Load(IConfiguration configuration, TimeProvider? clock = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var now = (clock ?? TimeProvider.System).GetUtcNow();
        return (
            Read(configuration, SigningKey, X509KeyUsageFlags.DigitalSignature, "digital signature", now),
            Read(configuration, EncryptionKey, X509KeyUsageFlags.KeyEncipherment, "key encipherment", now));
    }

    private static X509Certificate2 Read(IConfiguration configuration, string key, X509KeyUsageFlags usage, string usageName, DateTimeOffset now)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"'{key}' is missing: outside Development the Api needs its OpenIddict signing and encryption certificates "
                + "(base64 of a PKCS#12 file without a password; docs/infra.md says where they live).");
        }

        X509Certificate2 certificate;
        try
        {
            certificate = X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(value.Trim()), password: null, X509KeyStorageFlags.EphemeralKeySet);
        }
        catch (Exception exception) when (exception is FormatException or System.Security.Cryptography.CryptographicException)
        {
            throw new InvalidOperationException($"'{key}' is not the base64 of a PKCS#12 file without a password.", exception);
        }

        if (!certificate.HasPrivateKey)
        {
            throw new InvalidOperationException($"'{key}' has no private key: the PKCS#12 file must carry it (Key Vault exports it with the secret).");
        }

        var keyUsage = certificate.Extensions.OfType<X509KeyUsageExtension>().FirstOrDefault();
        if (keyUsage is null || !keyUsage.KeyUsages.HasFlag(usage))
        {
            throw new InvalidOperationException($"'{key}' lacks the key usage '{usageName}' that its role needs (docs/infra.md: create it with the committed policy).");
        }

        if (now < certificate.NotBefore)
        {
            throw new InvalidOperationException($"'{key}' is not valid yet (valid from {certificate.NotBefore.ToUniversalTime():yyyy-MM-dd} UTC).");
        }

        if (now > certificate.NotAfter)
        {
            throw new InvalidOperationException($"'{key}' expired on {certificate.NotAfter.ToUniversalTime():yyyy-MM-dd} UTC: renew it (docs/infra.md) and redeploy.");
        }

        return certificate;
    }
}
