using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;

namespace Simulab.Identity.Infrastructure;

/// <summary>
/// F-64 BR4, D10: the certificates OpenIddict signs and encrypts tokens with outside Development. A container that
/// generated its own on every start would sign everybody out on every restart and every deploy, so they come from
/// configuration (Key Vault in the cloud, where a certificate is read as a secret holding its PKCS#12 in base64,
/// no password) and the host refuses to start without them.
/// </summary>
internal static class OpenIddictCertificates
{
    internal const string SigningKey = "OpenIddict:SigningCertificate";
    internal const string EncryptionKey = "OpenIddict:EncryptionCertificate";

    internal static (X509Certificate2 Signing, X509Certificate2 Encryption) Load(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return (Read(configuration, SigningKey), Read(configuration, EncryptionKey));
    }

    private static X509Certificate2 Read(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"'{key}' is missing: outside Development the Api needs its OpenIddict signing and encryption certificates "
                + "(base64 of a PKCS#12 file without a password; docs/infra.md says where they live).");
        }

        try
        {
            return X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(value.Trim()), password: null, X509KeyStorageFlags.EphemeralKeySet);
        }
        catch (Exception exception) when (exception is FormatException or System.Security.Cryptography.CryptographicException)
        {
            throw new InvalidOperationException($"'{key}' is not the base64 of a PKCS#12 file without a password.", exception);
        }
    }
}
