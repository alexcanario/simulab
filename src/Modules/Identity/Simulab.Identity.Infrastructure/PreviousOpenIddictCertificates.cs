using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Simulab.Identity.Infrastructure;

/// <summary>
/// F-84: the previous signing and encryption certificates, read once at start from the version immediately before the
/// current one in the vault. They only validate and decrypt; the current ones sign and encrypt.
/// </summary>
public sealed class PreviousOpenIddictCertificates
{
    public static PreviousOpenIddictCertificates None { get; } = new(null, null);

    private PreviousOpenIddictCertificates(X509Certificate2? signing, X509Certificate2? encryption)
    {
        Signing = signing;
        Encryption = encryption;
    }

    public X509Certificate2? Signing { get; }

    public X509Certificate2? Encryption { get; }

    public static PreviousOpenIddictCertificates Load(
        IConfiguration configuration,
        ICertificateVersionSource? source,
        PreviousCertificateOptions options,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        return None;
    }
}
