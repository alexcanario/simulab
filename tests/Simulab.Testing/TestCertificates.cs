using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Simulab.Testing;

/// <summary>
/// F-64 AC2: the signing and encryption certificates a host outside Development needs, made on the spot (self-signed,
/// nothing committed) and handed over the way Key Vault does, as the base64 of a PKCS#12 file without a password.
/// </summary>
public sealed record TestCertificates(string Signing, string Encryption)
{
    /// <summary>A new pair, different from every other pair.</summary>
    public static TestCertificates Create() => new(CreateOne("simulab-test-signing"), CreateOne("simulab-test-encryption"));

    private static string CreateOne(string name)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={name}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Valid a year back as well: a test host runs on a fake clock that is not "now".
        // What Key Vault's default policy puts in a certificate, and what OpenIddict checks before it uses one.
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: true));
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddYears(-1), DateTimeOffset.UtcNow.AddYears(2));
        return Convert.ToBase64String(certificate.Export(X509ContentType.Pfx));
    }
}
