using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Simulab.Testing;

/// <summary>
/// F-64 AC2: the signing and encryption certificates a host outside Development needs, made on the spot (self-signed,
/// nothing committed) and handed over the way Key Vault does, as the base64 of a PKCS#12 file without a password.
/// </summary>
public sealed record TestCertificates(string Signing, string Encryption)
{
    private const X509KeyUsageFlags BothUsages = X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment;

    /// <summary>A new pair, different from every other pair.</summary>
    public static TestCertificates Create() => new(CreateOne("simulab-test-signing"), CreateOne("simulab-test-encryption"));

    /// <summary>
    /// F-73 AC5: one certificate with a single thing wrong, for the refusals the Api makes at start. By default it is a
    /// good one (the default policy's key usages, valid from a year back to two years ahead, with its private key).
    /// </summary>
    public static string CreateOne(
        string name,
        X509KeyUsageFlags? usage = BothUsages,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? notAfter = null,
        bool withPrivateKey = true)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={name}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Valid a year back as well: a test host runs on a fake clock that is not "now".
        // What Key Vault's default policy puts in a certificate, and what OpenIddict checks before it uses one.
        if (usage is { } flags)
        {
            request.CertificateExtensions.Add(new X509KeyUsageExtension(flags, critical: true));
        }

        using var certificate = request.CreateSelfSigned(
            notBefore ?? DateTimeOffset.UtcNow.AddYears(-1),
            notAfter ?? DateTimeOffset.UtcNow.AddYears(2));

        return withPrivateKey
            ? Convert.ToBase64String(certificate.Export(X509ContentType.Pfx))
            : Convert.ToBase64String(PublicOnlyPkcs12(certificate));
    }

    /// <summary>
    /// A PKCS#12 file that holds the certificate and no key, which is what a mistaken export produces. Written with
    /// <c>System.Formats.Asn1</c> (RFC 7292: a PFX whose one safe holds one certificate bag, no MAC), because the
    /// builder for it lives in a package this solution does not reference.
    /// </summary>
    private static byte[] PublicOnlyPkcs12(X509Certificate2 certificate)
    {
        const string dataOid = "1.2.840.113549.1.7.1";
        const string certBagOid = "1.2.840.113549.1.12.10.1.3";
        const string x509CertificateOid = "1.2.840.113549.1.9.22.1";

        var certBag = new AsnWriter(AsnEncodingRules.DER);
        using (certBag.PushSequence())
        {
            certBag.WriteObjectIdentifier(x509CertificateOid);
            using (certBag.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                certBag.WriteOctetString(certificate.Export(X509ContentType.Cert));
            }
        }

        var safeContents = new AsnWriter(AsnEncodingRules.DER);
        using (safeContents.PushSequence())
        using (safeContents.PushSequence())
        {
            safeContents.WriteObjectIdentifier(certBagOid);
            using (safeContents.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                safeContents.WriteEncodedValue(certBag.Encode());
            }
        }

        var authenticatedSafe = new AsnWriter(AsnEncodingRules.DER);
        using (authenticatedSafe.PushSequence())
        using (authenticatedSafe.PushSequence())
        {
            authenticatedSafe.WriteObjectIdentifier(dataOid);
            using (authenticatedSafe.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                authenticatedSafe.WriteOctetString(safeContents.Encode());
            }
        }

        var pfx = new AsnWriter(AsnEncodingRules.DER);
        using (pfx.PushSequence())
        {
            pfx.WriteInteger(3);
            using (pfx.PushSequence())
            {
                pfx.WriteObjectIdentifier(dataOid);
                using (pfx.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0)))
                {
                    pfx.WriteOctetString(authenticatedSafe.Encode());
                }
            }
        }

        return pfx.Encode();
    }
}
